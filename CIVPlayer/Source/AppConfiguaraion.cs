using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CIVPlayer.Source
{
	[Serializable]
	class AppConfiguaraion
	{
		public string CIV5Folder { get; set; }
		public string DropBoxFolder { get; set; }
		public string PlayerName { get; set; }
		public bool Valid { get; set; }
		public string CivExePath { get; set; }
		public bool StartOnUsesTurn { get; set; }
		public bool StartWithoutPrompt { get; set; }
		public bool UsersTurnMusic { get; set; }
		public bool EverythingSetup { get; set; }

		public AppConfiguaraion()
		{
			CIV5Folder = "";
			DropBoxFolder = "";
			PlayerName = "";
			Valid = false;
			EverythingSetup = false;
			StartOnUsesTurn = false;
			StartWithoutPrompt = false;
			UsersTurnMusic = true;
		}

	}
}
