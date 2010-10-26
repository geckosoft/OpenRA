using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgProduceActorOnDeathInfo : ITraitInfo
	{
		[ActorReference] public readonly string Actor;
		public readonly int Facing;
		public readonly string InitialActivity;
		public readonly bool MarkDestroyed = true;
		public readonly bool RequiresRefinery = false;
		public readonly int2 SpawnOffset = int2.Zero;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgProduceActorOnDeath(init.self, this);
		}

		#endregion
	}

	public class RgProduceActorOnDeath : INotifyDamage
	{
		public RgProduceActorOnDeathInfo Info;
		public Actor Self;

		public RgProduceActorOnDeath(Actor self, RgProduceActorOnDeathInfo info)
		{
			Info = info;
			Self = self;
		}

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead && self.IsInWorld)
			{
				self.World.AddFrameEndTask(
					w =>
						{
							self.World.Remove(self);

							ActorInfo actorInfo = Rules.Info[Info.Actor];
							string q = actorInfo.Traits.GetOrDefault<BuildableInfo>().Queue;

							int refs =
								self.World.Queries.OwnedBy[self.Owner].Where(a => !a.Destroyed && a.IsInWorld && a.Info.Name == "proc").Count();

							if (!Info.RequiresRefinery || refs > 0)
							{
								Actor queue =
									self.World.Queries.OwnedBy[self.Owner].Where(
										a =>
										a.IsInWorld && !a.Destroyed && a.TraitOrDefault<RgProductionQueue>() != null &&
										a.TraitOrDefault<RgProductionQueue>().Info.Type == q).FirstOrDefault();
								if (queue != null)
								{
									queue.Owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(
										actorInfo.Traits.GetOrDefault<ValuedInfo>().Cost);

									var unit = actorInfo;

									queue.TraitOrDefault<RgProductionQueue>().BeginProduction(new RgProductionItem(queue.TraitOrDefault<RgProductionQueue>(), Info.Actor,25*5, actorInfo.Traits.GetOrDefault<ValuedInfo>().Cost,
											() => self.World.AddFrameEndTask(
												_ =>
												{
													var isBuilding = unit.Traits.Contains<BuildingInfo>();
													/*if (!hasPlayedSound)
													{
														var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
														Sound.PlayToPlayer(order.Player, isBuilding ? eva.BuildingReadyAudio : eva.UnitReadyAudio);
														hasPlayedSound = true;
													}*/
													var u = queue.TraitOrDefault<RgProductionQueue>().BuildUnit(unit.Name, null);

													if (Info.InitialActivity != null)
														u.QueueActivity(Game.CreateObject<IActivity>(Info.InitialActivity));
												}), true));


									/*queue.TraitOrDefault<RgProductionQueue>().BuildUnit(Info.Actor, null);*/
								}
							}

							self.Destroy();
						}
					);
			}
		}

		#endregion
	}
}