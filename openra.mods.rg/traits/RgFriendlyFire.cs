using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    public class RgFriendlyFireInfo : ITraitInfo
    {
        public readonly bool Allow = false;

        public RgFriendlyFireInfo() { }		/* only because we have other ctors */

        public object Create(ActorInitializer init) { return new RgFriendlyFire(init.self, this); }
    }

    public class RgFriendlyFire : IDamageModifier
    {
        public Actor Self;
        public RgFriendlyFireInfo Info;

        public RgFriendlyFire(Actor self, RgFriendlyFireInfo info)
        {
            Self= self;
            Info = info;
        }

        public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
        {
            if (!Info.Allow)
            {
                /* Disallow dmg to allies, EXCEPT to self */
                if (attacker.Owner.Stances[Self.Owner] == Stance.Ally && Self != attacker)
                    return 0;
            }

            return 1; 
        }
    }
}
