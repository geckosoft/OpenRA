using System.Collections.Generic;
using OpenRA;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRg.Effects
{
	public class RGIonCannon : IEffect
	{
		public readonly Animation Anim;
		public readonly Actor FiredBy;
		public Target Target { get; protected set; }

		public RGIonCannon(Actor firedBy, World world, int2 location)
		{
			FiredBy = firedBy;
			Target = Target.FromCell(location);
			Anim = new Animation("ionsfx");
			Anim.PlayThen("idle", () => Finish(world));
		}

		#region IEffect Members

		public void Tick(World world)
		{
			Anim.Tick();
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(Anim.Image,
										Target.CenterLocation -
										new float2(.5f * Anim.Image.size.X, Anim.Image.size.Y - Game.CellSize),
										"effect", (int)Target.CenterLocation.Y);
		}

		#endregion

		private void Finish(World world)
		{
			foreach (var a in world.Queries.WithTrait<RGIonFx>())
				a.Trait.Enable();

			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(FiredBy, "RgIonCannon", Target.CenterLocation, 0);
		}
	}
}