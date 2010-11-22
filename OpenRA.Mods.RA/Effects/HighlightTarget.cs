using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class HighlightTarget : IEffect
	{
		public readonly Actor Target;
		protected bool PendingRemoval = false;

		public void Remove() { PendingRemoval = true; }
		public HighlightTarget(Actor target)
		{
			Target = target;

			if (!Target.Destroyed)
				foreach (var e in target.World.Effects.OfType<HighlightTarget>().Where(a => a.Target == target).ToArray())
					target.World.Remove(e);
		}

		public void Tick(World world)
		{
			if (Target.Destroyed || !Target.IsInWorld || PendingRemoval)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			if (Target.Destroyed || !Target.IsInWorld || PendingRemoval)
				yield break;

			foreach (var r in Target.Render())
				yield return r.WithPalette("highlight");
		}
	}
}
