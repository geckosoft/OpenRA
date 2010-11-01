#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Network;

namespace OpenRA.Server
{
	static class Server
	{
		public static Connection[] Connections
		{
			get { return conns.ToArray(); }
		}

		static List<Connection> conns = new List<Connection>();
		static TcpListener listener = null;
		static Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();
		static Session lobbyInfo;
		internal static bool GameStarted = false;
		static string Name;
		static int ExternalPort;
		static int randomSeed;

		const int DownloadChunkInterval = 20000;
		const int DownloadChunkSize = 16384;

		const int MasterPingInterval = 60 * 3;	// 3 minutes. server has a 5 minute TTL for games, so give ourselves a bit
												// of leeway.

		public static int MaxSpectators = 4; // How many spectators to allow // @todo Expose this as an option

		static int lastPing = 0;
		static bool isInternetServer;
		static string masterServerUrl;
		static bool isInitialPing;
		static ModData ModData;
		static Map Map;

		public static void StopListening()
		{
			conns.Clear();
			GameStarted = false;
			try
			{
				listener.Stop();
			}catch(Exception)
			{
			}


			if (Game.Settings.Server.Extension != null)
				Game.Settings.Server.Extension.OnServerStop(true);
		}
		public static void ServerMain(ModData modData, Settings settings, string map)
		{
			Log.AddChannel("server", "server.log");

			isInitialPing = true;
			Server.masterServerUrl = settings.Server.MasterServer;
			isInternetServer = settings.Server.AdvertiseOnline;

			listener = new TcpListener(IPAddress.Any, settings.Server.ListenPort);
			Name = settings.Server.Name;
			ExternalPort = settings.Server.ExternalPort;
			randomSeed = (int)DateTime.Now.ToBinary();
			ModData = modData;

			lobbyInfo = new Session( settings.Game.Mods );
			lobbyInfo.GlobalSettings.RandomSeed = randomSeed;
			lobbyInfo.GlobalSettings.Map = map;
			lobbyInfo.GlobalSettings.AllowCheats = settings.Server.AllowCheats;
			lobbyInfo.GlobalSettings.ServerName = settings.Server.Name;
			
			LoadMap();	// populates the Session's slots, too.
			
			Log.Write("server", "Initial mods: ");
			foreach( var m in lobbyInfo.GlobalSettings.Mods )
				Log.Write("server","- {0}", m);
			
			Log.Write("server", "Initial map: {0}",lobbyInfo.GlobalSettings.Map);
			
			try
			{
				listener.Start();
			}
			catch (Exception)
			{
				throw new InvalidOperationException( "Unable to start server: port is already in use" );
			}

			if (Game.Settings.Server.Extension != null)
				Game.Settings.Server.Extension.OnServerStart();
				
			new Thread( _ =>
			{
				for( ; ; )
				{
					var checkRead = new ArrayList();
					checkRead.Add( listener.Server );
					foreach( var c in conns ) checkRead.Add( c.socket );

					Socket.Select( checkRead, null, null, MasterPingInterval * 10000 );

					foreach( Socket s in checkRead )
						if( s == listener.Server ) AcceptConnection();
						else
						{
							var con = conns.SingleOrDefault(c => c.socket == s);
							if (con != null)
								con.ReadData();
						}

					if (Environment.TickCount - lastPing > MasterPingInterval * 1000)
						PingMasterServer();
					else
						lock (masterServerMessages)
							while (masterServerMessages.Count > 0)
								SendChat(null, masterServerMessages.Dequeue());
					
					if (conns.Count() == 0)
					{
						listener.Stop();
						GameStarted = false;

						if (Game.Settings.Server.Extension != null)
							Game.Settings.Server.Extension.OnServerStop(false);
						break;
					}
				}
			} ) { IsBackground = true }.Start();


		}

		static Session.Slot MakeSlotFromPlayerReference(PlayerReference pr)
		{
			if (!pr.Playable) return null;
			return new Session.Slot
			{
				MapPlayer = pr.Name,
				Bot = null,	/* todo: allow the map to specify a bot class? */
				Closed = false,
			};
		}

		static void LoadMap()
		{
			Map = new Map(ModData.AvailableMaps[lobbyInfo.GlobalSettings.Map].Package);

			if (Game.Settings.Server.Extension != null)
				Game.Settings.Server.Extension.OnLoadMap(ref Map);

			lobbyInfo.Slots = Map.Players
				.Select(p => MakeSlotFromPlayerReference(p.Value))
				.Where(s => s != null)
				.Select((s, i) => { s.Index = i; return s; })
				.ToList();

			// Generate slots for spectators
			for (int i = 0; i < MaxSpectators; i++)
			{
				lobbyInfo.Slots.Add(new Session.Slot { Spectator = true, Index = lobbyInfo.Slots.Count(), MapPlayer = null, Bot = null});
			}
		}

		/* lobby rework todo: 
		 * 
		 *	- auto-assign players to slots
		 *	- show all the slots in the lobby ui.
		 *	- rework the game start so we actually use the slots.
		 *	- all players should be able to click an empty slot to move to it
		 *	- host should be able to choose whether a slot is open/closed/bot, with
		 *		potentially more than one choice of bot class.
		 *	- host should be able to kick a client from the lobby by closing its slot.
		 *	- change lobby commands so the host can configure bots, rather than
		 *		just configuring itself.
		 *	- "teams together" option for team games -- will eliminate most need
		 *		for manual spawnpoint choosing.
		 *	- pick sensible non-conflicting colors for bots.
		 */
		/// <summary>
		/// @todo The 256 is a big hack, need to look @ amount of available slots instead?
		/// </summary>
		static int ChooseFreePlayerIndex()
		{
			for (var i = 0; i < 256; i++) // 256 was 8, but thats bit low
				if (conns.All(c => c.PlayerIndex != i))
					return i;

			throw new InvalidOperationException("Already got 256 players");
		}

		static int ChooseFreeSlot()
		{
			return lobbyInfo.Slots.First(s => !s.Closed && s.Bot == null 
				&& !lobbyInfo.Clients.Any( c => c.Slot == s.Index )).Index;
		}

		static void AcceptConnection()
		{
			Socket newSocket = null;

			try
			{
				if (!listener.Server.IsBound) return;
				newSocket = listener.AcceptSocket();
			}catch
			{
				/* could have an exception here when listener 'goes away' when calling AcceptConnection! */
				/* alternative would be to use locking but the listener doesnt go away without a reason */
				return; 
			}


			var newConn = new Connection { socket = newSocket };

			
			if (Game.Settings.Server.Extension != null && !Game.Settings.Server.Extension.OnValidateConnection(GameStarted, newConn))
			{
				DropClient(newConn, new Exception() );

				return;
			}

			try
			{
				if (GameStarted)
				{
					Log.Write("server", "Rejected connection from {0}; game is already started.", 
						newConn.socket.RemoteEndPoint);
					newConn.socket.Close();
					return;
				}

				newConn.socket.Blocking = false;
				newConn.socket.NoDelay = true;

				// assign the player number.
				newConn.PlayerIndex = ChooseFreePlayerIndex();
				newConn.socket.Send(BitConverter.GetBytes(ProtocolVersion.Version));
				newConn.socket.Send(BitConverter.GetBytes(newConn.PlayerIndex));
				conns.Add(newConn);

				var defaults = new GameRules.PlayerSettings();
				
				var client = new Session.Client()
				{
					Index = newConn.PlayerIndex,
					Color1 = defaults.Color1,
					Color2 = defaults.Color2,
					Name = defaults.Name,
					Country = "random",
					State = Session.ClientState.NotReady,
					SpawnPoint = 0,
					Team = 0,
					Slot = ChooseFreeSlot(),
				};
				
				var slotData = lobbyInfo.Slots.FirstOrDefault( x => x.Index == client.Slot );
				if (slotData != null)
					SyncClientToPlayerReference(client, Map.Players[slotData.MapPlayer]);
				
				lobbyInfo.Clients.Add(client);

				Log.Write("server", "Client {0}: Accepted connection from {1}",
					newConn.PlayerIndex, newConn.socket.RemoteEndPoint);

				SendChat(newConn, "has joined the game.");

				SyncLobbyInfo();
			}
			catch (Exception e) { DropClient(newConn, e); }
		}

		public static void UpdateInFlightFrames(Connection conn)
		{
			if (conn.Frame != 0)
			{
				if (!inFlightFrames.ContainsKey(conn.Frame))
					inFlightFrames[conn.Frame] = new List<Connection> { conn };
				else
					inFlightFrames[conn.Frame].Add(conn);

				if (conns.All(c => inFlightFrames[conn.Frame].Contains(c)))
				{
					inFlightFrames.Remove(conn.Frame);
				}
			}
		}

		static void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			try
			{
				var ms = new MemoryStream();
				ms.Write( BitConverter.GetBytes( data.Length + 4 ) );
				ms.Write( BitConverter.GetBytes( client ) );
				ms.Write( BitConverter.GetBytes( frame ) );
				ms.Write( data );
				c.socket.Send( ms.ToArray() );
			}
			catch( Exception e ) { DropClient( c, e ); }
		}

		public static void DispatchOrders(Connection conn, int frame, byte[] data)
		{
			if (frame == 0 && conn != null)
				InterpretServerOrders(conn, data);
			else
			{
				var from = conn != null ? conn.PlayerIndex : 0;
				foreach (var c in conns.Except(conn).ToArray())
					DispatchOrdersToClient(c, from, frame, data);
			}
		}

		static void InterpretServerOrders(Connection conn, byte[] data)
		{
			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);

			try
			{
				for (; ; )
				{
					var so = ServerOrder.Deserialize(br);
					if (so == null) return;
					InterpretServerOrder(conn, so);
				}
			}
			catch (EndOfStreamException) { }
			catch (NotImplementedException) { }
		}

		public static void SyncClientToPlayerReference(Session.Client c, PlayerReference pr)
		{
			if (pr == null)
				return;
			if (pr.LockColor)
			{
				c.Color1 = pr.Color;
				c.Color2 = pr.Color2;
			}
			if (pr.LockRace)
				c.Country = pr.Race;
		}
		
		public static bool InterpretCommand(Connection conn, string cmd)
		{
			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "ready",
					s =>
					{
						// if we're downloading, we can't ready up.

						var client = GetClient(conn);
						if (client.State == Session.ClientState.NotReady)
							client.State = Session.ClientState.Ready;
						else if (client.State == Session.ClientState.Ready)
							client.State = Session.ClientState.NotReady;

						Log.Write("server", "Player @{0} is {1}",
							conn.socket.RemoteEndPoint, client.State);

						SyncLobbyInfo();
						
						if (Game.Settings.Server.Extension== null || Game.Settings.Server.Extension.OnReadyUp(conn, client))
						{
							if (conns.Count > 0 && conns.All(c => GetClient(c).State == Session.ClientState.Ready))
								InterpretCommand(conn, "startgame");
						}

						return true;
					}},
				{ "startgame", 
					s => 
					{
						GameStarted = true;
						foreach( var c in conns )
							foreach( var d in conns )
								DispatchOrdersToClient( c, d.PlayerIndex, 0x7FFFFFFF, new byte[] { 0xBF } );

						DispatchOrders(null, 0,
							new ServerOrder("StartGame", "").Serialize());

						if (Game.Settings.Server.Extension != null) Game.Settings.Server.Extension.OnStartGame();

						PingMasterServer();
						return true;
					}},
				{ "name", 
					s => 
					{
						Log.Write("server", "Player@{0} is now known as {1}", conn.socket.RemoteEndPoint, s);

						if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnNickChange(conn, GetClient(conn), s))
							GetClient(conn).Name = s;
						
						
						SyncLobbyInfo();
						return true;
					}},
				{ "lag",
					s =>
					{
						int lag;
						if (!int.TryParse(s, out lag)) { Log.Write("server", "Invalid order lag: {0}", s); return false; }

						Log.Write("server", "Order lag is now {0} frames.", lag);

						lobbyInfo.GlobalSettings.OrderLatency = lag;
						SyncLobbyInfo();
						return true;
					}},
				{ "race",
					s => 
					{	
						if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnRaceChange(conn, GetClient(conn), s))
							GetClient(conn).Country = s;

						SyncLobbyInfo();
						return true;
					}},	
				{ "spectator",
					s =>
						{
						var slotData = lobbyInfo.Slots.Where(ax => ax.Spectator && !lobbyInfo.Clients.Any(l => l.Slot == ax.Index)).FirstOrDefault();
						if (slotData == null)
							return true;

						var cl = GetClient(conn);
						
						if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnSlotChange(conn, cl, slotData, Map))
						{
							cl.Slot = slotData.Index;

							SyncClientToPlayerReference(cl, slotData.MapPlayer != null ? Map.Players[slotData.MapPlayer] : null);
						}

						SyncLobbyInfo();
						return true;
					}},	
				{ "team",
					s => 
					{
						int team;
						if (!int.TryParse(s, out team)) { Log.Write("server", "Invalid team: {0}", s ); return false; }
						if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnTeamChange(conn, GetClient(conn), team))
						{
							GetClient(conn).Team = team;
						}
						SyncLobbyInfo();
						return true;
					}},	
				{ "spawn",
					s => 
					{
						int spawnPoint;
						if (!int.TryParse(s, out spawnPoint) || spawnPoint < 0 || spawnPoint > 8) //TODO: SET properly!
						{
							Log.Write("server", "Invalid spawn point: {0}", s);
							return false;
						}
						
						if (lobbyInfo.Clients.Where( c => c != GetClient(conn) ).Any( c => (c.SpawnPoint == spawnPoint) && (c.SpawnPoint != 0) ))
						{
							SendChatTo( conn, "You can't be at the same spawn point as another player" );
							return true;
						}
						if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnSpawnpointChange(conn, GetClient(conn), spawnPoint))
						{
							GetClient(conn).SpawnPoint = spawnPoint;
						}
						SyncLobbyInfo();
						return true;
					}},
				{ "color",
					s =>
					{
						var c = s.Split(',').Select(cc => int.Parse(cc)).ToArray();
						
						if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnColorChange(conn, GetClient(conn), Color.FromArgb(c[0], c[1], c[2]), Color.FromArgb(c[3], c[4], c[5])))
						{
							GetClient(conn).Color1 = Color.FromArgb(c[0], c[1], c[2]);
							GetClient(conn).Color2 = Color.FromArgb(c[3], c[4], c[5]);
						}
						SyncLobbyInfo();		
						return true;
					}},
				{ "slot",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null || slotData.Closed || slotData.Bot != null 
							|| lobbyInfo.Clients.Any( c => c.Slot == slot ))
							return false;

						var cl = GetClient(conn);
						
						if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnSlotChange(conn, cl, slotData, Map))
						{
							cl.Slot = slot;

							SyncClientToPlayerReference(cl, slotData.MapPlayer != null ? Map.Players[slotData.MapPlayer] : null);
						}

						SyncLobbyInfo();
						return true;
					}},
				{ "slot_close",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Closed = true;
						slotData.Bot = null;

						/* kick any player that's in the slot */
						var occupant = lobbyInfo.Clients.FirstOrDefault( c => c.Slot == slotData.Index );
						if (occupant != null)
						{
							var occupantConn = conns.FirstOrDefault( c => c.PlayerIndex == occupant.Index );
							if (occupantConn != null)
								DropClient( occupantConn, new Exception() );
						}

						SyncLobbyInfo();
						return true;
					}},
				{ "slot_open",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Closed = false;
						slotData.Bot = null;

						SyncLobbyInfo();
						return true;
					}},
				{ "slot_bot",
					s =>
					{
						var parts = s.Split(' ');

						if (parts.Length != 2)
						{
							SendChatTo( conn, "Malformed slot_bot command" );
							return true;
						}

						int slot;
						if (!int.TryParse(parts[0], out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Bot = parts[1];

						SyncLobbyInfo();
						return true;
					}},
				{ "map",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							SendChatTo( conn, "Only the host can change the map" );
							return true;
						}
						lobbyInfo.GlobalSettings.Map = s;			
						LoadMap();

						foreach(var client in lobbyInfo.Clients)
						{
							client.SpawnPoint = 0;
							var slotData = lobbyInfo.Slots.FirstOrDefault( x => x.Index == client.Slot );
							if (slotData != null)
								SyncClientToPlayerReference(client, Map.Players[slotData.MapPlayer]);
				
							client.State = Session.ClientState.NotReady;
						}
						
						SyncLobbyInfo();
						return true;
					}},
				{ "mods",
					s =>
					{
						// @todo Remove? Does not seem to be used.
						if (conn.PlayerIndex != 0)
						{
							SendChatTo( conn, "Only the host can set that option" );
							return true;
						}
						var args = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
						lobbyInfo.GlobalSettings.Mods = args.GetRange(0,args.Count - 1).ToArray();
						lobbyInfo.GlobalSettings.Map = args.Last();
						SyncLobbyInfo();
						return true;
					}},
				{ "lockteams",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							SendChatTo( conn, "Only the host can set that option" );
							return true;
						}
						
						bool.TryParse(s, out lobbyInfo.GlobalSettings.LockTeams);
						SyncLobbyInfo();
						return true;
					}},
			};

			var cmdName = cmd.Split(' ').First();
			var cmdValue = string.Join(" ", cmd.Split(' ').Skip(1).ToArray());

			Func<string,bool> a;
			if (!dict.TryGetValue(cmdName, out a))
				return false;

			Log.Write("server", "Client {0} sent server command: {1}", conn.PlayerIndex, cmd );
			return a(cmdValue);
		}

		static void SendChatTo(Connection conn, string text)
		{
			DispatchOrdersToClient(conn, 0, 0,
				new ServerOrder("Chat", text).Serialize());
		}

		static void SendChat(Connection asConn, string text)
		{
			DispatchOrders(asConn, 0, new ServerOrder("Chat", text).Serialize());
		}

		static void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			switch (so.Name)
			{
				case "Command":
				{
					if(GameStarted)
						SendChatTo(conn, "Cannot change state when game started.");
					else if (GetClient(conn).State == Session.ClientState.Ready && !(so.Data == "ready" || so.Data == "startgame") )
						SendChatTo(conn, "Cannot change state when marked as ready.");
					else if (!InterpretCommand(conn, so.Data))
					{
						Log.Write("server", "Bad server command: {0}", so.Data);
						SendChatTo(conn, "Bad server command.");
					};
				}
				break;

				case "Chat": 
				case "TeamChat":
				if (Game.Settings.Server.Extension == null || Game.Settings.Server.Extension.OnChat(conn, so.Data, so.Name == "TeamChat"))
					foreach (var c in conns.Except(conn).ToArray())
						DispatchOrdersToClient(c, GetClient(conn).Index, 0, so.Serialize());
				break;
			}
		}

		static Session.Client GetClient(Connection conn)
		{
			return lobbyInfo.Clients.First(c => c.Index == conn.PlayerIndex);
		}

		public static void DropClient(Connection toDrop, Exception e)
		{
			conns.Remove(toDrop);
			SendChat(toDrop, "Connection Dropped");

			lobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);

			DispatchOrders( toDrop, toDrop.MostRecentFrame, new byte[] { 0xbf } );
			
			if (conns.Count != 0)
				SyncLobbyInfo();
		}

		static void SyncLobbyInfo()
		{
			if (Game.Settings.Server.Extension != null)
				Game.Settings.Server.Extension.OnLobbySync(lobbyInfo, GameStarted);

			if (!GameStarted)	/* don't do this while the game is running, it breaks things. */
				DispatchOrders(null, 0,
					new ServerOrder("SyncInfo", lobbyInfo.Serialize()).Serialize());

			PingMasterServer();
		}

		static volatile bool isBusy;
		static Queue<string> masterServerMessages = new Queue<string>();
		static void PingMasterServer()
		{
			if (isBusy || !isInternetServer) return;

			if (Game.Settings.Server.Extension != null && !Game.Settings.Server.Extension.OnPingMasterServer(lobbyInfo, GameStarted))
				return;

			lastPing = Environment.TickCount;
			isBusy = true;

			Action a = () =>
				{
					try
					{
						var url = "ping.php?port={0}&name={1}&state={2}&players={3}&mods={4}&map={5}";
						if (isInitialPing) url += "&new=1";

						using (var wc = new WebClient())
						{
							 wc.DownloadData(
								masterServerUrl + url.F(
								ExternalPort, Uri.EscapeUriString(Name),
								GameStarted ? 2 : 1,	// todo: post-game states, etc.
								lobbyInfo.Clients.Count,
								string.Join(",", lobbyInfo.GlobalSettings.Mods),
								lobbyInfo.GlobalSettings.Map));

							if (isInitialPing)
							{
								isInitialPing = false;
								lock (masterServerMessages)
									masterServerMessages.Enqueue("Master server communication established.");
							}
						}
					}
					catch(Exception ex)
					{
						Log.Write("server", ex.ToString());
						lock( masterServerMessages )
							masterServerMessages.Enqueue( "Master server communication failed." );
					}

					isBusy = false;
				};

			a.BeginInvoke(null, null);
		}
	}
}
