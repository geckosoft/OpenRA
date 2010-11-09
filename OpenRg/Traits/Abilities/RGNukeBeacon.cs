using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRg.Activities;
using OpenRg.Traits.Inventory;

namespace OpenRg.Traits.Abilities
{
	public class RGNukeBeaconInfo : RGAbilityInfo
	{
		public override object Create(ActorInitializer init) { return new RGNukeBeacon(init.self, this); }
	}

	public class RGNukeBeacon : RGAbility, IResolveOrder
	{
		public RGNukeBeacon(Actor self, RGAbilityInfo info)
			: base(self, info)
		{

		}

		public void ResolveOrder(Actor self, Order order)
		{
			var inv = ((RGPlayer)self).Inventory;
			var beaconItm = inv.Get<RGInvNukeBeacon>();

			if (order.OrderString == "PlaceNukeBeacon" && beaconItm != null && beaconItm.CanTake(1))
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Yellow);
					});

				self.CancelActivity();
				var mobile = self.Trait<Mobile>();
				self.QueueActivity(mobile.MoveTo(order.TargetActor.Location, 2)); // order.TargetActor
				self.QueueActivity(new RGPlaceNukeBeacon(order.TargetActor));
			}
		}

		protected override void OnActivate()
		{
			var rgplayer = ((RGPlayer) Owner);
			if (rgplayer == null)
				return; /* should not occur! */

			Self.World.OrderGenerator = new SelectTarget(this);
		}

		public override bool IsAvailable
		{
			get
			{
				var inv = ((RGPlayer)Self).Inventory;
				var beaconItm = inv.Get<RGInvNukeBeacon>();

				if (beaconItm == null)
					return false;

				return beaconItm.CanTake(1);
			}
		}
		class SelectTarget : IOrderGenerator
		{
			public RGNukeBeacon Beacon;

			public SelectTarget(RGNukeBeacon beacon)
			{
				Beacon = beacon;
			}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				world.CancelInputMode();

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var underCursor = world.FindUnitsAtMouse(mi.Location)
						.Where(a => a.Owner != null
							&& !a.HasTrait<RGIndestructible>()
							&& a.HasTrait<Selectable>()
							&& a.HasTrait<RGNukeBeaconTarget>()
							&& a.Owner.Stances[Beacon.Self.Owner] == Stance.Enemy).FirstOrDefault();

					if (underCursor != null)
						yield return new Order("PlaceNukeBeacon", Beacon.Self, underCursor);
				}
			}

			public void Tick(World world)
			{
				if (Beacon.Self.Destroyed || !Beacon.Self.IsInWorld)
					world.CancelInputMode();
			}

			public void RenderAfterWorld(WorldRenderer wr, World world) { }
			public void RenderBeforeWorld(WorldRenderer wr, World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? "nuke" : "move-blocked";
			}
		}
	}
}
