using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Effects
{
	public class RgIonCannon : IEffect
	{
		private readonly Animation anim;
		private readonly Actor firedBy;
		private Target target;

		public RgIonCannon(Actor firedBy, World world, int2 location)
		{
			this.firedBy = firedBy;
			target = Target.FromCell(location);
			anim = new Animation("ionsfx");
			anim.PlayThen("idle", () => Finish(world));
		}

		#region IEffect Members

		public void Tick(World world)
		{
			anim.Tick();
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
			                            target.CenterLocation -
			                            new float2(.5f*anim.Image.size.X, anim.Image.size.Y - Game.CellSize),
			                            "effect", (int) target.CenterLocation.Y);
		}

		#endregion

		private void Finish(World world)
		{
			foreach (var a in world.Queries.WithTrait<RgIonFx>())
				a.Trait.Enable();

			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(firedBy, "RgIonCannon", target.CenterLocation, 0);
		}
	}
}