using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.Effects;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRg.Activities;

namespace OpenRg.Traits
{
	/** Allows launching of nuke / ion cannon */

	public class RGDisarmBeaconInfo : TraitInfo<RGDisarmBeacon>
	{
		public readonly int DisarmSpeed = 5; /* per X ticks */
		public readonly float ProgressPerDisarm = 0.030f; /* how many progress per 'disarm' */
	}

	public class RGDisarmBeacon : IIssueOrder, IResolveOrder, IOrderVoice
	{
		#region IIssueOrder Members

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DisarmSuperPowerOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			return order.OrderID == "DisarmBeacon" ? new Order(order.OrderID, self, false) {TargetActor = target.Actor} : null;
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
				self.QueueActivity(new Activities.RGDisarmBeacon(order.TargetActor));
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

			public override bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueue, ref string cursor)
			{
				if (!base.CanTargetUnit(self, target, forceAttack, forceMove, forceQueue, ref cursor)) return false;

				if (target.TraitOrDefault<RGIndestructible>() != null) return false; /* dead building */
				if (!target.TraitsImplementing<IRGBeaconTarget>().Any()) return false; /* not a valid target */
				if (!target.TraitsImplementing<IRGBeaconTarget>().Where(bt => bt.TicksBeforeLaunch > 0).Any()) return false; /* no beacon placed */

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