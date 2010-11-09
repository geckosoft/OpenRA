using System.Collections.Generic;
using OpenRA;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;
using Util = OpenRA.Traits.Util;

namespace OpenRg.Effects
{
	public class RGNukeLaunch : IEffect
	{
		public readonly Animation Anim;
		public readonly Actor Placer;
		public readonly Actor Silo;
		public readonly int2 TargetLocation;
		public readonly string Weapon;
		public int Altitude { get; protected set; }
		public bool GoingUp { get; protected set; }
		public float2 pos { get; protected set; }

		public RGNukeLaunch(Actor playerActor, Actor silo, string weapon, int2 spawnOffset, int2 targetLocation)
		{
			Placer = playerActor;
			Silo = silo;
			TargetLocation = targetLocation;
			Weapon = weapon;
			Anim = new Animation(weapon);
			Anim.PlayRepeating("up");
			GoingUp = true;
			if (silo == null)
			{
				Altitude = playerActor.World.Map.Height * Game.CellSize;
				StartDescent(playerActor.World);
			}
			else
				pos = silo.CenterLocation + spawnOffset;
		}

		#region IEffect Members

		public void Tick(World world)
		{
			Anim.Tick();

			if (GoingUp)
			{
				Altitude += 100;
				if (Altitude >= world.Map.Height * Game.CellSize)
					StartDescent(world);
			}
			else
			{
				Altitude -= 100;
				if (Altitude <= 0)
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
			yield return new Renderable(Anim.Image, pos - 0.5f * Anim.Image.size - new float2(0, Altitude), "effect", (int)pos.Y);
		}

		#endregion

		private void StartDescent(World world)
		{
			pos = Util.CenterOfCell(TargetLocation);
			Anim.PlayRepeating("down");
			GoingUp = false;
		}

		private void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(Placer, Weapon, pos, 0);
			world.WorldActor.Trait<ScreenShaker>().AddEffect(20, pos, 5);
		}
	}
}