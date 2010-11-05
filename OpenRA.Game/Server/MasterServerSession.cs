using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Support;

namespace OpenRA.Server
{
	static partial class Server
	{
		public class GameSession : MasterResult
		{
			public int OwnerId = 0;
			public int GameId = 0;
			public string GameKey = "";
		}

		public class MasterServerSession
		{
			public int LastPing { get; private set; }
			public bool IsInitialPing { get; private set; }
			public bool IsBusy { get { return _isBusy; } }
			private volatile bool _isBusy = false;
			//public int GameId = 0;
			//public string GameKey = "";
			public readonly int PingInterval = 60 * 3;	// 3 minutes. server has a 5 minute TTL for games, so give ourselves a bit of leeway.
			public bool IsRegistered { get; protected set; }
			public GameSession Session = new GameSession();
			public readonly string SHAREDIV = "Jpa45SNtUHK2C9dy";

			public MasterServerSession()
			{
				LastPing = 0;
				IsInitialPing = true;
				IsRegistered = false;
				_isBusy = false;
			}

			public void FinalPing( bool forcedStop )
			{
				
			}

			public void Register()
			{
				if (IsBusy || IsRegistered) return;

				_isBusy = true;

				Action a = () =>
				{
					try
					{
						string url = Game.Settings.Server.MasterServer + "master.php?do=register";

							// Assign the post-data
						var data = new NameValueCollection();
						data["user_id"] = Game.User.UserId + "";
						data["user_key"] = Game.User.UserKey;

						// Perform the post
						string reply = HttpClient.Post(url, data);

						// Attempt to load the yaml
						List<MiniYamlNode> yaml = MiniYaml.FromString(reply);

						// Attempt to apply the yaml onto the current class
						FieldLoader.Load(Session, yaml[0].Value);

						lock (masterServerMessages)
							masterServerMessages.Enqueue("Master server communication: Registered game.");

						lobbyInfo.GlobalSettings.GameId = Session.GameId;
						lobbyInfo.GlobalSettings.IsInternetGame = true;
						IsRegistered = true;
					}
					catch (Exception ex)
					{
						Log.Write("server", ex.ToString());
						// Could not register (something bad happened... internet down?)
						lock (masterServerMessages)
							masterServerMessages.Enqueue("Master server communication failed: Could not register game.");

					}
					_isBusy = false;
				};

				a.BeginInvoke(null, null);
			}
			public void Ping()
			{
				if (_isBusy || !IsInternetServer || !IsRegistered) return;

				if (!E(e => e.OnPingMasterServer(lobbyInfo, GameStarted)))
					return;

				LastPing = Environment.TickCount;
				_isBusy = true;

				Action a = () =>
				{
					try
					{
						var url = "ping.php?port={0}&name={1}&state={2}&players={3}&mods={4}&map={5}";
						if (IsInitialPing) url += "&new=1";

						using (var wc = new WebClient())
						{
							wc.DownloadData(
							   masterServerUrl + url.F(
							   ExternalPort, Uri.EscapeUriString(Name),
							   GameStarted ? 2 : 1,	// todo: post-game states, etc.
							   lobbyInfo.Clients.Count,
							   string.Join(",", lobbyInfo.GlobalSettings.Mods),
							   lobbyInfo.GlobalSettings.Map));

							if (IsInitialPing)
							{
								IsInitialPing = false;
								lock (masterServerMessages)
									masterServerMessages.Enqueue("Master server communication established.");
							}
						}
					}
					catch (Exception ex)
					{
						Log.Write("server", ex.ToString());
						lock (masterServerMessages)
							masterServerMessages.Enqueue("Master server communication failed: Could not ping");
					}

					_isBusy = false;
				};

				a.BeginInvoke(null, null);
			}

			public void Validate( Connection conn, string encryptedUserId )
			{
				var client = lobbyInfo.Clients.Where(c => c.Index == conn.PlayerIndex).SingleOrDefault();

				if (client == null)
				{
					DropClient(conn, "Dropped: Does not have a client."); // Can this even occur?
					return;
				}

				if (client.UserId != 0)
				{
					DropClient(conn, "Dropped: Was already validated."); // HAX!
					return;
				}

				if (Session.GameKey == "") return;

				// Decrypt the data
				var possibleUserId = Encryption.Decrypt(encryptedUserId, Session.GameKey, SHAREDIV);
				int userId = 0;
				if (!int.TryParse(possibleUserId, out userId))
				{
					DropClient(conn, "Dropped: Validation failed."); // HAX!
					return;
				}

				// Validate it against the master server
				Action a = () =>
				{
					try
					{
						string url = Game.Settings.Server.MasterServer + "master.php?do=validate";

						// Assign the post-data
						var data = new NameValueCollection();
						data["user_id"] = "" + userId;
						data["game_id"] = "" + Session.GameId;

						// Perform the post
						string reply = HttpClient.Post(url, data);

						// Attempt to load the yaml
						List<MiniYamlNode> yaml = MiniYaml.FromString(reply);

						// Attempt to apply the yaml onto the current class
						var result = FieldLoader.Load<MasterResult>(yaml[0].Value);

						if (result.Success)
						{
							client.UserId = userId;
							client.State = Network.Session.ClientState.NotReady;
							SyncLobbyInfo();

							lock (masterServerMessages)
								masterServerMessages.Enqueue("Master server communication: " + client.Name + " is authenticated.");
						}
						else
							DropClient(conn, "Dropped: Validation failed."); // HAX!
					}
					catch
					{
						DropClient(conn, "Dropped: Validation failed."); // HAX!
					}
				};

				a.BeginInvoke(null, null);
			}
		}
	}
}
