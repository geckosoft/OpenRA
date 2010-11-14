using System.Collections.Generic;
using OpenRA;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using System.Linq;

namespace OpenRg.Traits
{
	public class RGProneInfo : ITraitInfo
	{
		public readonly string Animation = "crawl";

		public readonly float DamageTakenModifier = 1f; /* 1f = dmg not modified */
		public readonly string[] NoTransformSounds = { };
		public readonly string ShootAnimation = "prone-shoot";
		public readonly decimal Speed = .5m; /* for some reason making this too low makes it unuseable :s */
		public readonly string[] TransformSounds = { };

		#region ITraitInfo Members

		public virtual object Create(ActorInitializer init)
		{
			return new RGProne(init.self, this);
		}

		#endregion
	}

	public class RGProne : IIssueOrder, IResolveOrder, IOrderVoice, IDamageModifier, ISpeedModifier
	{
		public RGProneInfo Info;

		[Sync]
		public bool IsProne;

		public Actor Owner { get; protected set; }

		public RGProne(Actor owner, RGProneInfo info)
		{
			Owner = owner;
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
			get { yield return new DeployOrderTargeter("RGSwitchProne", 5, CanGoProne); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "RGSwitchProne")
				return new Order(order.OrderID, self, false);

			return null;
		}

		#endregion

		#region IOrderVoice Members

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "RGSwitchProne") ? "Move" : null;
		}

		#endregion

		#region IResolveOrder Members

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "RGSwitchProne")
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

				foreach (var t in self.TraitsImplementing<IRGNotifyProne>())
					t.OnProne(self, IsProne);
			}
		}

		#endregion

		#region ISpeedModifier Members

		public decimal GetSpeedModifier()
		{
			return IsProne ? Info.Speed : 1m;
		}

		#endregion

		private bool CanGoProne()
		{
			return true;
		}
	}
}