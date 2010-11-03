using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgCreateMPPlayersInfo : TraitInfo<RgCreateMPPlayers>
	{
	}

	public class RgCreateMPPlayers : ICreatePlayers
	{
		#region ICreatePlayers Members

		public void CreatePlayers(World w)
		{
			int playerIndex = 0;
			int mapPlayerIndex = -1; // todo: unhack this, but people still rely on it.

			// create the unplayable map players -- neutral, shellmap, scripted, etc.
			foreach (var kv in w.Map.Players.Where(p => !p.Value.Playable))
			{
				var player = new Player(w, kv.Value, mapPlayerIndex--);
				w.AddPlayer(player);
				if (kv.Value.OwnsWorld)
					w.WorldActor.Owner = player;
			}

			// create the players which are bound through slots.
			foreach (Session.Slot slot in w.LobbyInfo.Slots)
			{
				Session.Client client = w.LobbyInfo.Clients.FirstOrDefault(c => c.Slot == slot.Index && slot.MapPlayer != null);
				if (client != null)
				{
					/* spawn a real player in this slot. */
					var player = new Player(w, client, w.Map.Players[slot.MapPlayer], playerIndex++);
					w.AddPlayer(player);
					if (client.Index == Game.LocalClientId)
					{
						w.SetLocalPlayer(player.Index); // bind this one to the local player.
						w.WorldActor.Trait<Shroud>().ExploreAll(w);
					}
				}
				else if (slot.Bot != null)
				{
					/* spawn a bot in this slot, "owned" by the host */

					/* pick a random color for the bot */
					var hue = (float) w.SharedRandom.NextDouble();
					w.Map.Players[slot.MapPlayer].Color = PlayerColorRemap.ColorFromHSL(hue, 1.0f, 0.7f);
					w.Map.Players[slot.MapPlayer].Color2 = PlayerColorRemap.ColorFromHSL(hue, 1.0f, 0.2f);

					/* todo: pick a random name from the pool */

					var player = new Player(w, w.Map.Players[slot.MapPlayer], playerIndex++);
					w.AddPlayer(player);

					/* todo: only activate the bot option that's selected! */
					if (Game.IsHost)
						foreach (IBot bot in player.PlayerActor.TraitsImplementing<IBot>())
							bot.Activate(player);

					/* a bit of a hack */
					player.IsBot = true;
				}
			}

			foreach (Player p in w.players.Values)
				foreach (Player q in w.players.Values)
				{
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = ChooseInitialStance(p, q);
				}
		}

		#endregion

		private static Stance ChooseInitialStance(Player p, Player q)
		{
			if (p == q) return Stance.Ally;

			if (p.Country.Race == q.Country.Race)
				return Stance.Ally;
			if (p.Country.Race != q.Country.Race && p.InternalName != "Neutral" && q.InternalName != "Neutral")
				return Stance.Enemy;
			if (p.PlayerRef.Allies.Contains(q.InternalName))
				return Stance.Ally;
			if (p.PlayerRef.Enemies.Contains(q.InternalName))
				return Stance.Enemy;


			// Hack: All map players are neutral wrt everyone else
			if (p.Index < 0 || q.Index < 0) return Stance.Neutral;

			if (p.IsBot ^ q.IsBot)
				return Stance.Enemy; // bots and humans hate each other

			Session.Client pc = GetClientForPlayer(p);
			Session.Client qc = GetClientForPlayer(q);

			return pc.Team != 0 && pc.Team == qc.Team
			       	? Stance.Ally
			       	: Stance.Enemy;
		}

		private static Session.Client GetClientForPlayer(Player p)
		{
			return p.World.LobbyInfo.ClientWithIndex(p.ClientIndex);
		}
	}
}