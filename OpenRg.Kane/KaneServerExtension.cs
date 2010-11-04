using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Meebey.SmartIrc4net;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.Rg.Traits;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRg.Kane
{
	public class KaneServerExtension : NullServerExtension, INotifyDamage
	{
		public bool FirstLaunch = true;
		public OrderManager Orders;
		public IrcClient Irc = new IrcClient();
		public readonly KaneSettings Kane = new KaneSettings();

		public readonly string SettingsFile;
		Dictionary<string, object> Sections;
		public readonly Arguments Arguments;
		public readonly IrcSettings IrcSettings = new IrcSettings();
		public Storage Storage = null;
		public string StorageFile = "";
		public const string VERSION = "Kane : OpenRg Server Extension (alpha)";
		public KaneServerExtension()
		{
			try
			{
				Arguments = new Arguments(Environment.GetCommandLineArgs());
				SettingsFile = Path.GetFullPath(Arguments.GetValue("Kane.Settings", "KaneSettings.yaml"));
				StorageFile = Path.GetFullPath(Kane.Storage);
				Storage = Storage.LoadStorage(StorageFile);
				Sections = new Dictionary<string, object>()
				           	{
				           		{"Irc", IrcSettings},
								{"Kane", Kane}
				           	};

				// Override fieldloader to ignore invalid entries
				var err1 = FieldLoader.UnknownFieldAction;
				var err2 = FieldLoader.InvalidValueAction;
				var ufa = FieldLoader.UnknownFieldAction;
				FieldLoader.UnknownFieldAction = (s, f) =>
				                                 	{
				                                 		Console.WriteLine("Ignoring unknown field `{0}` on `{1}`".F(s, f.Name));
				                                 	};

				if (File.Exists(SettingsFile))
				{
					Console.WriteLine("[Kane] Loading settings file {0}", SettingsFile);
					var yaml = MiniYaml.DictFromFile(SettingsFile);

					foreach (var kv in Sections)
						if (yaml.ContainsKey(kv.Key))
							LoadSectionYaml(yaml[kv.Key], kv.Value);

					// Override with commandline args
					foreach (var kv in Sections)
						foreach (var f in kv.Value.GetType().GetFields())
							if (Arguments.Contains(kv.Key + "." + f.Name))
								FieldLoader.LoadField(kv.Value, f.Name, Arguments.GetValue(kv.Key + "." + f.Name, ""));

					FieldLoader.UnknownFieldAction = err1;
					FieldLoader.InvalidValueAction = err2;
				}

				FieldLoader.UnknownFieldAction = ufa;
			}catch(Exception ex)
			{
				Console.Write(ex.ToString());
			}
		}


		void LoadSectionYaml(MiniYaml yaml, object section)
		{
			object defaults = Activator.CreateInstance(section.GetType());
			FieldLoader.InvalidValueAction = (s,t,f) =>
			{
				object ret = defaults.GetType().GetField(f).GetValue(defaults);
				System.Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s,t.Name,f,ret) );
				return ret;
			};
			
			FieldLoader.Load(section, yaml);
		}

		public override void OnLobbyUp(OrderManager orderManager)
		{
			Orders = orderManager;
			OnServerStop(false); // Seems to have some issues sometimes.. (I could be mistaken)

			if (FirstLaunch)
			{
				if (IrcSettings.Enabled)
				{
					StartIrc();
				}
			}

			FirstLaunch = false;
		}

		private void StartIrc()
		{
			Irc.SendDelay = 250;
			Irc.AutoRetry = true;
			Irc.ActiveChannelSyncing = true;
			Irc.AutoRejoin = true;
			Irc.AutoReconnect = true;
			Irc.AutoRejoinOnKick = true;
			Irc.AutoRelogin = true;

			Irc.Connect(IrcSettings.Server, IrcSettings.Port);
			Irc.Login(IrcSettings.Nick, IrcSettings.Name);

			foreach (var chan in IrcSettings.Channels)
			{
				Irc.RfcJoin(chan);
			}

			if (IrcSettings.Bridge && !IrcSettings.OneWay)
			{
				Irc.OnChannelMessage += new IrcEventHandler(Irc_OnChannelMessage);	
			}

			new Thread(_ =>
			{
				for (; ; )
				{
					Irc.Listen(false);
					Thread.Sleep(200);
				}
			}) { IsBackground = true }.Start();
		}

		void Irc_OnChannelMessage(object sender, IrcEventArgs e)
		{
			if (ProcessIrcMessage(e))
				Orders.IssueOrder(new Order("Chat", null, "(IRC)" + e.Data.Nick + ": " + e.Data.Message){IsImmediate =  true});
		}

		public bool ProcessIrcMessage(IrcEventArgs arg)
		{
			if (arg.Data.Message.Trim() == "")
				return false;

			var parts = arg.Data.Message.Split(' ');
			parts[0] = parts[0].ToLower();

			switch (parts[0])
			{
				case "!version":
					IrcChat("Version: " + VERSION);
					return false;
			}

			return true;
		}

		public void Chat(string message)
		{
			Orders.IssueOrder(new Order("Chat", null, message) { IsImmediate = true });
		}

		public override void OnLobbySync(Session lobbyInfo, bool gameStarted)
		{
			if (lobbyInfo.Slots.Count == 0 || Orders == null || LocalClient == null)
				return;
			if (LocalClient.Name != Kane.Nick)
				Orders.IssueOrder(Order.Command("name " + Kane.Nick));
		}

		public Session.Client LocalClient
		{
			get { if (Orders == null) return null; return Orders.LocalClient; }
		}

		public override bool OnIngameChat(Session.Client client, string message, bool teamChat)
		{
			if (client == null || teamChat || !IrcSettings.Enabled || !IrcSettings.Bridge)
				return true;

			var header = "[" + client.Name + "]";
			
			if (IrcSettings.UseColors)
			{
				if (!GetClientSlot(client).Spectator && client.Country.ToLower() == "gdi")
					header = (char)3 + IrcSettings.ColorGDI + "," + IrcSettings.ColorBack + header + (char)3 ;
				else if (!GetClientSlot(client).Spectator && client.Country.ToLower() == "nod")
					header = (char)3 + IrcSettings.ColorNod + "," + IrcSettings.ColorBack + header + (char)3;
				else
					header = (char)3 + IrcSettings.ColorSpectator + "," + IrcSettings.ColorBack + header + (char)3;
			}

			if (ProcessMessage(client, GetClientPlayer(client), message))
				IrcChat(header + " " + message);

			return true;
		}

		public void IrcChat(string message)
		{
			foreach (var chan in IrcSettings.Channels)
			{
				Irc.SendMessage(SendType.Message, chan, message);
			}
		}
		private bool ProcessMessage(Session.Client client, Player player, string message)
		{
			if (message.Trim() == "")
				return false;

			var parts = message.Split(' ');
			parts[0] = parts[0].ToLower();

			switch (parts[0])
			{
				case "!version":
					Chat(VERSION);
					break;
				case "!stats":
					ShowStats(client, player);
					break;
			}
			return true;
		}

		private void ShowStats(Session.Client client, Player player)
		{
			StoredUser user = null;

			if (player == null)
			{
				user = Storage.Users.Where(u => u.Nickname == client.Name).FirstOrDefault();
			}else
			{
				user = Storage.GetUser(player);
			}

			if (user == null)
			{
				Chat("No stats available for {0}".F(client.Name));
				return;
			}

			Chat("stats: {0} has been seen {1} time(s)".F(client.Name, user.TimesSeen));
		}


		public Player GetClientPlayer(int clientId)
		{
			if (Orders.world == null)
				return null;

			return Orders.world.players.Where(p => p.Value.ClientIndex == clientId).Select(p => p.Value).FirstOrDefault();
		}

		public Session.Slot GetClientSlot(Session.Client client )
		{
			return Orders.LobbyInfo.Slots.Where(s => s.Index == client.Slot).SingleOrDefault();
		}

		public Session.Client GetClient(int clientId )
		{
			return Orders.LobbyInfo.Clients.Where(s => s.Index == clientId).SingleOrDefault();
		}
		
		public Player GetClientPlayer(Session.Client client)
		{
			if (Orders.world == null)
				return null;

			return Orders.world.players.Where(p => p.Value.ClientIndex == client.Index).Select(p=> p.Value).FirstOrDefault();
		}

		public string GetSerial(Session.Client client)
		{
			var player = GetClientPlayer(client);
			if (player == null)
				return "";

			return player.PlayerActor.TraitOrDefault<RgUniqueId>().Serial;
		}

		public override void OnServerStop(bool forced)
		{
			lock (this)
			{
				if (!Running)
					return;

				Running = false;

				if (Orders.world == null)
					return;

				// Add all players seen

				Orders.world.players.Where(
					p => p.Value.ClientIndex >= 0 && !p.Value.PlayerRef.NonCombatant && !p.Value.PlayerRef.OwnsWorld).Do(p => SavePlayer(p.Value));

				Storage.Save(StorageFile);
			}
		}

		public bool Running = false;

		public override void OnServerStart()
		{
			Running = true;
		}

		private void SavePlayer(Player player)
		{
			Storage.GetUser(player).TimesSeen++;
		}


		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.Owner.PlayerActor.TraitOrDefault<RgPlayer>() == null)
				return;

			if (self.Owner.PlayerActor.TraitOrDefault<RgPlayer>().Avatar == self)
			{
				if (e.DamageStateChanged && e.DamageState == DamageState.Dead)
				{

					if (e.Attacker != null && e.Attacker != self)
					{
						if (e.Attacker.Owner.PlayerRef.OwnsWorld)
							Chat(self.Owner.PlayerName + " got killed by " + e.Attacker.Owner.PlayerName + " (" + e.Attacker.Info.Traits.GetOrDefault<TooltipInfo>().Name +")");
						else
							Chat(self.Owner.PlayerName + " got killed by " + e.Attacker.Owner.PlayerName);
					}
					else if (e.Attacker == self)
						Chat(self.Owner.PlayerName + " committed suicide.");
					else
						Chat(self.Owner.PlayerName + " got killed.");
				}
			}
		}
	}
}
