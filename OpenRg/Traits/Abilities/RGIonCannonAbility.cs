using OpenRA;
using OpenRA.Mods.Cnc;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Traits;
using OpenRg.Generics;

namespace OpenRg.Traits.Abilities
{
	public class RGIonCannonAbilityInfo : RGAbilityInfo
	{
		public override object Create(ActorInitializer init) { return new RGIonCannonAbility(init.self, this); }
	}

	class RGIonCannonAbility : RGAbility, IResolveOrder
	{
		public RGIonCannonAbility(Actor self, RGAbilityInfo info)
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
			var rgplayer = ((RGPlayer) Owner);
			if (rgplayer == null)
				return; /* should not occur! */

			Self.World.OrderGenerator =
				new RGSelectTargetWithBuilding<RGIonControl>(rgplayer.Faction, Self, "IonCannon", "ability");
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
