using System.Linq;
using OpenRA;
using OpenRA.Traits;
using OpenRg.Traits;
using OpenRg.Traits.Inventory;

namespace OpenRg.Activities
{
	internal class RGPlaceNukeBeacon : CancelableActivity
	{
		public ISound CountDownSnd;
		private int launchTicks = 25*20; /* around 20 seconds delay before it is launched after placement */
		private int placeTicks = 25*4; /* around 4 second delay before it is placed */
		private Target target;

		public RGPlaceNukeBeacon(Actor target)
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

			if (target.Actor.TraitOrDefault<RGNukeBeaconTarget>() == null)
				return NextActivity;
			var rgPlayer = (RGPlayer) self;

			if (rgPlayer == null)
				return NextActivity; /* should not occur! */

			var inv = rgPlayer.Inventory;
			var beaconItm = inv.Get<RGInvNukeBeacon>();
			if (beaconItm == null || !beaconItm.CanTake(1))
				return NextActivity;

			Actor launchSite = self.World.Actors.Where(a => !a.Destroyed && a.IsInWorld && (a.Info.Name == "tmpl" || a.Info.Name == "tmpl_destroyed")).FirstOrDefault();
			if (launchSite == null)
				return NextActivity; /* nope */ // @todo Allow launching anyway

			if (CountDownSnd == null)
			{
				CountDownSnd = Sound.Play("nuke_beacon.aud", self.CenterLocation);
			}
                 
			if (placeTicks > 0 && beaconItm.CanTake(1))
			{
				placeTicks--;
				if (placeTicks == 0)
				{
					/* Place beacon! */
					/* first get the placer his 'parent' */
					Player parent = ((RGPlayer) self).Faction;
					if (parent == null) /* should not occur! */
						return NextActivity;

					/* Init launch countdown! */
					target.Actor.TraitOrDefault<RGNukeBeaconTarget>().TriggerLaunch(self.Owner, launchSite, launchTicks);

					/* Take 1 beacon item away */
					beaconItm.Take(1);

					return NextActivity;
				}
			}

			return this;
		}

	}
}