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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public abstract class AttackBaseInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string PrimaryWeapon = null;
		[WeaponReference]
		public readonly string SecondaryWeapon = null;
		public readonly int Recoil = 0;
		public readonly int[] PrimaryLocalOffset = { };
		public readonly int[] SecondaryLocalOffset = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly bool MuzzleFlash = false;
		public readonly int FireDelay = 0;

		public readonly bool AlignIdleTurrets = false;
		public readonly bool CanAttackGround = true;

		public readonly float ScanTimeAverage = 2f;
		public readonly float ScanTimeSpread = .5f;

		public abstract object Create(ActorInitializer init);
	}

	public abstract class AttackBase : IIssueOrder, IResolveOrder, ITick, IExplodeModifier, IOrderVoice
	{
		[Sync]
		int nextScanTime = 0;

		public bool IsAttacking { get; internal set; }
		public Target target;

		public List<Weapon> Weapons = new List<Weapon>();
		public List<Turret> Turrets = new List<Turret>();

		readonly Actor self;

		public AttackBase(Actor self)
		{
			this.self = self;
			var info = self.Info.Traits.Get<AttackBaseInfo>();

			Turrets.Add(new Turret(info.PrimaryOffset));
			if (info.SecondaryOffset != null)
				Turrets.Add(new Turret(info.SecondaryOffset));

			if (info.PrimaryWeapon != null)
				Weapons.Add(new Weapon(info.PrimaryWeapon, 
					Turrets[0], info.PrimaryLocalOffset));

			if (info.SecondaryWeapon != null)
				Weapons.Add(new Weapon(info.SecondaryWeapon, 
					info.SecondaryOffset != null ? Turrets[1] : Turrets[0], info.SecondaryLocalOffset));
		}

		protected virtual bool CanAttack(Actor self)
		{
			if (!target.IsValid) return false;
			if (Weapons.All(w => w.IsReloading)) return false;
			if (self.TraitsImplementing<IDisable>().Any(d => d.Disabled)) return false;

			return true;
		}

		public bool ShouldExplode(Actor self) { return !IsReloading(); }

		public bool IsReloading() { return Weapons.Any(w => w.IsReloading); }

		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public virtual void Tick(Actor self)
		{
			foreach (var w in Weapons)
				w.Tick();

			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.First <= 0)
					x.Second();
				delayedActions[i] = x;
			}
			delayedActions.RemoveAll(a => a.First <= 0);
		}

		internal void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add(Pair.New(t, a));
			else
				a();
		}

		public void DoAttack(Actor self, Target target)
		{
			if( !CanAttack( self ) ) return;

			var move = self.TraitOrDefault<IMove>();
			var facing = self.TraitOrDefault<IFacing>();
			foreach (var w in Weapons)
				w.CheckFire(self, this, move, facing, target);
		}

		public virtual int FireDelay( Actor self, AttackBaseInfo info )
		{
			return info.FireDelay;
		}

		bool IsHeal { get { return Weapons[ 0 ].Info.Warheads[ 0 ].Damage < 0; } }

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new AttackOrderTargeter( "Attack", 6, IsHeal ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order is AttackOrderTargeter )
			{
				if( target.IsActor )
					return new Order("Attack", self, target.Actor, order.IsQueued);
				else
					return new Order("Attack", self, Util.CellContaining(target.CenterLocation), order.IsQueued);
			}
			return null;
		}

		protected void PerformAttack(Actor self, Target target, bool hold, bool queued)
		{
			bool targetIsActor = target.IsActor;

			var ph = new QueuedActivity(
				(qa) =>
				{
					if (!queued || (targetIsActor && !target.IsValid))
					{
						self.CancelActivity();

						// target was an actor, but it isnt valid anymore => cancel the chain
						if (queued) 
							return;
					}
					
					qa.Insert(GetQueuedAttack(self, target, !hold));

					if (self.Owner == self.World.LocalPlayer)
						self.World.AddFrameEndTask(w =>
						{
							if (self.Destroyed) return;
							if (target.IsValid && target.IsActor)
								w.Add(new FlashTarget(target.Actor));
							var line = self.TraitOrDefault<DrawLineToTarget>();
							if (line != null)
								line.SetTarget(self, target, Color.Red);
						});
				});

			self.QueueActivity(queued ? ph : ph.Run(self));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack" || order.OrderString == "AttackHold")
			{
				PerformAttack(self, Target.FromOrder(order), order.OrderString == "AttackHold", order.Queued);
				return;
			} // else not an attack order

			// Is a chained event
			if (order.Queued)
				return;

			target = Target.None;

			/* hack */
			if (self.HasTrait<Turreted>() && self.Info.Traits.Get<AttackBaseInfo>().AlignIdleTurrets)
				self.Trait<Turreted>().desiredFacing = null;
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Attack" || order.OrderString == "AttackHold") ? "Attack" : null;
		}

		protected virtual IActivity GetQueuedAttack(Actor self, Target newTarget, bool allowMovement)
		{
			return new QueuedActivity(true,
				(qa) =>
				{
					var weapon = ChooseWeaponForTarget(newTarget);

					if (weapon != null)
						qa.Insert(new Activities.Attack(newTarget, Math.Max(0, (int) weapon.Info.Range), allowMovement));
				});
		}

		public bool HasAnyValidWeapons(Target t) { return Weapons.Any(w => w.IsValidAgainst(self.World, t)); }
		public float GetMaximumRange() { return Weapons.Max(w => w.Info.Range); }

		public Weapon ChooseWeaponForTarget(Target t) { return Weapons.FirstOrDefault(w => w.IsValidAgainst(self.World, t)); }

		public void AttackTarget(Actor self, Actor target, bool allowMovement)
		{
			AttackTarget(self, target, allowMovement, false);
		}

		public void AttackTarget(Actor self, Actor target, bool allowMovement, bool holdStill)
		{
			var attack = self.Trait<AttackBase>();
			if (target != null)
			{
				if (allowMovement)
					attack.ResolveOrder(self, new Order((holdStill) ? "AttackHold" : "Attack", self, target));
				else
					attack.target = Target.FromActor(target);	// for turreted things on rails.
			}
		}

		public void ScanAndAttack(Actor self, bool allowMovement, bool holdStill)
		{
			if (--nextScanTime <= 0)
			{
				var targetActor = ScanForTarget(self);

				if (targetActor != null)
					AttackTarget(self, targetActor, allowMovement, holdStill);

				var info = self.Info.Traits.Get<AttackBaseInfo>();
				nextScanTime = (int)(25 * (info.ScanTimeAverage +
					(self.World.SharedRandom.NextDouble() * 2 - 1) * info.ScanTimeSpread));
			}
		}

		public Actor ScanForTarget(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			var range = attack.GetMaximumRange();

			if ((!attack.target.IsValid || self.IsIdle) || !Combat.IsInRange(self.CenterLocation, range, attack.target))
				return ChooseTarget(self, range);

			return null;
		}

		public void ScanAndAttack(Actor self, bool allowMovement)
		{
			ScanAndAttack(self, allowMovement, false);
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);
			var attack = self.Trait<AttackBase>();

			return inRange
				.Where(a => a.Owner != null && self.Owner.Stances[a.Owner] == Stance.Enemy)
				.Where(a => attack.HasAnyValidWeapons(Target.FromActor(a)))
				.Where(a => !a.HasTrait<Cloak>() || a.Trait<Cloak>().IsVisible(a, self.Owner))
				.OrderBy(a => (a.CenterLocation - self.CenterLocation).LengthSquared)
				.FirstOrDefault();
		}

		class AttackOrderTargeter : IOrderTargeter
		{
			readonly bool isHeal;

			public AttackOrderTargeter( string order, int priority, bool isHeal )
			{
				this.OrderID = order;
				this.OrderPriority = priority;
				this.isHeal = isHeal;
			}

			public string OrderID { get; private set; }
			public int OrderPriority { get; private set; }

			public bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
			{
				IsQueued = forceQueued;

				cursor = isHeal ? "heal" : "attack";
				if( self == target ) return false;
				if( !self.Trait<AttackBase>().HasAnyValidWeapons( Target.FromActor( target ) ) ) return false;

				var playerRelationship = self.Owner.Stances[ target.Owner ];

				if( isHeal )
					return playerRelationship == Stance.Ally || forceAttack;

				else
					return playerRelationship == Stance.Enemy || forceAttack;
			}

			public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
			{
				IsQueued = forceQueued;

				cursor = isHeal ? "heal" : "attack";
				if( isHeal ) return false;
				if( !self.Trait<AttackBase>().HasAnyValidWeapons( Target.FromCell( location ) ) ) return false;

				if( forceAttack )
					if( self.Info.Traits.Get<AttackBaseInfo>().CanAttackGround )
						return true;

				return false;
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
