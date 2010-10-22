using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Rg.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Activities
{
    class RgEnterTransport : CancelableActivity
    {
        public Actor transport;

        public RgEnterTransport(Actor self, Actor transport)
        {
            this.transport = transport;
        }

        public override IActivity Tick(Actor self)
        {
            if (IsCanceled) return NextActivity;
            if (transport == null || !transport.IsInWorld) return NextActivity;

            var cargo = transport.Trait<RgSteerable>();
            if (cargo.IsFull(transport))
                return NextActivity;


            // Todo: Queue a move order to the transport? need to be
            // careful about units that can't path to the transport
            if ((transport.Location - self.Location).Length > 1)
                return NextActivity;

            cargo.Load(transport, self);

            /* tell the player that his avatar is now inside a steerable */
            var rgplayer = self.Owner.PlayerActor.TraitOrDefault<RgPlayer>();
            if (rgplayer != null)
            {
                rgplayer.SetContainer(transport);
            }
            self.World.AddFrameEndTask(w => w.Remove(self));

            return this;
        }
    }
}
