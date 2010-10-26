using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Rg.Widgets.Delegates
{
	public class RgLobbyDelegate : IWidgetDelegate
	{
		public static Color CurrentColorPreview1;
		public static Color CurrentColorPreview2;
		private readonly Dictionary<string, string> CountryNames;
		private readonly ButtonWidget GDIButton;
		private readonly LabelWidget GDILabel;
		private readonly Widget LocalPlayerTemplate;
		private readonly ButtonWidget NodButton;
		private readonly LabelWidget NodLabel;
		private readonly Widget Players;
		private readonly OrderManager orderManager;
		private Widget EmptySlotTemplate, EmptySlotTemplateHost;
		private Map Map;
		private string MapUid;
		private Widget PickSideTemplate;
		private Widget RemotePlayerTemplate;
		private bool hasJoined;

		[ObjectCreator.UseCtorAttribute]
		internal RgLobbyDelegate([ObjectCreator.ParamAttribute("widget")] Widget lobby,
		                         [ObjectCreator.ParamAttribute] OrderManager orderManager)
		{
			this.orderManager = orderManager;
			Game.LobbyInfoChanged += UpdateCurrentMap;
			UpdateCurrentMap();

			CurrentColorPreview1 = Game.Settings.Player.Color1;
			CurrentColorPreview2 = Game.Settings.Player.Color2;

			Players = lobby.GetWidget("PLAYERS");
			NodLabel = (LabelWidget) lobby.GetWidget("NOD_LABEL");
			GDILabel = (LabelWidget) lobby.GetWidget("GDI_LABEL");


			NodButton = (ButtonWidget) lobby.GetWidget("NOD_BUTTON");
			GDIButton = (ButtonWidget) lobby.GetWidget("GDI_BUTTON");


			// orderManager.IssueOrder(Order.Command("race " + nextCountry));
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");

			/* assign the event handlers */
			NodButton.OnMouseUp = _ =>
			                      	{
			                      		if (CountPlayers("nod") > CountPlayers("gdi") + 1)
			                      			return false; /* too many nods already (allow 1 overflow) */


			                      		orderManager.IssueOrder(Order.Command("race nod"));

			                      		return true;
			                      	};


			GDIButton.OnMouseUp = _ =>
			                      	{
			                      		if (CountPlayers("gdi") > CountPlayers("nod") + 1)
			                      			return false; /* too many nods already (allow 1 overflow) */


			                      		orderManager.IssueOrder(Order.Command("race gdi"));

			                      		return true;
			                      	};
			// players assigned / slots available
			GDILabel.GetText =
				() => { return CountPlayers("gdi") + " / " + Math.Round((float) (orderManager.LobbyInfo.Slots.Count/2 + 1), 0); };

			// players assigned / slots available
			NodLabel.GetText =
				() => { return CountPlayers("nod") + " / " + Math.Round((float) (orderManager.LobbyInfo.Slots.Count/2 + 1), 0); };

			var mapPreview = lobby.GetWidget<MapPreviewWidget>("LOBBY_MAP_PREVIEW");
			mapPreview.Map = () => Map;
			mapPreview.OnSpawnClick = sp =>
			                          	{
			                          		if (orderManager.LocalClient.State == Session.ClientState.Ready) return;
			                          		bool owned = orderManager.LobbyInfo.Clients.Any(c => c.SpawnPoint == sp);
			                          		if (sp == 0 || !owned)
			                          			orderManager.IssueOrder(Order.Command("spawn {0}".F(sp)));
			                          	};

			mapPreview.SpawnColors = () =>
			                         	{
			                         		IEnumerable<int2> spawns = Map.SpawnPoints;
			                         		var sc = new Dictionary<int2, Color>();

			                         		for (int i = 1; i <= spawns.Count(); i++)
			                         		{
			                         			Session.Client client =
			                         				orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.SpawnPoint == i);
			                         			if (client == null)
			                         				continue;
			                         			sc.Add(spawns.ElementAt(i - 1), client.Color1);
			                         		}
			                         		return sc;
			                         	};

			CountryNames = Rules.Info["world"].Traits.WithInterface<CountryInfo>().ToDictionary(a => a.Race, a => a.Name);
			CountryNames.Add("random", "Random");

			Widget mapButton = lobby.GetWidget("CHANGEMAP_BUTTON");
			mapButton.OnMouseUp = mi =>
			                      	{
			                      		Widget.OpenWindow("MAP_CHOOSER",
			                      		                  new Dictionary<string, object>
			                      		                  	{{"orderManager", orderManager}, {"mapName", MapUid}});
			                      		return true;
			                      	};

			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;

			Widget disconnectButton = lobby.GetWidget("DISCONNECT_BUTTON");
			disconnectButton.OnMouseUp = mi =>
			                             	{
			                             		Game.Disconnect();
			                             		return true;
			                             	};

			var lockTeamsCheckbox = lobby.GetWidget<CheckboxWidget>("LOCKTEAMS_CHECKBOX");
			lockTeamsCheckbox.IsVisible = () => lockTeamsCheckbox.Visible && true;
			lockTeamsCheckbox.Checked = () => orderManager.LobbyInfo.GlobalSettings.LockTeams;
			lockTeamsCheckbox.OnMouseDown = mi =>
			                                	{
			                                		if (Game.IsHost)
			                                			orderManager.IssueOrder(Order.Command(
			                                				"lockteams {0}".F(!orderManager.LobbyInfo.GlobalSettings.LockTeams)));
			                                		return true;
			                                	};

			Widget startGameButton = lobby.GetWidget("START_GAME_BUTTON");
			startGameButton.OnMouseUp = mi =>
			                            	{
			                            		mapButton.Visible = false;
			                            		disconnectButton.Visible = false;
			                            		lockTeamsCheckbox.Visible = false;
			                            		orderManager.IssueOrder(Order.Command("startgame"));
			                            		return true;
			                            	};

			// Todo: Only show if the map requirements are met for player slots
			startGameButton.IsVisible = () => Game.IsHost;

			Game.LobbyInfoChanged += JoinedServer;
			Game.ConnectionStateChanged += ResetConnectionState;
			Game.LobbyInfoChanged += UpdatePlayerList;

			Game.AddChatLine += lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine;

			bool teamChat = false;
			var chatLabel = lobby.GetWidget<LabelWidget>("LABEL_CHATTYPE");
			var chatTextField = lobby.GetWidget<TextFieldWidget>("CHAT_TEXTFIELD");
			chatTextField.OnEnterKey = () =>
			                           	{
			                           		if (chatTextField.Text.Length == 0)
			                           			return true;

			                           		Order order = (teamChat)
			                           		              	? Order.TeamChat(chatTextField.Text)
			                           		              	: Order.Chat(chatTextField.Text);
			                           		orderManager.IssueOrder(order);
			                           		chatTextField.Text = "";
			                           		return true;
			                           	};

			chatTextField.OnTabKey = () =>
			                         	{
			                         		teamChat ^= true;
			                         		chatLabel.Text = (teamChat) ? "Team:" : "Chat:";
			                         		return true;
			                         	};
		}

		private string PickRace()
		{
			return (CountPlayers("gdi") > CountPlayers("nod")) ? "nod" : "gdi";
		}

		private void UpdatePlayerColor(float hf, float sf, float lf, float r)
		{
			Color c1 = PlayerColorRemap.ColorFromHSL(hf, sf, lf);
			Color c2 = PlayerColorRemap.ColorFromHSL(hf, sf, r*lf);

			Game.Settings.Player.Color1 = c1;
			Game.Settings.Player.Color2 = c2;
			Game.Settings.Save();
			orderManager.IssueOrder(Order.Command("color {0},{1},{2},{3},{4},{5}".F(c1.R, c1.G, c1.B, c2.R, c2.G, c2.B)));
		}

		private void UpdateColorPreview(float hf, float sf, float lf, float r)
		{
			CurrentColorPreview1 = PlayerColorRemap.ColorFromHSL(hf, sf, lf);
			CurrentColorPreview2 = PlayerColorRemap.ColorFromHSL(hf, sf, r*lf);
		}

		/*
		int CountPlayerSlots()
		{
			//return orderManager.LobbyInfo.Slots.Where(a => a.Index)
		}*/

		private void UpdateCurrentMap()
		{
			if (MapUid == orderManager.LobbyInfo.GlobalSettings.Map) return;
			MapUid = orderManager.LobbyInfo.GlobalSettings.Map;
			Map = new Map(Game.modData.AvailableMaps[MapUid].Package);

			var title = Widget.RootWidget.GetWidget<LabelWidget>("LOBBY_TITLE");
			title.Text = "OpenRg Multiplayer Lobby - " + orderManager.LobbyInfo.GlobalSettings.ServerName;
		}

		private void JoinedServer()
		{
			if (hasJoined)
				return;
			hasJoined = true;

			if (orderManager.LocalClient.Name != Game.Settings.Player.Name)
				orderManager.IssueOrder(Order.Command("name " + Game.Settings.Player.Name));

			Color c1 = Game.Settings.Player.Color1;
			Color c2 = Game.Settings.Player.Color2;

			if (orderManager.LocalClient.Color1 != c1 || orderManager.LocalClient.Color2 != c2)
				orderManager.IssueOrder(Order.Command("color {0},{1},{2},{3},{4},{5}".F(c1.R, c1.G, c1.B, c2.R, c2.G, c2.B)));
		}

		private void ResetConnectionState(OrderManager orderManager)
		{
			if (orderManager.Connection.ConnectionState == ConnectionState.PreConnecting)
				hasJoined = false;
		}

		private Session.Client GetClientInSlot(Session.Slot slot)
		{
			return orderManager.LobbyInfo.ClientInSlot(slot);
		}

		private int CountPlayers(string race)
		{
			int count = 0;

			foreach (Session.Slot slot in orderManager.LobbyInfo.Slots)
			{
				Session.Slot s = slot;
				Session.Client c = GetClientInSlot(s);
				Widget template;

				if (c != null && c.Country == race)
				{
					count++;
				}
			}

			return count;
		}

		private void UpdatePlayerList()
		{
			// This causes problems for people who are in the process of editing their names (the widgets vanish from beneath them)
			// Todo: handle this nicer
			Players.Children.Clear();

			int offset = 0;

			foreach (Session.Slot slot in orderManager.LobbyInfo.Slots)
			{
				Session.Slot s = slot;
				Session.Client c = GetClientInSlot(s);
				Widget template;

				if (c != null && c.Index == orderManager.LocalClient.Index && c.State != Session.ClientState.Ready)
				{
					template = LocalPlayerTemplate.Clone();
					var name = template.GetWidget<TextFieldWidget>("NAME");
					name.Text = c.Name;
					name.OnEnterKey = () =>
					                  	{
					                  		name.Text = name.Text.Trim();
					                  		if (name.Text.Length == 0)
					                  			name.Text = c.Name;

					                  		name.LoseFocus();
					                  		if (name.Text == c.Name)
					                  			return true;

					                  		orderManager.IssueOrder(Order.Command("name " + name.Text));
					                  		Game.Settings.Player.Name = name.Text;
					                  		Game.Settings.Save();
					                  		return true;
					                  	};
					name.OnLoseFocus = () => name.OnEnterKey();

					var color = template.GetWidget<ButtonWidget>("COLOR");

					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					PlayerReference p =
						Map.Players.Where(a => a.Value.Name.ToLower() == c.Country.ToLower()).Select(a => a.Value).SingleOrDefault();
					if (p != null)
					{
						c.Color1 = p.Color;
						c.Color2 = p.Color2;
					}
					colorBlock.GetColor = () => c.Color1;

					var faction = template.GetWidget<ButtonWidget>("FACTION");
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () =>
					                      	{
					                      		if (c.Country.ToLower() == "random")
					                      		{
					                      			//c.Country = PickRace();
					                      			orderManager.IssueOrder(Order.Command("race " + PickRace()));
					                      		}
					                      		return CountryNames[c.Country];
					                      	};
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => c.Country;
					factionflag.GetImageCollection = () => "flags";

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.Checked = () => c.State == Session.ClientState.Ready;
					status.OnMouseDown = CycleReady;
				}
				else
				{
					/* only processes local! */
					continue;
				}
				template.Id = "SLOT_{0}".F(s.Index);
				template.Parent = Players;

				template.Bounds = new Rectangle(0, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				Players.AddChild(template);

				offset += template.Bounds.Height;
			}
		}

		private bool CycleReady(MouseInput mi)
		{
			orderManager.IssueOrder(Order.Command("ready"));
			return true;
		}
	}
}