using System.Linq;
using OpenRA;
using OpenRA.Traits;
using OpenRg.Traits;

namespace OpenRg.Activities
{
	public class RGDisarmBeacon : CancelableActivity
	{
		public ISound CountDown;
		private Target target;
		//private ulong ticksPerDisarm = 5;
		//private float progressPerDisarm = 0.04f;
		private ulong ticks;

		public RGDisarmBeacon(Actor target)
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
			ticks++;

			if (IsCanceled) return NextActivity;
			if (!target.IsValid) return NextActivity;
			if ((target.Actor.Location - self.Location).Length > 2)
				return NextActivity; /* moving target ? */

			if (!target.Actor.TraitsImplementing<IRGBeaconTarget>().Any())
				return NextActivity;

			if (!target.Actor.TraitsImplementing<IRGBeaconTarget>().Where( bt => bt.TicksBeforeLaunch > 0).Any())
				return NextActivity; /* nothing to disarm (blew already?) */

			var rgPlayer = (RGPlayer) self;

			if (rgPlayer == null)
				return NextActivity; /* should not occur! */

			if (self.TraitOrDefault<Traits.RGDisarmBeacon>() == null) /* anti cheat */
				return NextActivity;

			if (ticks%(ulong) self.Info.Traits.GetOrDefault<RGDisarmBeaconInfo>().DisarmSpeed == 0)
			{
				target.Actor.TraitsImplementing<IRGBeaconTarget>().Where(bt => bt.TicksBeforeLaunch > 0).First().DoDisarm(self.Owner,
				                                                       self.Info.Traits.GetOrDefault<RGDisarmBeaconInfo>().
				                                                       	ProgressPerDisarm);
			}
			return this;
		}
	}
}