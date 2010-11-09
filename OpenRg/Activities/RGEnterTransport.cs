using OpenRA;
using OpenRA.Traits;
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

			var cargo = transport.Trait<RGSteerable>();
			if (cargo.IsFull(transport))
				return NextActivity;


			// Todo: Queue a move order to the transport? need to be
			// careful about units that can't path to the transport
			if ((transport.Location - self.Location).Length > 2)
				return NextActivity;

			cargo.Load(transport, self);

			/* tell the player that his avatar is now inside a steerable */
			var rgplayer = (RGPlayer) self;
			if (rgplayer != null)
			{
				rgplayer.Container = transport;
			}
			self.World.AddFrameEndTask(w => w.Remove(self));

			return this;
		}
	}
}