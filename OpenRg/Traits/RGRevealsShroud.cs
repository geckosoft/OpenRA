using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGRevealsShroudInfo : ITraitInfo
	{
		public readonly int Range = 0;	
		public object Create(ActorInitializer init) { return new RGRevealsShroud(this); }
	}

	public class RGRevealsShroud : ITick
	{
		RGRevealsShroudInfo Info;
		int2 previousLocation;
		
		public RGRevealsShroud(RGRevealsShroudInfo info)
		{
			Info = info;
		}
		
		public void Tick(Actor self)
		{	
			if (previousLocation != self.Location)
			{
				previousLocation = self.Location;
				self.World.WorldActor.Trait<Shroud>().UpdateActor(self);
			}
		}
		
		public int RevealRange { get { return Info.Range; } }
	}
}
