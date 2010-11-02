using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.Rg.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgReplaceActorOnDeathInfo : ITraitInfo
	{
		[ActorReference] public readonly string Actor;
		public readonly int Facing;
		public readonly string InitialActivity;
		public readonly bool MarkDestroyed = true;
		public readonly int2 SpawnOffset = int2.Zero;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgReplaceActorOnDeath(init.self, this);
		}

		#endregion
	}

	public class RgReplaceActorOnDeath : INotifyDamage
	{
		public RgReplaceActorOnDeathInfo Info;
		public Actor Self;

		public RgReplaceActorOnDeath(Actor self, RgReplaceActorOnDeathInfo info)
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
								a.World.AddFrameEndTask(ww => ww.Add(new RgPaletteFx(a, "destroyed")));
							}
						}
					);
			}
		}

		#endregion
	}
}