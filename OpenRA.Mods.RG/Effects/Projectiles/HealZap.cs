using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Effects.Projectiles
{
	internal class HealZapInfo : IProjectileInfo
	{
		public readonly int BeamRadius = 1;
		public readonly bool UsePlayerColor;

		#region IProjectileInfo Members

		public IEffect Create(ProjectileArgs args)
		{
			Color c = UsePlayerColor ? args.firedBy.Owner.Color : Color.Blue;
			return new HealZap(args, BeamRadius, c);
		}

		#endregion
	}

	internal class HealZap : IEffect
	{
		private readonly ProjectileArgs args;
		private readonly Color color;
		private readonly int radius;
		private bool doneDamage;
		private int timeUntilRemove = 10; // # of frames
		private int totalTime = 10;

		public HealZap(ProjectileArgs args, int radius, Color color)
		{
			this.args = args;
			this.color = color;
			this.radius = radius;
		}

		#region IEffect Members

		public void Tick(World world)
		{
			if (timeUntilRemove <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
			--timeUntilRemove;

			if (!doneDamage)
			{
				if (args.target.IsValid)
					args.dest = args.target.CenterLocation.ToInt2();

				Combat.DoImpacts(args);
				doneDamage = true;
			}
		}

		public IEnumerable<Renderable> Render()
		{
			var alpha = (int) ((1 - (float) (totalTime - timeUntilRemove)/totalTime)*255);
			Color rc = Color.FromArgb(alpha, color);

			float2 unit = 1.0f/(args.src - args.dest).Length*(args.src - args.dest).ToFloat2();
			var norm = new float2(-unit.Y, unit.X);

			for (int i = -radius; i < radius; i++)
				Game.Renderer.LineRenderer.DrawLine(args.src + i*norm, args.dest + i*norm, rc, rc);

			yield break;
		}

		#endregion
	}
}