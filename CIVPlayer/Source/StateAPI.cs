using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIVPlayer.Source
{
	class StateAPI
	{
		private static int ioSleepTime = 1000;
		private static int saveWaitSeconds = 3;
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
		public DateTime lastSetupTime;
		private FileSystemWatcher dropBoxFolderWatcher;
		private FileSystemWatcher gameFolderWatcher;

		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string CurrentPlayer
		{
			get
			{
				return currentPlayer;
			}
			set
			{
				lock (currentPlayer)
				{
					log.Info("Current player changed from " + currentPlayer + " to " + value);
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
						} else
						{
							currentPlayer = value;
							CurrentPlayerChanged();
						}
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
			//currentPlayer = "";
		}
		public void Initialize()
		{
			log.Info("Initializing StateAPI");
			currentPlayer = "";
			lastSetupTime = DateTime.Now;
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
			dropBoxFolderWatcher.InternalBufferSize = 1048576;
			dropBoxFolderWatcher.Changed += DropBoxFolderChanged;
			dropBoxFolderWatcher.Created += DropBoxFolderChanged;
			dropBoxFolderWatcher.Renamed += DropBoxFolderChanged;
			dropBoxFolderWatcher.Error += OnError;
			log.Info("DropBoxFolderWatcher watching at: " + dropBoxFolderWatcher.Path);

			gameFolderWatcher = new FileSystemWatcher();
			gameFolderWatcher.Path = appConfig.CIV5Folder;
			//gameFolderWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite;
			gameFolderWatcher.Filter = "*" + gameConfig.saveExtension;
			gameFolderWatcher.EnableRaisingEvents = true;
			gameFolderWatcher.InternalBufferSize = 1048576;
			//gameFolderWatcher.Renamed += GameFolderChanged;
			gameFolderWatcher.Created += GameFolderChanged;
			gameFolderWatcher.Changed += GameFolderChanged;
			gameFolderWatcher.Error += OnError;
			log.Info("GameFolderWatcher watching at: " + gameFolderWatcher.Path);

			load();

		}
		private void load()
		{
			log.Debug("Updating status from DropBox");
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
			log.Info("Copying save file from dropbox to PC");
			gameFolderWatcher.EnableRaisingEvents = false;
			Thread.Sleep(ioSleepTime);
			activeDropBoxSaveFile.CopyTo(appConfig.CIV5Folder + "/" + activeDropBoxSaveFile.Name, true);
			gameFolderWatcher.EnableRaisingEvents = true;
		}

		public void DropBoxFolderChanged(object sender, FileSystemEventArgs e)
		{
			log.Debug("DropBoxFolderChangedEvent arrived");
			load();
		}

		public void GameFolderChanged(object sender, FileSystemEventArgs e)
		{
			log.Debug("GameFolderChangedEvent arrived");
			lock (currentPlayer)
			{
				if (!(CurrentPlayer == appConfig.PlayerName) || alreadyCopied || (DateTime.Now - lastGameFolderChecked).TotalSeconds < 1 || (DateTime.Now - lastSetupTime).TotalMilliseconds < ioSleepTime + 1000)
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
					log.Debug("Finding newest save file in Game Folder");
					FileInfo newestSave = dInfo.GetFiles().Where(f => f.Extension == gameConfig.saveExtension)
						.OrderByDescending(f => f.LastWriteTime).First();
					try
					{
						invokeFunction(() => askThenCopySaveFromGameFolder(newestSave, appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath + "/" + getNewPlayerName() + gameConfig.fileNameEnding + gameConfig.saveExtension));
					} catch (Exception exeption)
					{
						errFunc(exeption.Message, exeption);
					}
				}
			}
		}

		private void askThenCopySaveFromGameFolder(FileInfo newestSave, string savePath)
		{
			log.Info("Asking for copy savefile to dropbox");
			Form myForm = new Form { TopMost = true };
			DateTime curTime = DateTime.Now;
			DialogResult dialogResult = MessageBox.Show(myForm, "Másolhatom ezt a mentést?\n" + newestSave.Name, "Új mentés a mappában", MessageBoxButtons.YesNo);
			if (dialogResult == DialogResult.Yes)
			{
				dropBoxFolderWatcher.EnableRaisingEvents = false;
				int seconds = (DateTime.Now - curTime).Seconds;
				if (seconds < saveWaitSeconds)
				{
					Thread.Sleep((saveWaitSeconds - seconds) * 1000);
				}
				//Copy to Temp for logging
				DateTime n = DateTime.Now;
				string customTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				log.Info("Saving last save to Temp");
				string curFileName = Path.GetFileNameWithoutExtension(activeDropBoxSaveFile.Name);
				activeDropBoxSaveFile.MoveTo(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath + gameConfig.tempSaveExtendedPath + "/" + curFileName + "_" + customTimeStamp + gameConfig.saveExtension);
				dropBoxFolderWatcher.EnableRaisingEvents = true;
				log.Info("Saving new save to DropBox from GameFolder:" + newestSave.Name);
				newestSave.CopyTo(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath + "/" + getNewPlayerName() + gameConfig.fileNameEnding + gameConfig.saveExtension, true);
				//activeDropBoxSaveFile.Delete();
				alreadyCopied = true;
				lastGameFolderChecked = DateTime.Now;
				UserPassed();
				//load();
			} else if (dialogResult == DialogResult.No)
			{
				log.Info("User refused new save copy to DropBox");
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
		//Not used currently
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
			log.Debug("Calculating new player name");
			int indexOfNew = gameConfig.players.IndexOf(currentPlayer);
			if (indexOfNew == -1) errFunc("A beállított játékos nem szerepel a config-ban!", null);
			if (gameConfig.players.Count - 1 == indexOfNew)
			{
				return gameConfig.players[0];
			} else
			{
				return gameConfig.players[indexOfNew + 1];
			}
		}
		private void OnError(object source, ErrorEventArgs e)
		{
			if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
			{
				log.Error("Error: File System Watcher internal buffer overflow at " + DateTime.Now);
			} else
			{
				log.Error("Error: Watched directory not accessible at " + DateTime.Now);
			}
			if (source == dropBoxFolderWatcher)
			{
				DropBoxNotAccessibleError(dropBoxFolderWatcher, e);
			} else if (source == gameFolderWatcher)
			{
				GameFolderNotAccessibleError(gameFolderWatcher, e);
			} else
			{
				DropBoxNotAccessibleError(dropBoxFolderWatcher, e);
				GameFolderNotAccessibleError(gameFolderWatcher, e);
			}
		}

		//Handler for FilSystemWatcher errors
		void DropBoxNotAccessibleError(FileSystemWatcher source, ErrorEventArgs e)
		{
			int iMaxAttempts = 120;
			int iTimeOut = 3000;
			int i = 0;
			while ((!Directory.Exists(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath) || source.EnableRaisingEvents == false) && i < iMaxAttempts)
			{
				i += 1;
				try
				{
					source.EnableRaisingEvents = false;
					if (!Directory.Exists(appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath))
					{
						log.Error("Directory Inaccessible " + source.Path + " at " + DateTime.Now.ToString("HH:mm:ss"));
						System.Threading.Thread.Sleep(iTimeOut);
					} else
					{
						// ReInitialize the Component
						source.Dispose();
						source = null;
						source = new System.IO.FileSystemWatcher();
						((System.ComponentModel.ISupportInitialize)(source)).BeginInit();
						source.EnableRaisingEvents = true;
						source.Filter = "*" + gameConfig.fileNameEnding + gameConfig.saveExtension;
						source.Path = appConfig.DropBoxFolder + gameConfig.dropBoxExtendedPath;
						source.InternalBufferSize = 1048576;
						//source.NotifyFilter = System.IO.NotifyFilters.FileName;
						source.Changed += DropBoxFolderChanged;
						source.Created += DropBoxFolderChanged;
						source.Renamed += DropBoxFolderChanged;
						source.Error += OnError;
						((System.ComponentModel.ISupportInitialize)(source)).EndInit();
						log.Info("Trying to Restart RaisingEvents Watcher at " + DateTime.Now.ToString("HH:mm:ss"));
					}
				} catch (Exception error)
				{
					log.Error("Error trying Restart Service " + error.StackTrace + " at " + DateTime.Now.ToString("HH:mm:ss"), error);
					source.EnableRaisingEvents = false;
					System.Threading.Thread.Sleep(iTimeOut);
				}
			}
		}

		void GameFolderNotAccessibleError(FileSystemWatcher source, ErrorEventArgs e)
		{
			int iMaxAttempts = 120;
			int iTimeOut = 3000;
			int i = 0;
			while ((!Directory.Exists(appConfig.CIV5Folder) || source.EnableRaisingEvents == false) && i < iMaxAttempts)
			{
				i += 1;
				try
				{
					source.EnableRaisingEvents = false;
					if (!Directory.Exists(appConfig.CIV5Folder))
					{
						log.Error("Directory Inaccessible " + source.Path + " at " + DateTime.Now.ToString("HH:mm:ss"));
						System.Threading.Thread.Sleep(iTimeOut);
					} else
					{
						// ReInitialize the Component
						source.Dispose();
						source = null;
						source = new System.IO.FileSystemWatcher();
						((System.ComponentModel.ISupportInitialize)(source)).BeginInit();
						source.EnableRaisingEvents = true;
						source.Filter = "*" + gameConfig.saveExtension;
						source.Path = appConfig.CIV5Folder;
						source.InternalBufferSize = 1048576;
						//source.NotifyFilter = System.IO.NotifyFilters.FileName;
						source.Changed += GameFolderChanged;
						source.Created += GameFolderChanged;
						source.Renamed += GameFolderChanged;
						source.Error += OnError;
						((System.ComponentModel.ISupportInitialize)(source)).EndInit();
						log.Info("Trying to Restart RaisingEvents Watcher at " + DateTime.Now.ToString("HH:mm:ss"));
					}
				} catch (Exception error)
				{
					log.Error("Error trying Restart Service " + error.StackTrace + " at " + DateTime.Now.ToString("HH:mm:ss"), error);
					source.EnableRaisingEvents = false;
					System.Threading.Thread.Sleep(iTimeOut);
				}
			}
		}
	}
}
