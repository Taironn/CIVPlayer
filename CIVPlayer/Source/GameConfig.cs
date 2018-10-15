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
		public List<GameConfigListRow> gameConfigRows;
		public string dropBoxExtendedPath = "/CIV";
		public string tempSaveExtendedPath = "/Temp";
		public string fileNameEnding = "jon";
		public string saveExtension = "Civ5Save";

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
			players = new List<string>();
			players = rawData["players"].Split(';').ToList();
			gameConfigRows.Clear();
			for (int i = 0; i < players.Count; i++)
			{
				gameConfigRows.Add(new GameConfigListRow(i + 1, players[i]));
			}
			dropBoxExtendedPath = rawData["dropboxextendedpath"];
			fileNameEnding = rawData["fileending"];
			saveExtension = rawData["saveextension"];
			tempSaveExtendedPath = rawData["tempsaveextendedpath"];
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
		public GameConfigListRow(int n, string p)
		{
			this.Number = n;
			this.Player = p;
		}
	}
}
