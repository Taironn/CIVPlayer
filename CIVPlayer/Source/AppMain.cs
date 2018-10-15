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

		public string DropBoxFolder
		{
			get
			{
				return appConfig.DropBoxFolder;
			}

			set
			{
				if (appConfig.EverythingSetup)
				{

					appConfig.DropBoxFolder = value;
					ReadGameConfig();
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DropBoxFolder"));
					SaveAppConfig();
				}
				else
				{
					appConfig.DropBoxFolder = value;
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DropBoxFolder"));
					checkAllSettingsGiven();
				}
			}
		}
		public string CIV5Folder
		{
			get
			{
				return appConfig.CIV5Folder;
			}

			set
			{
				if (appConfig.EverythingSetup)
				{
					appConfig.CIV5Folder = value;
					ReadGameConfig();
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CIV5Folder"));
					SaveAppConfig();
				}
				else
				{
					appConfig.CIV5Folder = value;
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CIV5Folder"));
					checkAllSettingsGiven();
				}
			}
		}
		public string PlayerName
		{
			get
			{
				return appConfig.PlayerName;
			}

			set
			{
				if (appConfig.EverythingSetup)
				{
					appConfig.PlayerName = value;
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PlayerName"));
					if (appConfig.DropBoxFolder != "" && appConfig.CIV5Folder != "")
					{
						SaveAppConfig();
						stateApi.PlayerNameChanged();
					}
				}
				else
				{
					appConfig.PlayerName = value;
					PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PlayerName"));
					checkAllSettingsGiven();
				}
			}
		}
		public string CurrentPlayer
		{
			get
			{
				return stateApi.CurrentPlayer;
			}
		}
		public List<GameConfigListRow> GameConfigRows
		{
			get
			{
				return gameConfig.gameConfigRows;
			}
		}
		public bool ConfigExists
		{
			get
			{
				return appConfig.Valid;
			}

			set
			{
				appConfig.Valid = value;
				if (value)
					stateApi.Initialize();
			}
		}

		public FileInfo appConfigPath = new FileInfo(Environment.CurrentDirectory + "/Resources/appconfig.bin");
		private MainWindow appWindow;

		public event PropertyChangedEventHandler PropertyChanged;

		public AppMain(MainWindow appWindow)
		{
			this.appWindow = appWindow;
			soundPlayer = new SoundPlayer(Environment.CurrentDirectory + "/Resources/notification.wav");
			gameConfig = new GameConfig();
			appConfig = new AppConfiguaraion();
			stateApi = new StateAPI(appConfig, gameConfig, this.VarnUser, this.appWindow.Dispatcher.Invoke);
			stateApi.CurrentPlayerChanged += this.currentPlayerChangedHandler;
			stateApi.UsersTurn += this.UsersTurnHandler;
			stateApi.UserPassed += this.UserPassedHandler;
			ReadAppConfig();
		}

		private void checkAllSettingsGiven()
		{
			if (DropBoxFolder != "" && CIV5Folder != "" && PlayerName != "")
			{
				appConfig.EverythingSetup = true;
				ReadGameConfig();
				SaveAppConfig();
			}
		}

		protected void ReadAppConfig()
		{
			if (!appConfigPath.Exists)
			{
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

				} catch (Exception)
				{
					SaveAppConfig();
					Console.WriteLine("Cannot deserialize AppConfig!");
				}
			}
			ReadGameConfig();
		}

		protected void ReadGameConfig()
		{
			try {
				if (appConfig.DropBoxFolder != "")
				{
					string[] lines = File.ReadAllLines(appConfig.DropBoxFolder + "/CIV/civ5.config");
					gameConfig.RawData = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);
					ConfigExists = true;
				}
			} catch (Exception e)
			{
				VarnUser("A dropbox mappa nem helyes, vagy hiányzik a config fájl!",e);
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
		}
		private void currentPlayerChangedHandler()
		{
			if (PropertyChanged != null)
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CurrentPlayer"));
			appWindow.tray_icon.Text = CurrentPlayer + " köre van";
		}
		private void UsersTurnHandler()
		{
			SystemSounds.Asterisk.Play();
			soundPlayer.Play();
			appWindow.Dispatcher.Invoke(() =>
			{
				appWindow.StatusGrid.Background = System.Windows.Media.Brushes.ForestGreen;
				System.Windows.Forms.MessageBox.Show(new Form { TopMost = true },"Te lépsz!");
			});
		}
		private void UserPassedHandler()
		{
			appWindow.Dispatcher.Invoke(() =>
			appWindow.StatusGrid.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFE5E5E5")));
		}
		private void VarnUser(string s, Exception e)
		{
			System.Windows.Forms.MessageBox.Show(new Form { TopMost = true },s);	
		}
	}
}
