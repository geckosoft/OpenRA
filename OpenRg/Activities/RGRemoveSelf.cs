using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Activities
{
	public class RGRemoveSelf : CancelableActivity
	{
		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			self.World.AddFrameEndTask(a => { if (self.IsInWorld) self.World.Remove(self); if (!self.Destroyed) self.Destroy(); });
			return null;
		}
	}
}
