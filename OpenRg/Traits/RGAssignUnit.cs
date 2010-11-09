using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Traits;
using OpenRg;
using OpenRg.Traits;
using OpenRg.Traits.Inventory;

namespace OpenRA.Mods.Rg.Traits
{
	public class RGAssignUnitInfo : ITraitInfo
	{
		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGAssignUnit(init.self);
		}

		#endregion
	}

	public class RGAssignUnit : INotifyProduction, IResolveOrder
	{
		public Queue<Player> PendingOrders = new Queue<Player>();
		public Actor Self;

		public RGAssignUnit(Actor self)
		{
			Self = self;
		}

		public ProductionQueueInfo Info
		{
			get { return Self.TraitOrDefault<ProductionQueue>().Info; }
		}

		#region INotifyProduction Members

		public void UnitProduced(Actor self, Actor other, int2 exit)
		{
			if (PendingOrders.Count > 0)
			{
				Player newPlayer = PendingOrders.Dequeue();

				if (newPlayer.PlayerRef.OwnsWorld || newPlayer.NonCombatant)
					return;

				/* find our current units */
				var player = (RGPlayer) newPlayer;

				Actor avatar = player.Avatar;
				var inv = newPlayer.PlayerActor.TraitOrDefault<RGInventory>();

				/* todo Add distance check? */
				if (avatar != null && !avatar.Destroyed && avatar.IsInWorld && player.Container == null)
				{

					if (other.Info.Name == "gdi_ion_beacon" || other.Info.Name == "nod_nuke_beacon" || other.Info.Name == "refill_infantry")
					{
						switch (other.Info.Name)
						{
							case "refill_infantry":
								player.Refill();

								break;
							case "gdi_ion_beacon":
								if (inv.Get<RGInvIonBeacon>().Amount == inv.Get<RGInvIonBeacon>().Max)
								{
									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										RGGame.EVA("You can only carry " + inv.Get<RGInvIonBeacon>().Max + "  at the same time!");
									}

									/* give back the money */
									newPlayer.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(
										other.Info.Traits.GetOrDefault<ValuedInfo>().Cost);

								}
								else
								{
									inv.Get<RGInvIonBeacon>().Give(1);

									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										RGGame.EVA(inv.Get<RGInvIonBeacon>().Name + " acquired!");
									}
								}
								break;
							case "nod_nuke_beacon":
								if (inv.Get<RGInvNukeBeacon>().Amount == inv.Get<RGInvNukeBeacon>().Max)
								{
									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										RGGame.EVA("You can only carry " + inv.Get<RGInvNukeBeacon>().Max + "  at the same time!");
									}

									/* give back the money */
									newPlayer.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(
										other.Info.Traits.GetOrDefault<ValuedInfo>().Cost);

								}
								else
								{
									inv.Get<RGInvNukeBeacon>().Give(1);

									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										RGGame.EVA(inv.Get<RGInvNukeBeacon>().Name + " acquired!");
									}
								}
								break;
						}

						other.World.Remove(other);
						other.Destroy();
					}
					else if (avatar.Info.Traits.GetOrDefault<BuildableInfo>() != null &&
							 avatar.Info.Traits.GetOrDefault<BuildableInfo>().Queue == "Infantry")
					{
						avatar.CancelActivity();
						self.World.Remove(other);

						other.Owner = newPlayer;
						self.World.Add(other);

						self.World.Remove(avatar);

						foreach (var nd in avatar.TraitsImplementing<IRGNotifyUnitSwitched>().Concat(avatar.Owner.PlayerActor.TraitsImplementing<IRGNotifyUnitSwitched>()))
							nd.UnitSwitched(self, other);

						avatar.Destroy();
					}
					else
					{
						self.World.Remove(other);
					}
				}
				else
				{
					self.World.Remove(other);
					/*other.Destroy();*/
				}
			}
		}

		#endregion

		#region IResolveOrder Members

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

		#endregion

		public void Enqueue(Player player)
		{
			PendingOrders.Enqueue(player);
		}
	}
}