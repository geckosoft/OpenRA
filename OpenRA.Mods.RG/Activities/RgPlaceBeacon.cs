﻿using System.Linq;
using OpenRA.Mods.Rg.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Activities
{
	internal class RgPlaceBeacon : CancelableActivity
	{
		public ISound CountDown;
		private int launchTicks = 25*20; /* around 20 seconds delay before it is launched after placement */
		private int placeTicks = 25*4; /* around 4 second delay before it is placed */
		private Target target;

		public RgPlaceBeacon(Actor target)
		{
			this.target = Target.FromActor(target);
		}

		protected override bool OnCancel(Actor self)
		{
			if (CountDown != null)
				Sound.StopSound(CountDown);

			return true;
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (!target.IsValid) return NextActivity;
			if ((target.Actor.Location - self.Location).Length > 2)
				return NextActivity; /* moving target ? */

			if (target.Actor.TraitOrDefault<RgBeaconTarget>() == null)
				return NextActivity;
			var rgPlayer = self.Owner.PlayerActor.TraitOrDefault<RgPlayer>();

			if (rgPlayer == null)
				return NextActivity; /* should not occur! */

			if (self.TraitOrDefault<RgSuperPowerLauncher>().Ammo == 0)
				return NextActivity;

			Actor launchSite = rgPlayer.ParentActors().Where(a => a.Info.Name == "tmpl" || a.Info.Name == "eye").FirstOrDefault();
			if (launchSite == null)
				return NextActivity; /* nope */
			if (CountDown == null)
			{
				if (launchSite.Info.Name == "tmpl")
					CountDown = Sound.Play("nuke_beacon.aud", self.CenterLocation);
				else
					CountDown = Sound.Play("ion_beacon.aud", self.CenterLocation);
			}
			if (placeTicks > 0 && self.TraitOrDefault<RgSuperPowerLauncher>().Ammo > 0)
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
					target.Actor.TraitOrDefault<RgBeaconTarget>().TriggerLaunch(self.Owner, launchSite, launchTicks);

					/* Remove 1 ammo */
					self.TraitOrDefault<RgSuperPowerLauncher>().Ammo--;

					return NextActivity;
				}
			}

			return this;
		}
	}
}