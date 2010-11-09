using System;
using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGPlayerResourcesInfo : ITraitInfo
	{
		public readonly int InitialCash = 0;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGPlayerResources(init.self);
		}

		#endregion
	}

	public class RGPlayerResources : ITick
	{
		private const float DisplayCashFracPerFrame = .07f;
		private const int DisplayCashDeltaPerFrame = 37;

		[Sync]
		public int Cash;
		[Sync]
		public int DisplayCash;

		public Player Owner { get; protected set; }

		public RGPlayerResources(Actor self)
		{
			Owner = self.Owner;
			Cash = self.Info.Traits.Get<RGPlayerResourcesInfo>().InitialCash;
		}

		#region ITick Members

		public void Tick(Actor self)
		{
			int diff = Math.Abs(Cash - DisplayCash);
			int move = Math.Min(Math.Max((int)(diff * DisplayCashFracPerFrame),
										 DisplayCashDeltaPerFrame), diff);

			var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();

			if (DisplayCash < Cash)
			{
				DisplayCash += move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
			}
			else if (DisplayCash > Cash)
			{
				DisplayCash -= move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
			}
		}

		#endregion

		public void GiveCash(int num)
		{
			Cash += num;
		}

		public bool TakeCash(int num)
		{
			Cash -= num;

			return true;
		}
	}
}