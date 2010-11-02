using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;
using Util = OpenRA.Traits.Util;

namespace OpenRA.Mods.Rg.Effects
{
	public class RgNukeLaunch : IEffect
	{
		private readonly Animation anim;
		private readonly Actor placer;
		private readonly Actor silo;
		private readonly int2 targetLocation;
		private readonly string weapon;
		private int altitude;
		private bool goingUp = true;
		private float2 pos;

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
				altitude = silo.World.Map.Height*Game.CellSize;
				StartDescent(silo.World);
			}
			else
				pos = silo.CenterLocation + spawnOffset;
		}

		#region IEffect Members

		public void Tick(World world)
		{
			anim.Tick();

			if (goingUp)
			{
				altitude += 100;
				if (altitude >= world.Map.Height*Game.CellSize)
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

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - 0.5f*anim.Image.size - new float2(0, altitude), "effect", (int) pos.Y);
		}

		#endregion

		private void StartDescent(World world)
		{
			pos = Util.CenterOfCell(targetLocation);
			anim.PlayRepeating("down");
			goingUp = false;
		}

		private void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(placer, weapon, pos, 0);
			world.WorldActor.Trait<ScreenShaker>().AddEffect(20, pos, 5);
		}
	}
}