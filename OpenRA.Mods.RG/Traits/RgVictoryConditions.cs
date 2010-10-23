﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    // @todo Currently prevents a proper 'end of game'
    class RgVictoryConditionsInfo : TraitInfo<RgVictoryConditions> { }

    class RgVictoryConditions : ITick, IResolveOrder
    {
        public void Tick(Actor self)
        {
            if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant || self.Owner.PlayerRef.OwnsWorld) return;

            var targets = self.World.Actors.Where(a => !a.Destroyed && a.Owner.Stances[self.Owner] == Stance.Ally && a.TraitOrDefault<MustBeDestroyed>() != null);

            if (targets.Count() == 0)
                Surrender(self);

            var others = self.World.players.Where(p => !p.Value.NonCombatant  && !p.Value.PlayerRef.OwnsWorld && p.Value != self.Owner && p.Value.Stances[self.Owner] == Stance.Enemy);
            if (others.Count() == 0) return;

            if (others.All(p => p.Value.WinState == WinState.Lost))
                 Win(self);
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString == "Surrender")
                Surrender(self);
        }

        void Surrender(Actor self)
        {
           
            if (self.Owner.WinState == WinState.Lost) return;
            self.Owner.WinState = WinState.Lost;

            Game.Debug("{0} is defeated.".F(self.Owner.PlayerName));
            foreach (var a in self.World.Queries.OwnedBy[self.Owner])
                a.Kill(a);

            self.Owner.Shroud.Disabled = true;
             
        }

        void Win(Actor self)
        {
            if (self.Owner.WinState == WinState.Won) return;
            self.Owner.WinState = WinState.Won;

            Game.Debug("{0} is victorious.".F(self.Owner.PlayerName));
            self.Owner.Shroud.Disabled = true;
        }
    }

    /* tag trait for things that must be destroyed for a short game to end */
    /*
    class MustBeDestroyedInfo : TraitInfo<MustBeDestroyed> { }
    class MustBeDestroyed { }*/
}
