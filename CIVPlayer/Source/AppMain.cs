using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace CIVPlayer.Source
{
	class AppMain : INotifyPropertyChanged
	{
		private AppConfiguaraion appConfig;
		private GameConfig gameConfig { get; }
		private StateAPI stateApi;
		private SoundPlayer soundPlayer;
		private string dropBoxFolder;
		public string DropBoxFolder
		{
			get
			{
				return dropBoxFolder;
			}
			set
			{
				dropBoxFolder = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DropBoxFolder"));
			}
		}
		private string civ5Folder;
		public string CIV5Folder
		{
			get
			{
				return civ5Folder;
			}

			set
			{
				civ5Folder = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CIV5Folder"));
			}
		}
		private string playerName;
		public string PlayerName
		{
			get
			{
				return playerName;
			}

			set
			{
				playerName = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlayerName"));
			}
		}
		public string CurrentPlayer
		{
			get
			{
				return stateApi.CurrentPlayer;
			}
		}
		public List<string> Players
		{
			get
			{
				return gameConfig.players;
			}
		}
		public List<GameConfigListRow> GameConfigRows
		{
			get
			{
				return gameConfig.gameConfigRows;
			}
		}

		public FileInfo appConfigPath = new FileInfo(Environment.CurrentDirectory + "/Resources/appconfig.bin");
		private MainWindow appWindow;

		public event PropertyChangedEventHandler PropertyChanged;
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public AppMain(MainWindow appWindow)
		{
			this.appWindow = appWindow;
			log.Info("Application started");
			FileInfo soundFile = new FileInfo(Environment.CurrentDirectory + "/Resources/notification.wav");
			if (soundFile.Exists)
			{
				soundPlayer = new SoundPlayer(Environment.CurrentDirectory + "/Resources/notification.wav");
			} else
			{
				soundPlayer = null;
			}
			gameConfig = new GameConfig();
			appConfig = new AppConfiguaraion();
			stateApi = new StateAPI(appConfig, gameConfig, this.VarnUser, this.appWindow.Dispatcher.Invoke);
			stateApi.CurrentPlayerChanged += this.currentPlayerChangedHandler;
			stateApi.UsersTurn += this.UsersTurnHandler;
			stateApi.UserPassed += this.UserPassedHandler;
			ReadAppConfig();
			CIV5Folder = appConfig.CIV5Folder;
			DropBoxFolder = appConfig.DropBoxFolder;
			PlayerName = appConfig.PlayerName;
		}

		public void checkAllSettingsGiven()
		{
			if (DropBoxFolder != "" && CIV5Folder != "" && PlayerName != "")
			{
				log.Info("Initializing setup");
				ResetStatus();
				appConfig.DropBoxFolder = this.DropBoxFolder;
				appConfig.CIV5Folder = this.CIV5Folder;
				appConfig.PlayerName = this.PlayerName;
				appConfig.EverythingSetup = true;
				ReadGameConfig();
				SaveAppConfig();
				log.Info("New setup finished");
			} else
			{
				log.Info("Settings not filled correctly.");
				VarnUser("Minden mezőt tölts ki!", null);
			}
		}

		protected void ReadAppConfig()
		{
			log.Info("Reading Appconfig file");
			if (!appConfigPath.Exists)
			{
				log.Info("Appconfig file does not exist! Creating new one");
				SaveAppConfig();
			} else
			{
				try
				{
					IFormatter formatter = new BinaryFormatter();
					Stream stream = new FileStream(appConfigPath.ToString(),
											  FileMode.Open,
											  FileAccess.Read,
											  FileShare.Read);
					appConfig = (AppConfiguaraion)formatter.Deserialize(stream);
					stateApi.appConfig = this.appConfig;
					stream.Close();
					log.Info("Appconfig read successfully");
					CIV5Folder = appConfig.CIV5Folder;
					DropBoxFolder = appConfig.DropBoxFolder;
					PlayerName = appConfig.PlayerName;
					//appWindow.Dispatcher.Invoke(() => appWindow.refreshPlayersComboBox());

				} catch (Exception)
				{
					SaveAppConfig();
					log.Error("Cannot deserialize AppConfig! - Created new instead");
				}
			}
			ReadGameConfig();
		}

		protected void ReadGameConfig()
		{
			try
			{
				if (appConfig.DropBoxFolder != "")
				{
					log.Info("Reading config file from dropbox");
					string[] lines = File.ReadAllLines(appConfig.DropBoxFolder + "/CIV/civ5.config");
					gameConfig.RawData = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);
					log.Info("Config file found and valid");
					stateApi.Initialize();
				}
			} catch (Exception e)
			{
				VarnUser("A dropbox mappa nem helyes, vagy hiányzik a config fájl!", e);
			}
		}

		public bool LoadGameConfigData()
		{
			if (this.DropBoxFolder == "" || this.DropBoxFolder == null)
			{
				VarnUser("Nem adtad meg a dropbox elérési útját!", null);
				return false;
			} else
			{
				log.Info("Loading config file for player selection");
				string[] lines = File.ReadAllLines(this.DropBoxFolder + "/CIV/civ5.config");
				gameConfig.RawData = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);
				log.Info("Config file found and valid");
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Players"));
				return true;
			}
		}

		protected void SaveAppConfig()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream(appConfigPath.ToString(),
									 FileMode.Create,
									 FileAccess.Write, FileShare.None);
			formatter.Serialize(stream, appConfig);
			stream.Close();
			log.Info("Appconfig saved");
		}
		private void currentPlayerChangedHandler()
		{
			log.Info("Handling current player changed");
			if (PropertyChanged != null)
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CurrentPlayer"));
			appWindow.tray_icon.Text = CurrentPlayer + " köre van";
		}
		private void UsersTurnHandler()
		{
			log.Info("Handling user's turn");
			SystemSounds.Asterisk.Play();
			FileInfo soundFile = new FileInfo(Environment.CurrentDirectory + "/Resources/notification.wav");
			if (soundPlayer != null && soundFile.Exists)
			{
				soundPlayer.Play();
			}
			appWindow.Dispatcher.Invoke(() =>
			{
				appWindow.StatusGrid.Background = System.Windows.Media.Brushes.ForestGreen;
				System.Windows.Forms.MessageBox.Show(new Form { TopMost = true }, "Te lépsz!");
				stateApi.lastSetupTime = DateTime.Now;
			});
		}
		private void ResetStatus()
		{
			appWindow.Dispatcher.Invoke(() =>
			appWindow.StatusGrid.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFE5E5E5")));
		}
		private void UserPassedHandler()
		{
			log.Info("Handling user passed");
			ResetStatus();
		}
		private void VarnUser(string s, Exception e)
		{
			if (e != null) log.Error(s, e);
			else log.Warn(s);
			System.Windows.Forms.MessageBox.Show(new Form { TopMost = true }, s);
		}
	}
}
