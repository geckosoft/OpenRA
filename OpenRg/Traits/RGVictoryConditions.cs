using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGVictoryConditionsInfo : TraitInfo<RGVictoryConditions>
	{
	}

	public class RGVictoryConditions : ITick, IResolveOrder
	{
		#region IResolveOrder Members

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Surrender")
				Surrender(self);
		}

		#endregion

		#region ITick Members

		public void Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant || self.Owner.PlayerRef.OwnsWorld) return;

			IEnumerable<Actor> targets =
				self.World.Actors.Where(
					a => !a.Destroyed && a.Owner.Stances[self.Owner] == Stance.Ally && a.TraitOrDefault<RGMustBeDestroyed>() != null);

			if (targets.Count() == 0)
				Surrender(self);

			IEnumerable<KeyValuePair<int, Player>> others =
				self.World.players.Where(
					p =>
					!p.Value.NonCombatant && !p.Value.PlayerRef.OwnsWorld && p.Value != self.Owner &&
					p.Value.Stances[self.Owner] == Stance.Enemy);

			if (others.All(p => p.Value.WinState == WinState.Lost) && (others.Count() > 0) || (others.Count() == 0 && !Debugger.IsAttached))
				Win(self);
		}

		#endregion

		private void Surrender(Actor self)
		{
			if (self.Owner.WinState == WinState.Lost) return;
			self.Owner.WinState = WinState.Lost;

			Game.Debug("{0} is defeated.".F(self.Owner.PlayerName));
			foreach (Actor a in self.World.Queries.OwnedBy[self.Owner])
				a.Kill(a);

			self.Owner.Shroud.Disabled = true;
		}

		private void Win(Actor self)
		{
			if (self.Owner.WinState == WinState.Won) return;
			self.Owner.WinState = WinState.Won;

			Game.Debug("{0} is victorious.".F(self.Owner.PlayerName));
			self.Owner.Shroud.Disabled = true;
		}
	}
}