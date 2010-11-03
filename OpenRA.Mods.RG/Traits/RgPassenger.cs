﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.Rg.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Rg.Traits
{
    class RgPassengerInfo : ITraitInfo
    {
        public readonly string CargoType = null;
        public readonly PipType PipType = PipType.Green;

        public object Create(ActorInitializer init) { return new RgPassenger(init.self); }
    }

    public class RgPassenger : IIssueOrder, IResolveOrder, IOrderVoice
    {
        readonly Actor self;
        public RgPassenger(Actor self) { this.self = self; }

        public IEnumerable<IOrderTargeter> Orders
        {
            get
            {
                yield return new EnterOrderTargeter<RgSteerable>("EnterTransport", 100, false, true,
                    target => IsCorrectCargoType(target), target => CanEnter(target));
            }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target)
        {
            if (order.OrderID == "EnterTransport")
                return new Order(order.OrderID, self, target.Actor);

            return null;
        }

        bool IsCorrectCargoType(Actor target)
        {
            var pi = self.Info.Traits.Get<RgPassengerInfo>();
            var ci = target.Info.Traits.Get<RgSteerableInfo>();
            return ci.Types.Contains(pi.CargoType);
        }

        bool CanEnter(Actor target)
        {
            var cargo = target.TraitOrDefault<RgSteerable>();
            return (cargo != null && !cargo.IsFull(target));
        }

        public string VoicePhraseForOrder(Actor self, Order order)
        {
            if (order.OrderString != "EnterTransport" ||
                !CanEnter(order.TargetActor)) return null;
            return "Move";
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString == "EnterTransport" )
            {
                if (order.TargetActor == null) return;
                if (!CanEnter(order.TargetActor)) return;
                if (!IsCorrectCargoType(order.TargetActor)) return;

                if (order.TargetActor.TraitOrDefault<RgSteerable>() == null) return;

                if (self.Owner == self.World.LocalPlayer)
                    self.World.AddFrameEndTask(w =>
                    {
                        w.Add(new FlashTarget(order.TargetActor));
                        var line = self.TraitOrDefault<DrawLineToTarget>();
                        if (line != null)
                            line.SetTarget(self, Target.FromOrder(order), Color.Green);
                    });

                self.CancelActivity();
                var mobile = self.Trait<Mobile>();
                self.QueueActivity(mobile.MoveTo(order.TargetActor.Location, 1));
                self.QueueActivity(new RgEnterTransport(self, order.TargetActor));
            }
        }

        public PipType ColorOfCargoPip(Actor self)
        {
            return self.Info.Traits.Get<RgPassengerInfo>().PipType;
        }
    }
}
