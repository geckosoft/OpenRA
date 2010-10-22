using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    public class RgPlayerResourcesInfo : ITraitInfo
    {
        public readonly int InitialCash = 0;

        public object Create(ActorInitializer init) { return new RgPlayerResources(init.self); }
    }

    public class RgPlayerResources : ITick
    {
        Player Owner;

        public RgPlayerResources(Actor self)
        {
            Owner = self.Owner;
            Cash = self.Info.Traits.Get<RgPlayerResourcesInfo>().InitialCash;
        }

        [Sync]
        public int Cash;
        [Sync]
        public int DisplayCash;

        public void GiveCash(int num)
        {
            Cash += num;
        }

        public bool TakeCash(int num)
        {
            Cash -= num ;

            return true;
        }

        const float displayCashFracPerFrame = .07f;
        const int displayCashDeltaPerFrame = 37;

        public void Tick(Actor self)
        {
            var diff = Math.Abs(Cash - DisplayCash);
            var move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
                    displayCashDeltaPerFrame), diff);

            var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
            if (DisplayCash < Cash)
            {
                DisplayCash += move;
                Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
            }
            else if (DisplayCash > Cash)
            {
                DisplayCash -= move;
                Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
            }
        }
    }
}
