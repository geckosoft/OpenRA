using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    class RgAssignUnitInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new RgAssignUnit(init.self); }
    }

    class A<T, T2>
    {
        
    }
    class RgAssignUnit : INotifyProduction, IResolveOrder
    {
        public Queue<Player> PendingOrders = new Queue<Player>();
        public Actor Self = null;

        public RgAssignUnit(Actor self)
        {
            Self = self;
        }

        public ProductionQueueInfo Info
        {
            get { return Self.TraitOrDefault<ProductionQueue>().Info; }
        }

        public void UnitProduced(Actor self, Actor other, int2 exit)
        {
            if (PendingOrders.Count > 0)
            {
                var newPlayer = PendingOrders.Dequeue();

                if (newPlayer.PlayerRef.OwnsWorld || newPlayer.NonCombatant)
                    return;

                /* find our current units */
                var player = newPlayer.PlayerActor.TraitOrDefault<RgPlayer>();

                var avatar = player.Avatar;

                /* todo Add distance check? */
                if (avatar != null && !avatar.Destroyed && avatar.IsInWorld && player.Container == null )
                {
                    if (other.Info.Name == "gdi_ion_beacon" || other.Info.Name == "nod_nuke_beacon")
                    {
                        if (avatar.TraitOrDefault<RgSuperPowerLauncher>().Ammo == 0)
                        {
                            if (newPlayer == newPlayer.World.LocalPlayer)
                            {
                                Game.AddChatLine(Color.OrangeRed, "EVA",
                                                 "Beacon acquired!");
                            }
                            avatar.TraitOrDefault<RgSuperPowerLauncher>().Ammo++;
                        }else
                        {
                            if (newPlayer == newPlayer.World.LocalPlayer)
                            {
                                Game.AddChatLine(Color.OrangeRed, "EVA",
                                                 "You can only carry one beacon at the same time!");
                            }
                            /* give back the money */
                            newPlayer.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(other.Info.Traits.GetOrDefault<ValuedInfo>().Cost);
                        }

                        other.World.Remove(other);
                        other.Destroy();
                    }
                    else if (avatar.Info.Traits.GetOrDefault<BuildableInfo>() != null && avatar.Info.Traits.GetOrDefault<BuildableInfo>().Queue == "Infantry")
                    {
                        avatar.CancelActivity();
                        self.World.Remove(other);
                        other.Owner = newPlayer;
                        self.World.Add(other);

                        self.World.Remove(avatar);

                        avatar.Destroy();
                    }
                }else
                {
                    self.World.Remove(other);
                    /*other.Destroy();*/
                }
            }
        }

        public void Enqueue(Player player)
        {
            PendingOrders.Enqueue(player);
        }

        public void ResolveOrder(Actor self, Order order)
        {
            switch (order.OrderString)
            {
                case "RgStartProduction":
                    {
                        break;
                    }
            }
        }
    }
}
