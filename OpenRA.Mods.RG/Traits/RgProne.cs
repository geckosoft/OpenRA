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
using OpenRA.GameRules;
using OpenRA.Traits;
using System.Collections.Generic;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.Rg.Traits
{
	class RgProneInfo : ITraitInfo
	{
		public readonly string[] TransformSounds = {};
        public readonly string[] NoTransformSounds = { };
        public readonly string Animation = "crawl";
        public readonly string ShootAnimation = "prone-shoot";

        public readonly float DamageTakenModifier = 1f; /* 1f = dmg not modified */
        public readonly float Speed = .5f; /* for some reason making this too low makes it unuseable :s */

        public virtual object Create(ActorInitializer init) { return new RgProne(init.self, this); }
	}

    class RgProne : IIssueOrder, IResolveOrder, IOrderVoice, IDamageModifier, ISpeedModifier
	{
		Actor self;
        public RgProneInfo Info;

        [Sync]
        public bool IsProne = false;

        public RgProne(Actor self, RgProneInfo info)
		{
			this.self = self;
			Info = info;
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "RgSwitchProne") ? "Move" : null;
		}
		
		bool CanGoProne()
		{
		    return true;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
            get { yield return new DeployOrderTargeter("RgSwitchProne", 5, CanGoProne); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
            if (order.OrderID == "RgSwitchProne")
				return new Order( order.OrderID, self );

			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
            if (order.OrderString == "RgSwitchProne")
			{
				if (!CanGoProne())
				{
					foreach (var s in Info.NoTransformSounds)
						Sound.PlayToPlayer(self.Owner, s);
					return;
				}
				self.CancelActivity();
				
                /* @todo Go Prone */
			    IsProne = !IsProne;
			}
		}

        public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
        {
			return IsProne ? Info.DamageTakenModifier : 1f;
        }

        public float GetSpeedModifier()
        {
            return IsProne ? Info.Speed : 1f;
        }
	}
}
