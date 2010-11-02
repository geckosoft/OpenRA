using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.Rg.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	/** Allows launching of nuke / ion cannon */

	public class RgDisarmSuperPowerInfo : TraitInfo<RgDisarmSuperPower>
	{
		public readonly int DisarmSpeed = 5; /* per X ticks */
		public readonly float ProgressPerDisarm = 0.030f; /* how many progress per 'disarm' */
	}

	public class RgDisarmSuperPower : IIssueOrder, IResolveOrder, IOrderVoice
	{
		#region IIssueOrder Members

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DisarmSuperPowerOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target)
		{
			if (order.OrderID == "DisarmBeacon")
				return new Order(order.OrderID, self, target.Actor);

			return null;
		}

		#endregion

		#region IOrderVoice Members

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DisarmBeacon") ? "Attack" : null;
		}

		#endregion

		#region IResolveOrder Members

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DisarmBeacon" && order.TargetActor.Owner.Stances[order.Subject.Owner] == Stance.Ally)
				/* only target allied buildings */
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					                           	{
					                           		w.Add(new FlashTarget(order.TargetActor));
					                           		var line = self.TraitOrDefault<DrawLineToTarget>();
					                           		if (line != null)
					                           			line.SetTarget(self, Target.FromOrder(order), Color.Blue);
					                           	});

				self.CancelActivity();
				var mobile = self.Trait<Mobile>();
				self.QueueActivity(mobile.MoveTo(order.TargetActor.Location, order.TargetActor));
				self.QueueActivity(new RgDisarmBeacon(order.TargetActor));
			}
		}

		#endregion

		#region Nested type: DisarmSuperPowerOrderTargeter

		private class DisarmSuperPowerOrderTargeter : UnitTraitOrderTargeter<Building>
		{
			public DisarmSuperPowerOrderTargeter()
				: base("DisarmBeacon", 60, "default", false, true)
			{
			}

			public override bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor)
			{
				if (!base.CanTargetUnit(self, target, forceAttack, forceMove, ref cursor)) return false;

				if (target.TraitOrDefault<RgDestroyed>() != null) return false; /* dead building */
				if (target.TraitOrDefault<RgBeaconTarget>() == null) return false; /* not a valid target */
				if (target.TraitOrDefault<RgBeaconTarget>().TicksBeforeLaunch == 0) return false; /* no beacon placed */

				if (!forceAttack) /* ctrl-aim only! */
					return false;

				if (target.Owner.Stances[self.Owner] != Stance.Ally)
					return false; /* can only target allies */

				if (self.Owner.Country.Race == "gdi")
					cursor = "ability";
				else if (self.Owner.Country.Race == "nod")
					cursor = "ability";
				else
				{
					return false;
				}

				return true;
			}
		}

		#endregion
	}
}