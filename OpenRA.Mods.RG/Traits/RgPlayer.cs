﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.Rg.Traits.Inventory;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	internal class RgPlayerInfo : TraitInfo<RgPlayer>, ITraitPrerequisite<RgInventoryInfo>
	{
	}

	internal class RgPlayer : ITick, INotifyDamage
	{
		public Actor Avatar;
		public Actor Container; /* when in a vehicle */
		public ulong LastSpawn;
		public Player Player;
		public ulong Ticks;

		public PlayerResources ParentResources
		{
			get
			{
				if (Parent == null) return null;
				return Parent.PlayerActor.Trait<PlayerResources>();
			}
		}

		public Player Parent
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
					IEnumerable<Actor> units =
						player.World.Queries.OwnedBy[player].Where(
							a => !a.Destroyed && a.Info.Name != "c17" && a.Owner == player && a != a.Owner.PlayerActor);

					if (units.Count() >= 2) /* @todo Make sure that every Rg map has atleast 2 'destroyable' buildings! */
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
			if (self != null)
				Player = self.Owner;

			if (Player.PlayerRef.NonCombatant || Player.PlayerRef.OwnsWorld)
				return; /* neutral player / core players */
			IEnumerable<Actor> myUnits =
				self.World.Queries.OwnedBy[self.Owner].Where(
					a => !a.Destroyed && a.Info.Name != "c17" && a.Owner == self.Owner && a != a.Owner.PlayerActor);
			if (Avatar != null && (Avatar.Destroyed || !Avatar.IsInWorld))
			{
				Avatar = null;
			}

			if (Container != null && (Container.Destroyed || !Container.IsInWorld))
			{
				Container = null;
			}

			if (Avatar == null && Container == null && (LastSpawn == 0 || Ticks - LastSpawn > 25*10))
				/* once every 10 sec - make it an option */
			{
				/* find the location of the cyard */
				int2? location = GetSpawnPoint(self);
				if (location != null)
				{
					DoParadrop(self.Owner, (int2) location, new[] {"soldier"}); /* also sets the avatar! */
					LastSpawn = Ticks;

					Game.AddChatLine(Color.OrangeRed, "EVA", "Respawning player ...");

					return;
				}
			}
			else if (myUnits.Count() >= 1 || Avatar != null || (Container != null && !Container.Destroyed && Container.IsInWorld))
			{
				LastSpawn = Ticks;
			}

			if (myUnits.Count() > 0)
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
			if (Avatar == null)
				return;

			// Restore health
			Avatar.TraitOrDefault<Health>().HP = Avatar.TraitOrDefault<Health>().MaxHP;

			// Refill relevant items
			Inventory.Refill();
		}

		public IEnumerable<Actor> ParentActors()
		{
			if (Parent == null)
				return null;

			return Parent.World.Queries.OwnedBy[Parent].Where(a => !a.Destroyed && a.IsInWorld);
		}

		public bool ParentHasActor(string actor)
		{
			if (Parent == null)
				return false;

			IEnumerable<Actor> units = Parent.World.Queries.OwnedBy[Parent].Where(a => !a.Destroyed && a.Info.Name == actor);

			return units.Count() > 0;
		}

		public RgInventory Inventory
		{
			get { return Player.PlayerActor.TraitOrDefault<RgInventory>(); }
		}

		public static RgPlayer Get(Player player)
		{
			return player.PlayerActor.TraitOrDefault<RgPlayer>();
		}

		public static RgPlayer Get(Actor any)
		{
			return any.Owner.PlayerActor.TraitOrDefault<RgPlayer>();
		}

		private void DoParadrop(Player owner, int2 p, string[] items)
		{
			int2 startPos = owner.World.ChooseRandomEdgeCell();
			Container = Player.PlayerActor;
			owner.World.AddFrameEndTask(w =>
			                            	{
			                            		Actor flare = null;
			                            			/* w.CreateActor("flare", new TypeDictionary
                                                       {
                                                           new LocationInit(p),
                                                           new OwnerInit(owner),
                                                       });*/

			                            		Actor a = w.CreateActor("c17", new TypeDictionary
			                            		                               	{
			                            		                               		new LocationInit(startPos),
			                            		                               		new OwnerInit(owner),
			                            		                               		new FacingInit(Util.GetFacing(p - startPos, 0)),
			                            		                               		new AltitudeInit(
			                            		                               			Rules.Info["c17"].Traits.Get<PlaneInfo>().
			                            		                               				CruiseAltitude),
			                            		                               	});

			                            		a.CancelActivity();
			                            		a.QueueActivity(new FlyCircle(p));
			                            		a.Trait<RgParaDrop>().SetLZ(p, flare);
			                            		Container = a;
			                            		var cargo = a.Trait<Cargo>();
			                            		foreach (string i in items)
			                            			cargo.Load(a,
			                            			           owner.World.CreateActor(false, i.ToLowerInvariant(),
			                            			                                   new TypeDictionary {new OwnerInit(a.Owner)}));
			                            	});
		}

		public void SetContainer(Actor transport)
		{
			Container = transport;
		}
	}
}