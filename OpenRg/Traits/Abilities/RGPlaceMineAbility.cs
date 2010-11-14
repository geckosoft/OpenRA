using OpenRA;
using OpenRA.Traits;
using OpenRg.Activities;

namespace OpenRg.Traits.Abilities
{
	class RGPlaceMineAbilityInfo : RGAbilityInfo
	{
		public readonly string Mine = "minp";
		public override object Create(ActorInitializer init) { return new RGPlaceMineAbility(init.self, this); }
	}

	class RGPlaceMineAbility : RGAbility, IResolveOrder
	{
		public RGPlaceMineAbility(Actor self, RGAbilityInfo info)
			: base(self, info)
		{

		}
		public override string Details
		{
			get
			{
				var inv = ((RGPlayer)Self).Inventory;
				var item = inv.Get<RGInvMine>();

				if (item.Amount == 0)
					return "No mines left";

				return item.Amount + " mine(s) left";
			}
		}
		public override bool IsReady
		{
			get
			{
				var inv = ((RGPlayer)Self).Inventory;
				var item = inv.Get<RGInvMine>();

				return base.IsReady && (item != null && item.Amount > 0);
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			var inv = ((RGPlayer)Self).Inventory;
			var beaconItm = inv.Get<RGInvMine>();

			if (order.OrderString == "PlaceMine" && beaconItm != null && beaconItm.CanTake(1))
			{
				self.CancelActivity();
				self.QueueActivity(new RGPlaceMine(((RGPlaceMineAbilityInfo)Info).Mine));
			}
		}

		protected override void OnActivate()
		{
			var rgplayer = ((RGPlayer) Owner);
			if (rgplayer == null)
				return; /* should not occur! */

			var inv = ((RGPlayer)Self).Inventory;
			var beaconItm = inv.Get<RGInvMine>();
			if (beaconItm != null && beaconItm.CanTake(1))
			{
				Self.World.IssueOrder(new Order("PlaceMine", Self, false));
			}
		}

		public override bool IsAvailable
		{
			get
			{
				var inv = ((RGPlayer)Self).Inventory;
				var itm = inv.Get<RGInvMine>();

				if (itm == null)
					return false;

				return true;
			}
		}

	}
}
