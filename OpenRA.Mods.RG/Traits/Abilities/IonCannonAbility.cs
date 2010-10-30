using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Cnc;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.Rg.Generics;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Abilities
{
	public class IonCannonAbilityInfo : RgAbilityInfo
	{
		public override object Create(ActorInitializer init) { return new IonCannonAbility(init.self, this); }
	}

	class IonCannonAbility : RgAbility, IResolveOrder
	{
		public IonCannonAbility(Actor self, RgAbilityInfo info)
			: base(self, info)
		{
		}


		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IonCannon")
			{
				Owner.World.AddFrameEndTask(w =>
				{
					Sound.Play(Info.LaunchSound, Game.CellSize * order.TargetLocation.ToFloat2());
					w.Add(new IonCannon(self, w, order.TargetLocation));
				});

				FinishActivate();
			}
		}

		protected override void OnActivate()
		{
			var rgplayer = Owner.PlayerActor.TraitOrDefault<RgPlayer>();
			if (rgplayer == null)
				return; /* should not occur! */

			Self.World.OrderGenerator =
				new RgGenericSelectTargetWithBuilding<IonControl>(rgplayer.Parent, Self, "IonCannon", "ability");
		}

		public override bool IsAvailable
		{
			get
			{
				return true; /* just a hack to show things */
			}
		}

	}
}
