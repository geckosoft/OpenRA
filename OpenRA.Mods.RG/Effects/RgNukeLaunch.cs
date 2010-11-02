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

    public class RgNukeLaunch : IEffect
    {
        readonly Actor silo;
        Animation anim;
        float2 pos;
        int2 targetLocation;
        int altitude;
        bool goingUp = true;
        string weapon;
        private Actor placer = null;

        public RgNukeLaunch(Actor playerActor, Actor silo, string weapon, int2 spawnOffset, int2 targetLocation)
        {
            placer = playerActor;
            this.silo = silo;
            this.targetLocation = targetLocation;
            this.weapon = weapon;
            anim = new Animation(weapon);
            anim.PlayRepeating("up");

            if (silo == null)
            {
                altitude = silo.World.Map.Height * Game.CellSize;
                StartDescent(silo.World);
            }
            else
                pos = silo.CenterLocation + spawnOffset;
        }


        void StartDescent(World world)
        {
            pos = OpenRA.Traits.Util.CenterOfCell(targetLocation);
            anim.PlayRepeating("down");
            goingUp = false;
        }

        public void Tick(World world)
        {
            anim.Tick();

            if (goingUp)
            {
                altitude += 100;
                if (altitude >= world.Map.Height * Game.CellSize)
                    StartDescent(world);
            }
            else
            {
                altitude -= 100;
                if (altitude <= 0)
                {
                    // Trigger screen desaturate effect
                    foreach (var a in world.Queries.WithTrait<NukePaletteEffect>())
                        a.Trait.Enable();

                    Explode(world);
                }
            }
        }

        void Explode(World world)
        {
            world.AddFrameEndTask(w => w.Remove(this));
            Combat.DoExplosion(placer, weapon, pos, 0);
            world.WorldActor.Trait<ScreenShaker>().AddEffect(20, pos, 5);
        }

        public IEnumerable<Renderable> Render()
        {
            yield return new Renderable(anim.Image, pos - 0.5f * anim.Image.size - new float2(0, altitude), "effect", (int)pos.Y);
        }
    }
}
