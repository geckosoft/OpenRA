using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Rg.Traits.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Rg.Activities
{
	public class RgIdleAnimation : Idle
	{
		string sequence;
		int delay;

		public RgIdleAnimation(string sequence, int delay)
		{
			this.sequence = sequence;
			this.delay = delay;
		}

		bool active = true;
		public override IActivity Tick(Actor self)
		{
			if (!active) return NextActivity;

			if (delay > 0 && --delay == 0)
				self.Trait<RgRenderInfantry>().anim.PlayThen(sequence, () => active = false);

			return this;
		}

		protected override bool OnCancel(Actor self)
		{
			active = false;
			return true;
		}

	}
}
