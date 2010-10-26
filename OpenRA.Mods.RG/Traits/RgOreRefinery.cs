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
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using Util = OpenRA.Graphics.Util;

namespace OpenRA.Mods.Rg.Traits
{
	internal class RgOreRefineryInfo : ITraitInfo
	{
		public readonly int Capacity;
		public readonly int2 DockOffset = new int2(1, 2);
		public readonly bool LocalStorage;
		public readonly int LowPowerProcessTick = 50;
		public readonly PipType PipColor = PipType.Red;
		public readonly int PipCount;
		public readonly int ProcessAmount = 50;
		public readonly int ProcessTick = 25;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgOreRefinery(init.self, this);
		}

		#endregion
	}

	internal class RgOreRefinery : ITick, IAcceptOre, INotifyDamage, INotifySold, INotifyCapture, IPips, IExplodeModifier
	{
		private readonly RgOreRefineryInfo Info;
		private readonly List<Actor> LinkedHarv;
		private readonly PowerManager PlayerPower;
		private readonly PlayerResources PlayerResources;
		private readonly Actor self;
		private int MoneyPerDelivery = 500;
		[Sync] public int Ore;
		private ulong Ticks;

		[Sync] private int nextProcessTime;

		public RgOreRefinery(Actor self, RgOreRefineryInfo info)
		{
			this.self = self;
			Info = info;
			PlayerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			PlayerPower = self.Owner.PlayerActor.Trait<PowerManager>();

			LinkedHarv = new List<Actor>();
		}

		public int MoneyPerSecond
		{
			get
			{
				var power = self.Owner.PlayerActor.Trait<PowerManager>();
				bool lowpower = power.PowerState != PowerState.Normal;

				return (lowpower) ? 1 : 2;
			}
		}

		#region IAcceptOre Members

		public void LinkHarvester(Actor self, Actor harv)
		{
			LinkedHarv.Add(harv);
		}

		public void UnlinkHarvester(Actor self, Actor harv)
		{
			if (LinkedHarv.Contains(harv))
				LinkedHarv.Remove(harv);
		}

		public void GiveOre(int amount)
		{
			/* give money to all players(read : allies) */

			KeyValuePair<int, Player>[] players =
				self.World.players.Where(a => a.Value.Stances[self.Owner] == Stance.Ally).ToArray();

			for (int i = 0; i < players.Length; i++)
			{
				KeyValuePair<int, Player> player = players[i];

				player.Value.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(MoneyPerDelivery);
			}
		}

		public int2 DeliverOffset
		{
			get { return Info.DockOffset; }
		}

		public void OnDock(Actor harv, DeliverResources dockOrder)
		{
			self.Trait<IAcceptOreDockAction>().OnDock(self, harv, dockOrder);
		}

		#endregion

		#region IExplodeModifier Members

		public bool ShouldExplode(Actor self)
		{
			return Ore > 0;
		}

		#endregion

		#region INotifyCapture Members

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			// Unlink any non-docked harvs
			foreach (Actor harv in LinkedHarv)
			{
				if (harv.Owner == self.Owner)
					harv.Trait<Harvester>().UnlinkProc(harv, self);
			}
		}

		#endregion

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				foreach (Actor harv in LinkedHarv)
					harv.Trait<Harvester>().UnlinkProc(harv, self);
		}

		#endregion

		#region INotifySold Members

		public void Selling(Actor self)
		{
		}

		public void Sold(Actor self)
		{
			foreach (Actor harv in LinkedHarv)
				harv.Trait<Harvester>().UnlinkProc(harv, self);
		}

		#endregion

		#region IPips Members

		public IEnumerable<PipType> GetPips(Actor self)
		{
			if (!Info.LocalStorage)
				return null;

			return Util.MakeArray(Info.PipCount,
			                      i => (Ore*1f/Info.Capacity > i*1f/Info.PipCount) ? Info.PipColor : PipType.Transparent);
		}

		#endregion

		#region ITick Members

		public void Tick(Actor self)
		{
			Ticks++;

			if (Ticks%25 == 0)
			{
				KeyValuePair<int, Player>[] players =
					self.World.players.Where(a => a.Value.Stances[self.Owner] == Stance.Ally).ToArray();

				for (int i = 0; i < players.Length; i++)
				{
					KeyValuePair<int, Player> player = players[i];

					player.Value.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(MoneyPerSecond);
				}
			}

			if (!Info.LocalStorage)
				return;

			if (--nextProcessTime <= 0)
			{
				// Convert resources to cash
				int amount = Math.Min(Ore, Info.ProcessAmount);
				amount = Math.Min(amount, PlayerResources.OreCapacity - PlayerResources.Ore);

				if (amount > 0)
				{
					Ore -= amount;
					PlayerResources.GiveOre(amount);
				}
				nextProcessTime = (PlayerPower.PowerState == PowerState.Normal)
				                  	? Info.ProcessTick
				                  	: Info.LowPowerProcessTick;
			}
		}

		#endregion
	}
}