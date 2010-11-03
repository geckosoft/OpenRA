using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Orders;

namespace OpenRA.Mods.Rg.Generics
{
	public class RgGenericSelectTargetWithBuilding<T> : GenericSelectTarget
	{
		private Player Player = null;

		public RgGenericSelectTargetWithBuilding(Player owner, Actor subject, string order, string cursor)
			: base(subject, order, cursor)
		{
			Player = owner;
		}

		public override void Tick(World world)
		{
			var hasStructure = world.Queries.OwnedBy[Player]
					.WithTrait<T>()
					.Any();

			if (!hasStructure)
				world.CancelInputMode();
		}
	}
}
