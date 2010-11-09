using System.Collections.Generic;
using System.Drawing;
using OpenRA;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRg.Effects.Projectiles
{
	public class RGHealZapInfo : IProjectileInfo
	{
		public readonly int BeamRadius = 1;
		public readonly bool UsePlayerColor = false;

		#region IProjectileInfo Members

		public IEffect Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.firedBy.Owner.Color : Color.Blue;

			return new RGHealZap(args, BeamRadius, c);
		}

		#endregion
	}

	public class RGHealZap : IEffect
	{
		private readonly ProjectileArgs _args;
		private readonly Color _color;
		private readonly int _radius;
		private bool _doneDamage;
		private int _timeUntilRemove = 10; // # of frames
		private const int TotalTime = 10;

		public RGHealZap(ProjectileArgs args, int radius, Color color)
		{
			_args = args;
			_color = color;
			_radius = radius;
		}

		#region IEffect Members

		public void Tick(World world)
		{
			if (_timeUntilRemove <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
			--_timeUntilRemove;

			if (!_doneDamage)
			{
				if (_args.target.IsValid)
					_args.dest = _args.target.CenterLocation.ToInt2();

				Combat.DoImpacts(_args);
				_doneDamage = true;
			}
		}

		public IEnumerable<Renderable> Render()
		{
			var alpha = (int)((1 - (float)(TotalTime - _timeUntilRemove) / TotalTime) * 255);
			var rc = Color.FromArgb(alpha, _color);

			float2 unit = 1.0f / (_args.src - _args.dest).Length * (_args.src - _args.dest).ToFloat2();
			var norm = new float2(-unit.Y, unit.X);

			for (int i = -_radius; i < _radius; i++)
				Game.Renderer.LineRenderer.DrawLine(_args.src + i * norm, _args.dest + i * norm, rc, rc);

			yield break;
		}

		#endregion
	}
}