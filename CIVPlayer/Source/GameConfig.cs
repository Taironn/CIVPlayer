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
		}

		public GameConfig()
		{
			rawData = null;
			players = null;
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
