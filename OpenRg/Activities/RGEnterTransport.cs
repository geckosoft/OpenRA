using OpenRA;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRg.Traits;

namespace OpenRg.Activities
{
	public class RGEnterTransport : CancelableActivity
	{
		public Actor transport;

		public RGEnterTransport(Actor self, Actor transport)
		{
			this.transport = transport;
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (transport == null || !transport.IsInWorld) return NextActivity;

			var rgplayer = (RGPlayer) self;

			var cargo = transport.Trait<RGSteerable>();
			if (rgplayer == null || cargo.IsFull(transport) || (!cargo.IsEmpty(transport) && transport.Owner != rgplayer.Faction))
				return NextActivity;

			if ((transport.Location - self.Location).Length > 2)
				return NextActivity;

			cargo.Load(transport, self);

			// Update ownership of the vehicle
			if (transport.Owner != rgplayer.Faction)
			{
				RGUtil.SetOwner(transport, rgplayer.Faction);
			}

			/* tell the player that his avatar is now inside a steerable */
			rgplayer.Container = transport;

			self.World.AddFrameEndTask(w => w.Remove(self));

			return this;
		}
	}
}