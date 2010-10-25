using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    public class RgScoreInfo : ITraitInfo
    {
        public RgScoreInfo() { }		/* only because we have other ctors */

        public object Create(ActorInitializer init) { return new RgScore(init.self, this); }
    }

    public class RgScore : INotifyDamage
    {
        public Actor Self;
        public RgScoreInfo Info;
        public long Score = 0;

        public RgScore(Actor self, RgScoreInfo info)
        {
            Self= self;
            Info = info;
        }

        public void Damaged(Actor self, AttackInfo e)
        {
            if (!self.IsIdle) return;
            /*if (e.Attacker.Destroyed) return;*/
            
            if (self.TraitOrDefault<RgScore>() == null) return;
            if (self.Owner.Stances[e.Attacker.Owner] == Stance.Ally && e.Damage > 0) return; /* dont give score to players who tk */

            if (e.Damage < 0 && self.Owner.Stances[e.Attacker.Owner] == Stance.Ally)
            {
                /* someone is healing you ! Give score! :) */
                var score = e.Health - e.PreviousHealth;
                if (score <= 0) return; 
                e.Attacker.Owner.PlayerActor.TraitOrDefault<RgScore>().Score += score;

                /* and give cash */
                e.Attacker.Owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(score);
            }else if (e.Damage > 0)
            {
                var score = e.PreviousHealth - e.Health;
                if (score <= 0) return;
                e.Attacker.Owner.PlayerActor.TraitOrDefault<RgScore>().Score += score;

                /* and give cash */
                e.Attacker.Owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(score);
            }
        }
    }
}
