using System.Linq;
using OpenRA.Mods.Rg.Traits;
using OpenRA.Mods.Rg.Traits.Inventory;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Activities
{
	internal class RgPlaceIonBeacon : CancelableActivity
	{
		public ISound CountDownSnd;
		private int launchTicks = 25*20; /* around 20 seconds delay before it is launched after placement */
		private int placeTicks = 25*4; /* around 4 second delay before it is placed */
		private Target target;

		public RgPlaceIonBeacon(Actor target)
		{
			this.target = Target.FromActor(target);
		}

		protected override bool OnCancel(Actor self)
		{
			if (CountDownSnd != null)
				Sound.StopSound(CountDownSnd);

			return true;
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (!target.IsValid) return NextActivity;
			if ((target.Actor.Location - self.Location).Length > 2)
				return NextActivity; /* moving target ? */

			if (target.Actor.TraitOrDefault<RgIonBeaconTarget>() == null)
				return NextActivity;
			var rgPlayer = self.Owner.PlayerActor.TraitOrDefault<RgPlayer>();

			if (rgPlayer == null)
				return NextActivity; /* should not occur! */

			var inv = RgPlayer.Get(self).Inventory;
			var beaconItm = inv.Get<InvIonBeacon>();
			if (beaconItm == null || !beaconItm.CanTake(1))
				return NextActivity;

			Actor launchSite = rgPlayer.ParentActors().Where(a => a.Info.Name == "eye").FirstOrDefault();
			if (launchSite == null)
				return NextActivity; /* nope */ // @todo Allow launching anyway (problem for nukes though ;p)

			if (CountDownSnd == null)
			{
				CountDownSnd = Sound.Play("ion_beacon.aud", self.CenterLocation);
			}
                 
			if (placeTicks > 0 && beaconItm.CanTake(1))
			{
				placeTicks--;
				if (placeTicks == 0)
				{
					/* Place beacon! */
					/* first get the placer his 'parent' */
					Player parent = self.Owner.PlayerActor.TraitOrDefault<RgPlayer>().Parent;
					if (parent == null) /* should not occur! */
						return NextActivity;

					/* Init launch countdown! */
					target.Actor.TraitOrDefault<RgIonBeaconTarget>().TriggerLaunch(self.Owner, launchSite, launchTicks);

					/* Take 1 beacon item away */
					beaconItm.Take(1);

					return NextActivity;
				}
			}

			return this;
		}

	}
}