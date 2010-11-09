using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGIdleAnimationInfo : ITraitInfo
	{
		public readonly string[] Animations = { };
		public readonly int IdleWaitTicks = 50;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGIdleAnimation(this);
		}

		#endregion
	}

	public class RGIdleAnimation : INotifyDamage, INotifyIdle
	{
		public readonly RGIdleAnimationInfo Info;

		public RGIdleAnimation(RGIdleAnimationInfo info)
		{
			Info = info;
		}

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.GetCurrentActivity() is Activities.RGIdleAnimation)
				self.CancelActivity();
		}

		#endregion

		#region INotifyIdle Members

		public void Idle(Actor self)
		{
			self.QueueActivity(new Activities.RGIdleAnimation(Info.Animations.Random(self.World.SharedRandom),
															  Info.IdleWaitTicks));
		}

		#endregion
	}
}