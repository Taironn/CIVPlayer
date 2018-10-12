using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CIVPlayer.Source
{
	class AppMain : INotifyPropertyChanged
	{
		private AppConfiguaraion appConfig;
		private GameConfig gameConfig { get; }

		public string DropBoxFolder
		{
			get
			{
				return appConfig.DropBoxFolder;
			}

			set
			{
				appConfig.DropBoxFolder = value;
				ReadGameConfig();
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DropBoxFolder"));
				SaveAppConfig();
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
				appConfig.CIV5Folder = value;
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CIV5Folder"));
				SaveAppConfig();
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
				appConfig.PlayerName = value;
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PlayerName"));
				SaveAppConfig();
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

		public AppMain(MainWindow appWindow)
		{
			this.appWindow = appWindow;
			gameConfig = new GameConfig();
			appConfig = new AppConfiguaraion();
			ReadAppConfig();
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
				}
			} catch
			{
				VarnUser("A dropbox mappa nem helyes, vagy hiányzik a config fájl!");
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

		public void setCIV5Folder(string folder)
		{
			appConfig.CIV5Folder = folder;
			SaveAppConfig();
		}

		public string getDropBoxFolder() { return appConfig.DropBoxFolder; }
		public string getCIV5Folder() { return appConfig.CIV5Folder; }

		private void VarnUser(string s)
		{
			System.Windows.MessageBox.Show(s);
			
		}
	}
}
