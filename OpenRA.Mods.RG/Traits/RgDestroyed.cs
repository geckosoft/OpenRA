using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    public class RgDestroyedInfo : ITraitInfo
    {
        public RgDestroyedInfo() { }		/* only because we have other ctors */

        public object Create(ActorInitializer init) { return new RgDestroyed(init.self, this); }
    }

    public class RgDestroyed : IDamageModifier
    {
        public Actor Self;
        public RgDestroyedInfo Info;

        public RgDestroyed(Actor self, RgDestroyedInfo info)
        {
            Self= self;
            Info = info;
        }

        public float GetDamageModifier(WarheadInfo warhead)
        {
            return 0;
        }
    }
}
