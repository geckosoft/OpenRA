using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.Rg.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	internal class RgPassengerInfo : ITraitInfo
	{
		public readonly string CargoType;
		public readonly PipType PipType = PipType.Green;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgPassenger(init.self);
		}

		#endregion
	}

	public class RgPassenger : IIssueOrder, IResolveOrder, IOrderVoice
	{
		private readonly Actor self;

		public RgPassenger(Actor self)
		{
			this.self = self;
		}

		#region IIssueOrder Members

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new RgSteerableEnterOrderTargeter<RgSteerable>("EnterTransport", 100, true, true,
				                                                            target => IsCorrectCargoType(target),
				                                                            target => CanEnter(target));
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target)
		{
			if (order.OrderID == "EnterTransport")
				return new Order(order.OrderID, self, target.Actor);

			return null;
		}

		#endregion

		#region IOrderVoice Members

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterTransport" ||
			    !CanEnter(order.TargetActor)) return null;
			return "Move";
		}

		#endregion

		#region IResolveOrder Members

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EnterTransport")
			{
				if (order.TargetActor == null) return;
				if (!CanEnter(order.TargetActor)) return;
				if (!IsCorrectCargoType(order.TargetActor)) return;

				if (order.TargetActor.TraitOrDefault<RgSteerable>() == null) return;

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					                           	{
					                           		w.Add(new FlashTarget(order.TargetActor));
					                           		var line = self.TraitOrDefault<DrawLineToTarget>();
					                           		if (line != null)
					                           			line.SetTarget(self, Target.FromOrder(order), Color.Green);
					                           	});

				self.CancelActivity();
				var mobile = self.Trait<Mobile>();

				if ((self.Location - order.TargetActor.Location).Length > 1)
					self.QueueActivity(mobile.MoveTo(order.TargetActor.Location, 1));

				self.QueueActivity(new RgEnterTransport(self, order.TargetActor));
			}
		}

		#endregion

		private bool IsCorrectCargoType(Actor target)
		{
			var pi = self.Info.Traits.Get<RgPassengerInfo>();
			var ci = target.Info.Traits.Get<RgSteerableInfo>();
			return ci.Types.Contains(pi.CargoType);
		}

		private bool CanEnter(Actor target)
		{
			var cargo = target.TraitOrDefault<RgSteerable>();
			if (cargo == null)
				return false;

			if (cargo.IsEmpty(target))
				return true;

			Actor first = cargo.Passengers.First();

			/* cant exit a vehicle used by an enemy */
			if (first.Owner.Stances[target.Owner] == Stance.Enemy)
				return false;

			return true;
		}

		public PipType ColorOfCargoPip(Actor self)
		{
			return self.Info.Traits.Get<RgPassengerInfo>().PipType;
		}
	}
}