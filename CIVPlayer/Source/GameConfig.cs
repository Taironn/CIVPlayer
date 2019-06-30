using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIVPlayer.Source
{
	public class GameConfig
	{
		private Dictionary<string, string> rawData;
		public List<string> players;
        public List<string> emails;
		public List<GameConfigListRow> gameConfigRows;
		public string dropBoxExtendedPath = "/CIV";
		public string tempSaveExtendedPath = "/Temp";
		public string fileNameEnding = "jon";
		public string saveExtension = "Civ5Save";
        public string fromMailAddress;
        public string fromMailPassword;

		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		public Dictionary<string, string> RawData
		{
			get
			{
				return rawData;
			}

			set
			{
				rawData = value;
				UpdateFromRaw();
			}
		}

		public void UpdateFromRaw()
		{
			log.Info("Updating Game config from dropbox config file");
			players = new List<string>();
			try
			{
				players = rawData["players"].Split(';').ToList();
                emails = rawData["emails"].Split(';').ToList();
				gameConfigRows.Clear();
				for (int i = 0; i < players.Count; i++)
				{
					gameConfigRows.Add(new GameConfigListRow(i + 1, players[i], emails[i]));
				}
                fromMailAddress = rawData["frommailaddress"];
                fromMailPassword = rawData["frommailpassword"];
				dropBoxExtendedPath = rawData["dropboxextendedpath"];
				fileNameEnding = rawData["fileending"];
				saveExtension = rawData["saveextension"];
				tempSaveExtendedPath = rawData["tempsaveextendedpath"];
			}catch (Exception e)
			{
				log.Error("Dropbox config file incorrect!",e);
				log.Info("Config file should have:");
				log.Info("players=x;y;z");
                log.Info("emails=asdf@xy.com;fdsa@xy.com,safd@yx.com");
                log.Info("frommailaddress=asdf@xy.com");
                log.Info("frommailpassword=pasword");
                log.Info("dropboxextendedpath=\foldername");
				log.Info("fileending=jon");
				log.Info("saveextension=.Civ5Save");
				log.Info("tempsaveextendedpath=\foldername");
			}
		}

		public GameConfig()
		{
			rawData = new Dictionary<string, string>();
			players = new List<string>();
			gameConfigRows = new List<GameConfigListRow>();
		}
	}

	public class GameConfigListRow
	{
		public int Number { get; set; }

		public string Player { get; set; }

        public string Email { get; set; }
		public GameConfigListRow(int n, string p, string e)
		{
			this.Number = n;
			this.Player = p;
            this.Email = e;
		}
	}
}
