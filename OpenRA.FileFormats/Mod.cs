using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenRA.FileFormats
{
	public class Mod
	{
		public string Title;
		public string Description;
		public string Version;
		public string Author;
		public string[] RequiresMods;
		public bool Standalone = false;
		public static string SupportDir = "";
		public static Dictionary<string, Mod> AllMods
		{
			get
			{
				return ValidateMods(
					Directory.GetDirectories(Path.Combine(SupportDir, "mods")).Select(
						x => x.Substring(Path.Combine(SupportDir, "mods").Length+1)).ToArray());
			}
		}
			public static Dictionary<string, Mod> ValidateMods(string[] mods)
		{
			var ret = new Dictionary<string, Mod>();
			foreach (var m in mods)
			{
				if (!File.Exists( Path.Combine(SupportDir, "mods" + Path.DirectorySeparatorChar + m + Path.DirectorySeparatorChar + "mod.yaml")))
					continue;

				var yaml = new MiniYaml(null, MiniYaml.FromFile(Path.Combine(SupportDir, "mods") + Path.DirectorySeparatorChar + m + Path.DirectorySeparatorChar + "mod.yaml"));
				if (!yaml.NodesDict.ContainsKey("Metadata"))
				{
					continue;
				}

				ret.Add(m, FieldLoader.Load<Mod>(yaml.NodesDict["Metadata"]));
			}
			return ret;
		}
	}
}
