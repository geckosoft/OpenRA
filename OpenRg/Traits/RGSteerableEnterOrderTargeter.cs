using System;
using OpenRA;
using OpenRA.Mods.RA.Orders;

namespace OpenRg.Traits
{
	public class RGSteerableEnterOrderTargeter<T> : UnitTraitOrderTargeter<T>
	{
		private readonly Func<Actor, bool> _canTarget;
		private readonly Func<Actor, bool> _useEnterCursor;

		public RGSteerableEnterOrderTargeter(string order, int priority, bool targetEnemy, bool targetAlly,
		                                     Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor)
			: base(order, priority, "enter", targetEnemy, targetAlly)
		{
			_canTarget = canTarget;
			_useEnterCursor = useEnterCursor;
		}

		public override bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor)
		{
			if (!base.CanTargetUnit(self, target, forceAttack, forceMove, ref cursor)) return false;
			if (!_canTarget(target)) return false;

			if (self.TraitOrDefault<RGIsEngineer>() != null && forceAttack)
				return false; /* engi wants to heal! */

			cursor = _useEnterCursor(target) ? "enter" : "enter-blocked";

			return true;
		}
	}
}