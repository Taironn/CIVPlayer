using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIVPlayer.Source
{   //TODO
	//Több rákérdezést lekezelni -> eventből timestampet kéne kiszedni
	//Temp mappa logolás
	class StateAPI
	{
		private static int ioSleepTime = 1000;
		private string currentPlayer;
		private FileInfo activeDropBoxSaveFile;
		public AppConfiguaraion appConfig { get; set; }
		public GameConfig gameConfig { get; set; }
		public delegate void ErrorFunc(string s, Exception e);
		public delegate void currentPlayerChanged();
		public delegate void usersTurn();
		public delegate void userPassed();
		public delegate void InvokeFunction(Action callback);
		public event currentPlayerChanged CurrentPlayerChanged;
		public event usersTurn UsersTurn;
		public event userPassed UserPassed;
		private ErrorFunc errFunc;
		private InvokeFunction invokeFunction;
		private bool alreadyCopied;
		private DateTime lastGameFolderChecked;

		private FileSystemWatcher dropBoxFolderWatcher;
		private FileSystemWatcher gameFolderWatcher;

		public string CurrentPlayer
		{
			get
			{
				return currentPlayer;
			}
			set
			{
				alreadyCopied = false;
				if (currentPlayer == appConfig.PlayerName && value != appConfig.PlayerName)
				{
					UserPassed();
				}
				if (currentPlayer != value)
				{
					if (value == appConfig.PlayerName)
					{
						CopySaveFromDropBoxtoPC();
						currentPlayer = value;
						CurrentPlayerChanged();
						UsersTurn();
					}
					else
					{
						currentPlayer = value;
						CurrentPlayerChanged();
					}
				}
			}
		}

		public StateAPI(AppConfiguaraion appConfig, GameConfig gameConfig, ErrorFunc ef, InvokeFunction invokefunc)
		{
			this.appConfig = appConfig;
			this.gameConfig = gameConfig;
			errFunc = ef;
			invokeFunction = invokefunc;
			dropBoxFolderWatcher = null;
		}
		public void Initialize()
		{
			alreadyCopied = false;
			lastGameFolderChecked = DateTime.Now.AddSeconds(-10);
			if (!checkConfigFoldersExistence())
			{
				return;
			}
			dropBoxFolderWatcher = new FileSystemWatcher();
			dropBoxFolderWatcher.Path = appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath;
			//dropBoxFolderWatcher.NotifyFilter = NotifyFilters.Attributes |  NotifyFilters.LastWrite;
			dropBoxFolderWatcher.Filter = "*" + gameConfig.fileNameEnding + gameConfig.saveExtension;
			dropBoxFolderWatcher.EnableRaisingEvents = true;
			dropBoxFolderWatcher.Changed += DropBoxFolderChanged;
			dropBoxFolderWatcher.Created += DropBoxFolderChanged;
			dropBoxFolderWatcher.Renamed += DropBoxFolderChanged;

			gameFolderWatcher = new FileSystemWatcher();
			gameFolderWatcher.Path = appConfig.CIV5Folder;
			//gameFolderWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite;
			gameFolderWatcher.Filter = "*" + gameConfig.saveExtension;
			gameFolderWatcher.EnableRaisingEvents = true;
			//gameFolderWatcher.Renamed += GameFolderChanged;
			gameFolderWatcher.Created += GameFolderChanged;
			gameFolderWatcher.Changed += GameFolderChanged;

			load();

		}
		private void load()
		{
			DirectoryInfo dInfo = new DirectoryInfo(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath);
			if (!dInfo.Exists)
			{
				errFunc(gameConfig.dropBoxExtendedPath + " elérés nem létezik a DropBox mappában!", null);
			} else
			{
				FileInfo[] files = dInfo.GetFiles();
				List<FileInfo> saves = files.Where(f => f.Extension == gameConfig.saveExtension && Path.GetFileNameWithoutExtension(f.Name).Substring(Path.GetFileNameWithoutExtension(f.Name).Length - gameConfig.fileNameEnding.Length) == gameConfig.fileNameEnding).ToList();
				if (saves.Count == 0) errFunc("Nincs " + gameConfig.fileNameEnding + gameConfig.saveExtension + " a DropBox" + gameConfig.dropBoxExtendedPath + " mappában! Nem lehet meghatározni az aktív játékost, kérlek javítsd!", null);
				else if (saves.Count > 1) errFunc("Több mentés a  Dropbox" + gameConfig.dropBoxExtendedPath + " mappában! Kérlek javítsd!", null);
				else
				{
					string fname = Path.GetFileNameWithoutExtension(saves[0].Name);
					activeDropBoxSaveFile = saves[0];
					CurrentPlayer = fname.Substring(0, fname.Length - gameConfig.fileNameEnding.Length);
				}
			}
		}

		private void CopySaveFromDropBoxtoPC()
		{
			gameFolderWatcher.EnableRaisingEvents = false;
			Thread.Sleep(ioSleepTime);
			activeDropBoxSaveFile.CopyTo(appConfig.CIV5Folder + "/" + activeDropBoxSaveFile.Name, true);
			gameFolderWatcher.EnableRaisingEvents = true;
		}

		public void DropBoxFolderChanged(object sender, FileSystemEventArgs e)
		{
			load();
		}

		public void GameFolderChanged(object sender, FileSystemEventArgs e)
		{
			if (!(CurrentPlayer == appConfig.PlayerName) || alreadyCopied || (DateTime.Now - lastGameFolderChecked).TotalSeconds < 1)
			{
				return;
			}
			DirectoryInfo dInfo = dInfo = new DirectoryInfo(appConfig.CIV5Folder);
			if (!dInfo.Exists)
			{
				errFunc(appConfig.CIV5Folder + " elérés nem létezik! (megadott Játék mentés elérés)", null);
				return;
			} else
			{
				FileInfo newestSave = dInfo.GetFiles().Where(f => f.Extension == gameConfig.saveExtension)
					.OrderByDescending(f => f.LastWriteTime).First();
				try
				{
					invokeFunction(() => askThenCopySaveFromGameFolder(newestSave, appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath + "/" + getNewPlayerName() + gameConfig.fileNameEnding + gameConfig.saveExtension));
				} catch (Exception exeption)
				{
					errFunc(exeption.Message,exeption);
				}
			}

		}

		private void askThenCopySaveFromGameFolder(FileInfo newestSave, string savePath)
		{
			Form myForm = new Form { TopMost = true };
			DialogResult dialogResult = MessageBox.Show(myForm, "Másolhatom ezt a mentést?\n" + newestSave.Name, "Új mentés a mappában", MessageBoxButtons.YesNo);
			if (dialogResult == DialogResult.Yes)
			{
				//dropBoxFolderWatcher.EnableRaisingEvents = false;
				activeDropBoxSaveFile.Delete();
				newestSave.CopyTo(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath + "/" + getNewPlayerName() + gameConfig.fileNameEnding + gameConfig.saveExtension, true);
				//Copy to Temp for logging
				DateTime n = DateTime.Now;
				string customTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmssff");
				newestSave.CopyTo(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath + gameConfig.tempSaveExtendedPath + "/" + appConfig.PlayerName +"_" + customTimeStamp + gameConfig.saveExtension, true);
				alreadyCopied = true;
				lastGameFolderChecked = DateTime.Now;
				//dropBoxFolderWatcher.EnableRaisingEvents = true;
				UserPassed();
				//load();
			} else if (dialogResult == DialogResult.No)
			{
				lastGameFolderChecked = DateTime.Now;
				return;
			}
		}

		public Boolean checkConfigFoldersExistence()
		{
			DirectoryInfo dInfo = new DirectoryInfo(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath);
			if (!dInfo.Exists)
			{
				errFunc(gameConfig.dropBoxExtendedPath + " elérés nem létezik a DropBox mappában!", null);
				return false;
			}
			dInfo = new DirectoryInfo(appConfig.CIV5Folder);
			if (!dInfo.Exists)
			{
				errFunc(appConfig.CIV5Folder + " elérés nem létezik! (megadott Játék mentés elérés)", null);
				return false;
			}

			return true;
		}
		public void PlayerNameChanged()
		{
			dropBoxFolderWatcher.EnableRaisingEvents = false;
			gameFolderWatcher.EnableRaisingEvents = false;
			if (currentPlayer == appConfig.PlayerName)
			{
				//Change currentPlayer locally, so if active player is the new player
				//the system still handles that as a normal playerchange
				currentPlayer = "----No player like this---";
				load();
			} else
			{
				UserPassed();
			}
			dropBoxFolderWatcher.EnableRaisingEvents = true;
			gameFolderWatcher.EnableRaisingEvents = true;
		}

		private String getNewPlayerName()
		{
			int indexOfNew = gameConfig.players.IndexOf(currentPlayer);
			if (indexOfNew == -1) throw new Exception("A beállított játékos nem szerepel a config-ban!");
			if (gameConfig.players.Count - 1 == indexOfNew)
			{
				return gameConfig.players[0];
			} else
			{
				return gameConfig.players[indexOfNew + 1];
			}
		}
	}
}
