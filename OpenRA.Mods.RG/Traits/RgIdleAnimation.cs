using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	internal class RgIdleAnimationInfo : ITraitInfo
	{
		public readonly string[] Animations = {};
		public readonly int IdleWaitTicks = 50;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgIdleAnimation(this);
		}

		#endregion
	}

	// infantry prone behavior
	internal class RgIdleAnimation : INotifyDamage, INotifyIdle
	{
		private readonly RgIdleAnimationInfo Info;

		public RgIdleAnimation(RgIdleAnimationInfo info)
		{
			Info = info;
		}

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.GetCurrentActivity() is Activities.RgIdleAnimation)
				self.CancelActivity();
		}

		#endregion

		#region INotifyIdle Members

		public void Idle(Actor self)
		{
			self.QueueActivity(new Activities.RgIdleAnimation(Info.Animations.Random(self.World.SharedRandom),
			                                                  Info.IdleWaitTicks));
		}

		#endregion
	}
}