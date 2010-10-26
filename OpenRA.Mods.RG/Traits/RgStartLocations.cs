using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgStartLocationsInfo : TraitInfo<RgStartLocations>
	{
		public readonly int InitialExploreRange = 5;
	}

	public class RgStartLocations : IWorldLoaded
	{
		public Dictionary<Player, int2> Start = new Dictionary<Player, int2>();

		#region IWorldLoaded Members

		public void WorldLoaded(World world)
		{
			List<int2> taken = world.LobbyInfo.Clients.Where(c => c.SpawnPoint != 0)
				.Select(c => world.Map.SpawnPoints.ElementAt(c.SpawnPoint - 1)).ToList();
			List<int2> available = world.Map.SpawnPoints.Except(taken).ToList();

			// Set spawn
			foreach (Session.Slot slot in world.LobbyInfo.Slots)
			{
				Session.Client client = world.LobbyInfo.Clients.FirstOrDefault(c => c.Slot == slot.Index);
				Player player = FindPlayerInSlot(world, slot);

				if (player == null) continue;

				int2 spid = (client == null || client.SpawnPoint == 0)
				            	? ChooseSpawnPoint(world, available, taken)
				            	: world.Map.SpawnPoints.ElementAt(client.SpawnPoint - 1);

				Start.Add(player, spid);
			}

			// Explore allied shroud
			foreach (var p in Start)
				if (p.Key == world.LocalPlayer || p.Key.Stances[world.LocalPlayer] == Stance.Ally)
					world.WorldActor.Trait<Shroud>().Explore(world, p.Value,
					                                         world.WorldActor.Info.Traits.Get<RgStartLocationsInfo>().
					                                         	InitialExploreRange);

			// Set viewport
			if (world.LocalPlayer != null && Start.ContainsKey(world.LocalPlayer))
			{
				int2? loc = GetSpawnPoint(world.LocalPlayer.PlayerActor);

				if (loc == null)
					Game.viewport.Center(Start[world.LocalPlayer]);
				else
					Game.viewport.Center((float2) loc);
			}
		}

		#endregion

		public int2? GetSpawnPoint(Actor self)
		{
			foreach (var kv in self.World.players)
			{
				Player player = kv.Value;

				if (self.Owner != player && self.Owner.Stances[player] == Stance.Ally)
				{
					/* spawn at any barrack (checking pyle, pyle_destroyed, hand, hand_destroyed) */
					Actor barracks =
						self.World.Queries.OwnedBy[player].Where(
							a =>
							!a.Destroyed && a.Owner == player &&
							(a.Info.Name == "pyle" || a.Info.Name == "pyle_destroyed" || a.Info.Name == "hand" ||
							 a.Info.Name == "hand_destroyed")).FirstOrDefault();

					if (barracks != null)
					{
						return barracks.Location; // +new int2(-5, -5);
					}
				}
			}

			return null;
		}

		private static Player FindPlayerInSlot(World world, Session.Slot slot)
		{
			return world.players.Values.FirstOrDefault(p => p.PlayerRef.Name == slot.MapPlayer);
		}

		private static int2 ChooseSpawnPoint(World world, List<int2> available, List<int2> taken)
		{
			if (available.Count == 0)
				throw new InvalidOperationException("No free spawnpoint.");

			int n = taken.Count == 0
			        	? world.SharedRandom.Next(available.Count)
			        	: available // pick the most distant spawnpoint from everyone else
			        	  	.Select((k, i) => Pair.New(k, i))
			        	  	.OrderByDescending(a => taken.Sum(t => (t - a.First).LengthSquared))
			        	  	.Select(a => a.Second)
			        	  	.First();

			int2 sp = available[n];
			available.RemoveAt(n);
			taken.Add(sp);
			return sp;
		}
	}
}