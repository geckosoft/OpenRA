using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Effects
{
    public class RgFakeParachute : IEffect
    {
        readonly Animation anim;
        //readonly Animation paraAnim;
        readonly float2 location;

        readonly Actor cargo;
        readonly Player owner;

        float altitude;
        const float fallRate = .3f;

        public RgFakeParachute(Player owner, string image, float2 location, int altitude, Actor cargo)
        {
            this.location = location;
            this.altitude = altitude;
            this.cargo = cargo;
            this.owner = owner;

            anim = new Animation(image);
            if (anim.HasSequence("idle"))
                anim.PlayFetchIndex("idle", () => 0);
            else
                anim.PlayFetchIndex("stand", () => 0);
            anim.Tick();
/*
            paraAnim = new Animation("parach");
            paraAnim.PlayThen("open", () => paraAnim.PlayRepeating("idle"));*/
        }

        public void Tick(World world)
        {
         //   paraAnim.Tick();

            altitude -= fallRate;

            if (altitude <= 0)
                world.AddFrameEndTask(w =>
                {
                    w.Remove(this);
                    var loc = OpenRA.Traits.Util.CellContaining(location);
                    cargo.CancelActivity();
                    cargo.Trait<ITeleportable>().SetPosition(cargo, loc);
                    w.Add(cargo);
                });
        }

        public IEnumerable<Renderable> Render()
        {
            var pos = location - new float2(0, altitude);
            yield return new Renderable(anim.Image, location - .5f * anim.Image.size, "shadow", 0);
            yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, owner.Palette, 2);
           /* yield return new Renderable(paraAnim.Image, pos - .5f * paraAnim.Image.size, owner.Palette, 3);*/
        }
    }
}
