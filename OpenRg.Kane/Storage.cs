using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.Rg.Traits;

namespace OpenRg.Kane
{
	public class UserStatistics
	{
		public int Kills = 0;
		public int Killed = 0;
		public int Damaged = 0;
		public int Healed = 0;
		public int Score = 0;
		public int BuildingsDestroyed = 0;
		// public int TimesPlayed = 0;

		public MiniYaml Save()
		{
			var y = FieldSaver.Save(this);

			return y;
		}
	}

	public class StoredUser
	{
		public string Nickname = "";
		public string Serial = "";
		public string Key = "";
		public int TimesSeen = 0;
		public int TotalScore = 0;
		public UserStatistics Stats = new UserStatistics();

		public void Load(MiniYamlNode userNode)
		{
			Key = userNode.Key;

			object defaults = Activator.CreateInstance(this.GetType());
			FieldLoader.InvalidValueAction = (s, t, f) =>
			{
				object ret = defaults.GetType().GetField(f).GetValue(defaults);
				System.Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s, t.Name, f, ret));
				return ret;
			};

			FieldLoader.Load(this, userNode.Value);
			FieldLoader.Load(Stats, userNode.Value.Nodes.Where(n => n.Key == "Statistics").FirstOrDefault().Value);
		}

		public MiniYaml Save()
		{
			var y= FieldSaver.Save(this);
			y.Nodes.Add(new MiniYamlNode("Statistics", Stats.Save()));

			return y;
		}
	}

	public class Storage
	{
		public List<StoredUser> Users = new List<StoredUser>();

		public StoredUser GetUser(Player player)
		{
			var entry = 
				Users.Where(u => u.Key == player.PlayerName + "@" + player.PlayerActor.TraitOrDefault<RgUniqueId>().Serial).
					SingleOrDefault();
			if (entry == null)
			{
				entry = new StoredUser();
				entry.Key = player.PlayerName + "@" + player.PlayerActor.TraitOrDefault<RgUniqueId>().Serial;
				entry.Nickname = player.PlayerName;
				entry.Serial = player.PlayerActor.TraitOrDefault<RgUniqueId>().Serial;
				Users.Add(entry);
			}
			return entry;
		}

		public static Storage LoadStorage(string path)
		{
			var storage = new Storage();
			if (!File.Exists(path))
			{
				storage.Save(path);
				return storage;
			}


			var y = MiniYaml.FromFile(path);
			foreach (var node in y)
			{
				if (node.Key == "Users")
				{
					foreach (var userNode in node.Value.Nodes)
					{
						var user = new StoredUser();
						user.Load(userNode);
						storage.Users.Add(user);
					}
				}
			}

			return storage;
		}

		public void Save(string path)
		{
			var y = new MiniYaml("");
			var users = Users.Select(u => new MiniYamlNode(u.Key,  u.Save()));

			y.Nodes.Add(new MiniYamlNode("Users", new MiniYaml("", users.ToList())));
			y.Nodes.WriteToFile(path);
		}
	}
}
