using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA;

namespace OpenRg
{
	public static class RGUtil
	{
		public static void SetOwner(Actor actor, Player owner)
		{
			actor.World.Remove(actor);
			actor.Owner = owner;
			actor.World.Add(actor);
		}
	}
}
