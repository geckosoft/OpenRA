#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class C4DemolitionInfo : TraitInfo<C4Demolition>
	{
		public readonly float C4Delay = 0;
	}

	class C4Demolition : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new UnitTraitOrderTargeter<Building>( "C4", 6, "c4", true, false ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "C4" )
				return new Order("C4", self, target.Actor, order.IsQueued);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "C4")
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});
				
				var mobile = self.Trait<Mobile>();
				self.CancelActivity();
				self.QueueActivity(new Enter(order.TargetActor));
				//self.QueueActivity(new Move(order.TargetActor.Location, order.TargetActor));
				self.QueueActivity(new Demolish(order.TargetActor));
				self.QueueActivity(mobile.MoveTo(self.Location, 0));
			}
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "C4") ? "Attack" : null;
		}
	}
}
