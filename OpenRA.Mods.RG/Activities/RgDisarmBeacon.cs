using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Rg.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Activities
{
    class RgDisarmBeacon : CancelableActivity
    {
        Target target;
        //private ulong ticksPerDisarm = 5;
        //private float progressPerDisarm = 0.04f;
        private ulong ticks = 0;

        public RgDisarmBeacon(Actor target) { this.target = Target.FromActor(target); }
        public ISound CountDown = null;

        protected override bool OnCancel()
        {
            if (CountDown != null)
                Sound.StopSound(CountDown);

            return true;
        }

        public override IActivity Tick(Actor self)
        {
            ticks++;

            if (IsCanceled) return NextActivity;
            if (!target.IsValid) return NextActivity;
            if ((target.Actor.Location - self.Location).Length > 2)
                return NextActivity; /* moving target ? */

            if (target.Actor.TraitOrDefault<RgBeaconTarget>() == null)
                return NextActivity;

            if (target.Actor.TraitOrDefault<RgBeaconTarget>().TicksBeforeLaunch == 0)
                return NextActivity; /* nothing to disarm (blew already?) */

            var rgPlayer = self.Owner.PlayerActor.TraitOrDefault<RgPlayer>();

            if (rgPlayer == null)
                return NextActivity; /* should not occur! */

            if (self.TraitOrDefault<RgDisarmSuperPower>() == null) /* anti cheat */
                return NextActivity;

            if (ticks % (ulong) self.Info.Traits.GetOrDefault<RgDisarmSuperPowerInfo>().DisarmSpeed == 0)
            {
                target.Actor.TraitOrDefault<RgBeaconTarget>().DoDisarm(self.Owner, self.Info.Traits.GetOrDefault<RgDisarmSuperPowerInfo>().ProgressPerDisarm);
            }
            return this;
        }
    }
}
