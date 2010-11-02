using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Effects
{
	internal class RgPaletteFx : IEffect
	{
		private readonly string Palette = "";
		private readonly Actor a;

		public RgPaletteFx(Actor a, string palette)
		{
			this.a = a;
			Palette = palette;
		}

		#region IEffect Members

		public void Tick(World world)
		{
			if (a.IsDead())
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			foreach (Renderable r in a.Render())
				yield return r.WithPalette(Palette);
		}

		#endregion
	}
}