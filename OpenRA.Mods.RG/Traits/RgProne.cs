using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	internal class RgProneInfo : ITraitInfo
	{
		public readonly string Animation = "crawl";

		public readonly float DamageTakenModifier = 1f; /* 1f = dmg not modified */
		public readonly string[] NoTransformSounds = {};
		public readonly string ShootAnimation = "prone-shoot";
		public readonly float Speed = .5f; /* for some reason making this too low makes it unuseable :s */
		public readonly string[] TransformSounds = {};

		#region ITraitInfo Members

		public virtual object Create(ActorInitializer init)
		{
			return new RgProne(init.self, this);
		}

		#endregion
	}

	internal class RgProne : IIssueOrder, IResolveOrder, IOrderVoice, IDamageModifier, ISpeedModifier
	{
		public RgProneInfo Info;

		[Sync] public bool IsProne;
		private Actor self;

		public RgProne(Actor self, RgProneInfo info)
		{
			this.self = self;
			Info = info;
		}

		#region IDamageModifier Members

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return IsProne ? Info.DamageTakenModifier : 1f;
		}

		#endregion

		#region IIssueOrder Members

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("RgSwitchProne", 5, CanGoProne); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target)
		{
			if (order.OrderID == "RgSwitchProne")
				return new Order(order.OrderID, self);

			return null;
		}

		#endregion

		#region IOrderVoice Members

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "RgSwitchProne") ? "Move" : null;
		}

		#endregion

		#region IResolveOrder Members

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "RgSwitchProne")
			{
				if (!CanGoProne())
				{
					foreach (string s in Info.NoTransformSounds)
						Sound.PlayToPlayer(self.Owner, s);
					return;
				}
				self.CancelActivity();

				/* @todo Go Prone */
				IsProne = !IsProne;
			}
		}

		#endregion

		#region ISpeedModifier Members

		public float GetSpeedModifier()
		{
			return IsProne ? Info.Speed : 1f;
		}

		#endregion

		private bool CanGoProne()
		{
			return true;
		}
	}
}