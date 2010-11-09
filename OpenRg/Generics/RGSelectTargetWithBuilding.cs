using System.Linq;
using OpenRA;
using OpenRA.Orders;

namespace OpenRg.Generics
{
	public class RGSelectTargetWithBuilding<T> : GenericSelectTarget
	{
		private readonly Player Player = null;

		public RGSelectTargetWithBuilding(Player owner, Actor subject, string order, string cursor)
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
