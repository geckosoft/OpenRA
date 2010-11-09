using System.Collections.Generic;
using OpenRA;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRg.Effects
{
	public class RGPaletteFx : IEffect
	{
		public readonly string Palette = "";
		public readonly Actor Owner;

		public RGPaletteFx(Actor owner, string palette)
		{
			Owner = owner;
			Palette = palette;
		}

		#region IEffect Members

		public void Tick(World world)
		{
			if (Owner.IsDead())
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			foreach (Renderable r in Owner.Render())
				yield return r.WithPalette(Palette);
		}

		#endregion
	}
}