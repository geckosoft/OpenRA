using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgPlayerResourcesInfo : ITraitInfo
	{
		public readonly int InitialCash;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgPlayerResources(init.self);
		}

		#endregion
	}

	public class RgPlayerResources : ITick
	{
		private const float displayCashFracPerFrame = .07f;
		private const int displayCashDeltaPerFrame = 37;
		[Sync] public int Cash;
		[Sync] public int DisplayCash;
		private Player Owner;

		public RgPlayerResources(Actor self)
		{
			Owner = self.Owner;
			Cash = self.Info.Traits.Get<RgPlayerResourcesInfo>().InitialCash;
		}

		#region ITick Members

		public void Tick(Actor self)
		{
			int diff = Math.Abs(Cash - DisplayCash);
			int move = Math.Min(Math.Max((int) (diff*displayCashFracPerFrame),
			                             displayCashDeltaPerFrame), diff);

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