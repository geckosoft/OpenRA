using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.Rg.Traits.Inventory;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	internal class RgAssignUnitInfo : ITraitInfo
	{
		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgAssignUnit(init.self);
		}

		#endregion
	}

	internal class RgAssignUnit : INotifyProduction, IResolveOrder
	{
		public Queue<Player> PendingOrders = new Queue<Player>();
		public Actor Self;

		public RgAssignUnit(Actor self)
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
				var player = newPlayer.PlayerActor.TraitOrDefault<RgPlayer>();

				Actor avatar = player.Avatar;
				var inv = newPlayer.PlayerActor.TraitOrDefault<RgInventory>();

				/* todo Add distance check? */
				if (avatar != null && !avatar.Destroyed && avatar.IsInWorld && player.Container == null)
				{

					if (other.Info.Name == "gdi_ion_beacon" || other.Info.Name == "nod_nuke_beacon")
					{
						switch (other.Info.Name)
						{
							case "gdi_ion_beacon":
								if (inv.Get<InvIonBeacon>().Amount == inv.Get<InvIonBeacon>().Max)
								{
									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										Game.AddChatLine(Color.OrangeRed, "EVA",
														 "You can only carry " + inv.Get<InvIonBeacon>().Max + "  at the same time!");
									}

									/* give back the money */
									newPlayer.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(
										other.Info.Traits.GetOrDefault<ValuedInfo>().Cost);
									
								}else
								{
									inv.Get<InvIonBeacon>().Give(1);

									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										Game.AddChatLine(Color.OrangeRed, "EVA",
														 inv.Get<InvIonBeacon>().Name + " acquired!");
									}
								}
								break;
							case "nod_nuke_beacon":
								if (inv.Get<InvNukeBeacon>().Amount == inv.Get<InvNukeBeacon>().Max)
								{
									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										Game.AddChatLine(Color.OrangeRed, "EVA",
														 "You can only carry " + inv.Get<InvNukeBeacon>().Max + "  at the same time!");
									}

									/* give back the money */
									newPlayer.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(
										other.Info.Traits.GetOrDefault<ValuedInfo>().Cost);

								}
								else
								{
									inv.Get<InvNukeBeacon>().Give(1);

									if (newPlayer == newPlayer.World.LocalPlayer)
									{
										Game.AddChatLine(Color.OrangeRed, "EVA",
														 inv.Get<InvNukeBeacon>().Name + " acquired!");
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

						foreach (var nd in avatar.TraitsImplementing<IRNotifyUnitSwitch>().Concat(avatar.Owner.PlayerActor.TraitsImplementing<IRNotifyUnitSwitch>()))
							nd.UnitSwitched(self, other);
						avatar.Destroy();


					}else
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