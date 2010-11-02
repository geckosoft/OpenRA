using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Mods.Rg.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    public class RgReplaceActorOnDeathInfo : ITraitInfo
    {
        [ActorReference]
        public readonly string Actor = null;
        public readonly string InitialActivity = null;
        public readonly int2 SpawnOffset = int2.Zero;
        public readonly int Facing = 0;
        public readonly bool MarkDestroyed = true;

        public object Create(ActorInitializer init) { return new RgReplaceActorOnDeath(init.self, this); }
    }

    public class RgReplaceActorOnDeath : INotifyDamage
    {
        public RgReplaceActorOnDeathInfo Info = null;
        public Actor Self = null;

        public RgReplaceActorOnDeath(Actor self, RgReplaceActorOnDeathInfo info)
        {
            Info = info;
            Self = self;
        }

        public void Damaged(Actor self, AttackInfo e)
        {
            if (e.DamageState == DamageState.Dead && self.IsInWorld)
            {
                self.World.AddFrameEndTask(
                   w =>
                   {
                       self.World.Remove(self);

                       var a = w.CreateActor(Info.Actor, new TypeDictionary
				        {
						    new LocationInit( self.Location + Info.SpawnOffset ),
						    new OwnerInit( self.Owner ),
					    });

                       if (Info.InitialActivity != null)
                           a.QueueActivity(Game.CreateObject<IActivity>(Info.InitialActivity));


                       self.Destroy();

                       if (Info.MarkDestroyed)
                       {
                           a.World.AddFrameEndTask(ww => ww.Add(new RgDestroyedFx(a)));
                       }
                   }
                   );
            }
        }
    }
}
