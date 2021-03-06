﻿using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.Rg.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgSteerableInfo : ITraitInfo
	{
		public readonly int Passengers;
		public readonly string[] Types = {};
		public readonly int UnloadFacing;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgSteerable(init.self);
		}

		#endregion
	}

	public class RgSteerable : IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyDamage, ITick
	{
		private readonly List<Actor> cargo = new List<Actor>();
		private readonly Actor self;

		public RgSteerable(Actor self)
		{
			this.self = self;
		}

		public IEnumerable<Actor> Passengers
		{
			get { return cargo; }
		}

		#region IIssueOrder Members

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DeployOrderTargeter("Unload", 10, () => CanUnload(self));
				yield return new UnitTraitOrderTargeter<RgPassenger>("ReverseEnterTransport", 9, null, false, true);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target)
		{
			if (order.OrderID == "Unload")
				return new Order(order.OrderID, self);

			if (order.OrderID == "ReverseEnterTransport")
				return new Order(order.OrderID, self, target.Actor);

			return null;
		}

		#endregion

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageStateChanged && e.DamageState == DamageState.Dead)
			{
				self.World.AddFrameEndTask(w =>
				                           	{
				                           		Actor actor = Unload(self);

				                           		while (actor != null)
				                           		{
				                           			w.Add(actor);
				                           			actor.TraitsImplementing<IMove>().FirstOrDefault().SetPosition(actor, self.Location);
				                           			actor.CancelActivity();
				                           			actor = Unload(self);
				                           		}
				                           	});
			}
		}

		#endregion

		#region IOrderVoice Members

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload" || IsEmpty(self)) return null;
			return "Move";
		}

		#endregion

		#region IPips Members

		public IEnumerable<PipType> GetPips(Actor self)
		{
			int numPips = self.Info.Traits.Get<RgSteerableInfo>().Passengers;
			for (int i = 0; i < numPips; i++)
				if (i >= cargo.Count)
					yield return PipType.Transparent;
				else
					yield return GetPipForPassenger(cargo[i]);
		}

		#endregion

		#region IResolveOrder Members

		public void ResolveOrder(Actor self, Order order)
		{
			if ((order.OrderString == "Move" || order.OrderString == "AttackMove" || order.OrderString == "Attack") &&
			    Passengers.Count() == 0)
			{
				self.CancelActivity();
				Game.Debug("Cannot move a vehicle without a driver!");
				return;
			}

			if (order.OrderString == "Unload")
			{
				if (!CanUnload(self))
					return;

				self.CancelActivity();
				self.QueueActivity(new RgExitSteerable());
			}

			if (order.OrderString == "ReverseEnterTransport")
			{
				if (order.TargetActor != null && order.Subject.Owner == order.TargetActor.Owner)
				{
					var passenger = order.TargetActor.Trait<RgPassenger>();
					passenger.ResolveOrder(order.TargetActor, new Order("EnterTransport", order.TargetActor, self));
				}
			}
		}

		#endregion

		#region ITick Members

		public void Tick(Actor self)
		{
			if (cargo.Count == 0)
				return; /* nothing to do here */

			/* remove people who surrendered from the cargo */
			cargo.RemoveAll(a => a.Owner.WinState == WinState.Lost);
		}

		#endregion

		private bool CanUnload(Actor self)
		{
			if (IsEmpty(self))
				return false;

			// Cannot unload mid-air
			var move = self.TraitOrDefault<IMove>();
			if (move != null && move.Altitude > 0)
				return false;

			// Todo: Check if there is a free tile to unload to
			return true;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload") return null;
			return CanUnload(self) ? "deploy" : "deploy-blocked";
		}

		public bool IsFull(Actor self)
		{
			return cargo.Count == self.Info.Traits.Get<RgSteerableInfo>().Passengers;
		}

		public bool IsEmpty(Actor self)
		{
			return cargo.Count == 0;
		}

		public Actor Peek(Actor self)
		{
			return cargo[0];
		}

		public Actor Unload(Actor self)
		{
			if (cargo.Count != 0)
			{
				Actor a = cargo[0];
				cargo.RemoveAt(0);
				return a;
			}

			return null;
		}

		private static PipType GetPipForPassenger(Actor a)
		{
			return a.Trait<RgPassenger>().ColorOfCargoPip(a);
		}

		public void Load(Actor self, Actor a)
		{
			cargo.Add(a);
		}
	}
}