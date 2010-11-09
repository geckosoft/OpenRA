using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Traits;
using OpenRg.Effects;

namespace OpenRg.Traits
{
	public class RGReplaceActorOnDeathInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string Actor;
		public readonly int Facing;
		public readonly string InitialActivity;
		public readonly bool MarkDestroyed = true;
		public readonly int2 SpawnOffset = int2.Zero;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGReplaceActorOnDeath(init.self, this);
		}

		#endregion
	}

	public class RGReplaceActorOnDeath : INotifyDamage
	{
		public RGReplaceActorOnDeathInfo Info;
		public Actor Self;

		public RGReplaceActorOnDeath(Actor self, RGReplaceActorOnDeathInfo info)
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

						Actor a = w.CreateActor(Info.Actor, new TypeDictionary
							                                    	{
							                                    		new LocationInit(self.Location + Info.SpawnOffset),
							                                    		new OwnerInit(self.Owner),
							                                    		new SkipMakeAnimsInit()
							                                    	});

						if (Info.InitialActivity != null)
							a.QueueActivity(Game.CreateObject<IActivity>(Info.InitialActivity));


						self.Destroy();

						if (Info.MarkDestroyed)
						{
							a.World.AddFrameEndTask(ww =>
							                        {
							                        	ww.Add(new RGPaletteFx(a, "destroyed"));
														a.TraitOrDefault<Health>().InflictDamage(a, a, a.TraitOrDefault<Health>().HP - 1, null);
							                        }
								);
						}
					}
					);
			}
		}

		#endregion
	}
}