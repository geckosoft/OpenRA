using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.Rg.Activities;
using OpenRA.Mods.Rg.Traits.Inventory;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Abilities
{
	class RgPlaceMineAbilityInfo : RgAbilityInfo
	{
		public readonly string Mine = "minp";
		public override object Create(ActorInitializer init) { return new RgPlaceMineAbility(init.self, this); }
	}

	class RgPlaceMineAbility : RgAbility, IResolveOrder
	{
		public RgPlaceMineAbility(Actor self, RgAbilityInfo info)
			: base(self, info)
		{

		}
		public override string Details
		{
			get
			{
				var inv = RgPlayer.Get(Self).Inventory;
				var item = inv.Get<InvMine>();

				if (item.Amount == 0)
					return "No mines left";

				return item.Amount + " mine(s) left";
			}
		}
		public override bool IsReady
		{
			get
			{
				var inv = RgPlayer.Get(Self).Inventory;
				var item = inv.Get<InvMine>();

				return base.IsReady && (item != null && item.Amount > 0);
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			var inv = RgPlayer.Get(self).Inventory;
			var beaconItm = inv.Get<InvMine>();

			if (order.OrderString == "PlaceMine" && beaconItm != null && beaconItm.CanTake(1))
			{
				self.CancelActivity();
				self.QueueActivity(new RgPlaceMine(((RgPlaceMineAbilityInfo)Info).Mine));
			}
		}

		protected override void OnActivate()
		{
			var rgplayer = Owner.PlayerActor.TraitOrDefault<RgPlayer>();
			if (rgplayer == null)
				return; /* should not occur! */

			var inv = RgPlayer.Get(Self).Inventory;
			var beaconItm = inv.Get<InvMine>();
			if (beaconItm != null && beaconItm.CanTake(1))
			{
				Self.CancelActivity();
				Self.World.IssueOrder(new Order("PlaceMine", Self));
			}
		}

		public override bool IsAvailable
		{
			get
			{
				var inv = RgPlayer.Get(Self).Inventory;
				var itm = inv.Get<InvMine>();

				if (itm == null)
					return false;

				return true;
			}
		}

	}
}
