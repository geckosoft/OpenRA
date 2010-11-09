using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;
using OpenRg.Traits.Inventory;

namespace OpenRg.Traits
{
	public class RGPlayerInfo : TraitInfo<RGPlayer>, ITraitPrerequisite<RGInventoryInfo>
	{

	}

	public class RGPlayer : ITick, INotifyDamage
	{
		public Actor Avatar;
		private Actor _container;

		public Actor Container
		{
			get { if (_container != null && (_container.Destroyed || !_container.IsInWorld)) return null; return _container; }
			set { _container = value; }
		}

		/* when in a vehicle */
		public ulong LastSpawn;
		public Player Player;
		public ulong Ticks;
		public Actor Actor
		{
			get
			{
				var res = Container == null ? Avatar : Container;
				if ((res != null) && (res.Destroyed || !res.IsInWorld))
					return null;

				return res;
			}
		}

		public PlayerResources ParentResources
		{
			get
			{
				if (Faction == null) return null;

				return Faction.PlayerActor.Trait<PlayerResources>();
			}
		}

		public Player Faction
		{
			get
			{
				if (Player == null)
					return null;

				foreach (var kv in Player.World.players)
				{
					Player player = kv.Value;

					if (player.Stances[Player] != Stance.Ally || Player.Stances[player] != Stance.Ally)
						continue; /* not an allied player - skip for sure */

					/* count units */
					var units =
						player.World.Queries.OwnedBy[player].Where(
							a => !a.Destroyed && a.Info.Name != "c17" && a.Owner == player && a != a.Owner.PlayerActor);

					if (units.Count() >= 2) // Every side must own at least 2 structures!
						return player;
				}

				return null; /* should not occur on valid maps! */
			}
		}

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageStateChanged && e.DamageState == DamageState.Dead && self == Avatar)
			{
				Avatar = null;
			}
		}

		#endregion

		#region ITick Members

		public void Tick(Actor self)
		{
			Ticks++;
			if (self == null)
				return;
			if (RGGame.LocalPlayer == null)
				RGGame.LocalPlayer = self.World.LocalPlayer != null ? self.World.LocalPlayer.PlayerActor.TraitOrDefault<RGPlayer>() : null;

			Player = self.Owner;

			if (Player.PlayerRef.NonCombatant || Player.PlayerRef.OwnsWorld)
				return; /* neutral player / core players */

			var myUnits =
				self.World.Queries.OwnedBy[self.Owner].Where(
					a => !a.Destroyed && a.Info.Name != "c17" && a.Owner == self.Owner && a != a.Owner.PlayerActor);
			
			if (Avatar != null && (Avatar.Destroyed))
			{
				Avatar = null;
			}

			if (Container != null && (Container.Destroyed || !Container.IsInWorld))
			{
				Container = null;
			}

			if (Avatar == null && Container == null && (LastSpawn == 0 || Ticks - LastSpawn > 25 * 10)) // Respawn every 10 seconds
			{
				/* find a spawn location */
				var location = GetSpawnPoint(self);
				if (location != null)
				{
					DoParadrop(self.Owner, (int2)location, new[] { "soldier" }); /* also sets the avatar! */
					LastSpawn = Ticks;

					RGGame.EVA("Respawning player ...");

					return;
				}
			}
			else if (myUnits.Count() >= 1 || Avatar != null || (Container != null && !Container.Destroyed && Container.IsInWorld))
			{
				LastSpawn = Ticks;
			}

			if (myUnits.Count() > 0 && Avatar == null)
				Avatar = myUnits.First();

			if (Avatar != null && Container != null && Container.Info.Name == "c17")
			{
				Container = null;
			}

			/* force selection (local player only) */
			if (Avatar != null && Avatar.Owner == Avatar.World.LocalPlayer && !Avatar.World.Selection.Contains(Avatar) &&
				(Container == null || !Container.IsInWorld))
			{
				Avatar.World.Selection.Add(Avatar.World, Avatar);
			}
			else if (Avatar != null && Avatar.Owner == Avatar.World.LocalPlayer && (Container != null && Container.IsInWorld) &&
					 !Avatar.World.Selection.Contains(Container))
			{
				Avatar.World.Selection.Add(Avatar.World, Container);
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

		public bool HasAvatar()
		{
			return Avatar != null && !Avatar.Destroyed;
		}

		public void SetAvatar(Actor actor)
		{
			Avatar = actor;
		}

		public void Refill()
		{
			if (Avatar == null || Avatar == Container)
				return;

			// Restore health
			Avatar.InflictDamage(Avatar, -Avatar.TraitOrDefault<Health>().MaxHP, null);

			// Refill relevant items
			Inventory.Refill();
		}

		public IEnumerable<Actor> ParentActors()
		{
			if (Faction == null)
				return null;

			return Faction.World.Queries.OwnedBy[Faction].Where(a => !a.Destroyed && a.IsInWorld);
		}

		public bool ParentHasActor(string actor)
		{
			if (Faction == null)
				return false;

			var units = Faction.World.Queries.OwnedBy[Faction].Where(a => !a.Destroyed && a.Info.Name == actor);

			return units.Count() > 0;
		}

		public RGInventory Inventory
		{
			get { return Player.PlayerActor.TraitOrDefault<RGInventory>(); }
		}

		public static RGPlayer Get(Player player)
		{
			if (player == null)
				return null;
			return player.PlayerActor.TraitOrDefault<RGPlayer>();
		}

		public static RGPlayer Get(Actor any)
		{
			if (any == null)
				return null;
			return any.Owner.PlayerActor.TraitOrDefault<RGPlayer>();
		}

		public static implicit operator RGPlayer(Actor a)
		{
			return Get(a);
		}

		public static implicit operator RGPlayer(Player a)
		{
			return Get(a);
		}

		private void DoParadrop(Player player, int2 p, string[] items)
		{
			int2 startPos = player.World.ChooseRandomEdgeCell();
			Container = Player.PlayerActor;
			player.World.AddFrameEndTask(w =>
			{
				Actor flare = null;
				
				var a = w.CreateActor("c17", new TypeDictionary
			                            		                               	{
			                            		                               		new LocationInit(startPos),
			                            		                               		new OwnerInit(player),
			                            		                               		new FacingInit(Util.GetFacing(p - startPos, 0)),
			                            		                               		new AltitudeInit(
			                            		                               			Rules.Info["c17"].Traits.Get<PlaneInfo>().
			                            		                               				CruiseAltitude),
			                            		                               	});

				a.CancelActivity();
				a.QueueActivity(new FlyCircle(p));
				a.Trait<RGParaDrop>().SetLZ(p, flare);
				Container = a;
				var cargo = a.Trait<Cargo>();
				foreach (string i in items)
					cargo.Load(a,
							   player.World.CreateActor(false, i.ToLowerInvariant(),
													   new TypeDictionary { new OwnerInit(a.Owner) }));
			});
		}
	}
}