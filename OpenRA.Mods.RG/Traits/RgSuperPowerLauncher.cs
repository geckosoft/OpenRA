using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.Rg.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Rg.Traits
{
    /** Allows launching of nuke / ion cannon */
	public class RgSuperPowerLauncherInfo : TraitInfo<RgSuperPowerLauncher>
	{
	    
	}

    public class RgSuperPowerLauncher : IIssueOrder, IResolveOrder, IOrderVoice
    {
        public int Ammo = 0;

        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new RgSuperPowerLauncherOrderTargeter(); }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target)
        {
            if (order.OrderID == "PlaceBeacon")
                return new Order(order.OrderID, self, target.Actor);

            return null;
        }

        public string VoicePhraseForOrder(Actor self, Order order)
        {
            return (order.OrderString == "PlaceBeacon") ? "Attack" : null;
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (Ammo > 0 && order.OrderString == "PlaceBeacon") // && order.TargetActor.Owner.Stances[order.Subject.Owner] == Stance.Enemy) /* only target enemies */
            {
                if (self.Owner == self.World.LocalPlayer)
                    self.World.AddFrameEndTask(w =>
                    {
                        w.Add(new FlashTarget(order.TargetActor));
                        var line = self.TraitOrDefault<DrawLineToTarget>();
                        if (line != null)
                            line.SetTarget(self, Target.FromOrder(order), Color.Yellow);
                    });

                self.CancelActivity();
                self.QueueActivity(new Move(order.TargetActor.Location, order.TargetActor));
                self.QueueActivity(new RgPlaceBeacon(order.TargetActor));
            }
        }

        class RgSuperPowerLauncherOrderTargeter : UnitTraitOrderTargeter<Building>
        {
            public RgSuperPowerLauncherOrderTargeter()
                : base("PlaceBeacon", 60, "default", true, false) // @todo last false
            {
            }

            public override bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor)
            {
                if (!base.CanTargetUnit(self, target, forceAttack, forceMove, ref cursor)) return false;
                if (self.TraitOrDefault<RgSuperPowerLauncher>().Ammo == 0) return false;

                if (target.TraitOrDefault<RgDestroyed>() != null) return false; /* dead building */

                if (!forceAttack) /* ctrl-aim only! */
                    return false;
                if (target.Owner.Stances[self.Owner] == Stance.Ally) // @todo uncomment
                    return false; /* cant target ally */

                if (self.Owner.Country.Race == "gdi")
                    cursor = "ioncannon";
                else if (self.Owner.Country.Race == "nod")
                    cursor = "nuke";
                else
                {
                    return false;
                }

                return true;
            }
        }
    }
}
