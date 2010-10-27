using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.Rg.Activities;
using OpenRA.Mods.Rg.Generics;
using OpenRA.Mods.Rg.Traits.Inventory;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Abilities
{
	class RgIonCannonBeaconInfo : RgAbilityInfo
	{
		public override object Create(ActorInitializer init) { return new RgIonCannonBeacon(init.self, this); }
	}

	class RgIonCannonBeacon : RgAbility, IResolveOrder
	{
		public RgIonCannonBeacon(Actor self, RgAbilityInfo info)
			: base(self, info)
		{

		}

		public void ResolveOrder(Actor self, Order order)
		{
			var inv = RgPlayer.Get(self).Inventory;
			var beaconItm = inv.Get<InvIonBeacon>();

			if (order.OrderString == "PlaceIonBeacon" && beaconItm != null && beaconItm.CanTake(1))
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
				self.QueueActivity(new RgPlaceIonBeacon(order.TargetActor));
			}
		}

		protected override void OnActivate()
		{
			var rgplayer = Owner.PlayerActor.TraitOrDefault<RgPlayer>();
			if (rgplayer == null)
				return; /* should not occur! */

			Self.World.OrderGenerator = new SelectTarget(this);
		}

		public override bool IsAvailable
		{
			get
			{
				var inv = RgPlayer.Get(Self).Inventory;
				var beaconItm = inv.Get<InvIonBeacon>();

				if (beaconItm == null)
					return false;

				return beaconItm.CanTake(1);
			}
		}
		class SelectTarget : IOrderGenerator
		{
			public RgIonCannonBeacon Beacon;

			public SelectTarget(RgIonCannonBeacon beacon)
			{
				Beacon = beacon;
			}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				//if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var underCursor = world.FindUnitsAtMouse(mi.Location)
						.Where(a => a.Owner != null
							&& !a.HasTrait<RgDestroyed>()
							&& a.HasTrait<Selectable>()
							&& a.HasTrait<RgIonBeaconTarget>()
							&& a.Owner.Stances[Beacon.Self.Owner] == Stance.Enemy).FirstOrDefault();

					if (underCursor != null)
						yield return new Order("PlaceIonBeacon", Beacon.Self, underCursor);
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
					? "ioncannon" : "move-blocked";
			}
		}
	}
}
