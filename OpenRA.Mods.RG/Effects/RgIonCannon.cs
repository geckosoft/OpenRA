using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Effects
{
    public class RgIonCannon : IEffect
    {
        Target target;
        Animation anim;
        Actor firedBy;

        public RgIonCannon(Actor firedBy, World world, int2 location)
        {
            this.firedBy = firedBy;
            target = Target.FromCell(location);
            anim = new Animation("ionsfx");
            anim.PlayThen("idle", () => Finish(world));
        }

        public void Tick(World world) { anim.Tick(); }

        public IEnumerable<Renderable> Render()
        {
            yield return new Renderable(anim.Image,
                target.CenterLocation - new float2(.5f * anim.Image.size.X, anim.Image.size.Y - Game.CellSize),
                "effect", (int)target.CenterLocation.Y);
        }

        void Finish(World world)
        {
            world.AddFrameEndTask(w => w.Remove(this));
            Combat.DoExplosion(firedBy, "RgIonCannon", target.CenterLocation, 0);
        }
    }
}
