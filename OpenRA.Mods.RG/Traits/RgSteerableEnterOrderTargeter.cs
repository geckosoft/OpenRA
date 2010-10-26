using System;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgSteerableEnterOrderTargeter<T> : UnitTraitOrderTargeter<T>
	{
		private readonly Func<Actor, bool> canTarget;
		private readonly Func<Actor, bool> useEnterCursor;

		public RgSteerableEnterOrderTargeter(string order, int priority, bool targetEnemy, bool targetAlly,
		                                     Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor)
			: base(order, priority, "enter", targetEnemy, targetAlly)
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
		}

		public override bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor)
		{
			if (!base.CanTargetUnit(self, target, forceAttack, forceMove, ref cursor)) return false;
			if (!canTarget(target)) return false;
			if (self.TraitOrDefault<RgIsEngineer>() != null && forceAttack)
				return false; /* engi wants to heal! */
			cursor = useEnterCursor(target) ? "enter" : "enter-blocked";
			return true;
		}
	}
}