using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Effects
{
    class RgDestroyedFx : IEffect
    {
        Actor a;

        public RgDestroyedFx(Actor a)
        {
            this.a = a;
        }

        public void Tick(World world)
        {
            if (a.IsDead())
                world.AddFrameEndTask(w => w.Remove(this));
        }

        public IEnumerable<Renderable> Render()
        {
            foreach (var r in a.Render())
                yield return r.WithPalette("destroyed");
        }
    }
}
