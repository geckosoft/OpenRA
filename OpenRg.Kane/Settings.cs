using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRg.Kane
{
	public class IrcSettings
	{
		public string Server = "";
		public int Port = 6667;
		public bool Enabled = false;
		public string Nick = "Kane_Rg";
		public string Name = "Kane Lubs OpenRg";
		public string[] Channels = new string[0];

		public bool Bridge = true;
		public bool OneWay = false;
		public bool UseColors = true;
		public string ColorGDI = "8";
		public string ColorNod = "4";
		public string ColorSpectator = "0";
		public string ColorBack = "1";

		public bool UseStats = false;
	}

	public class KaneSettings
	{
		public string Storage = "KaneDb.yaml";
		public string Nick = "Kane";
	}
}
