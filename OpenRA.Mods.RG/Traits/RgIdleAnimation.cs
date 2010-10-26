using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{

	class RgIdleAnimationInfo : ITraitInfo
	{
		public readonly int IdleWaitTicks = 50;
		public readonly string[] Animations = { };
		public object Create(ActorInitializer init) { return new RgIdleAnimation(this); }
	}

	// infantry prone behavior
	class RgIdleAnimation : INotifyDamage, INotifyIdle
	{
		RgIdleAnimationInfo Info;
		public RgIdleAnimation(RgIdleAnimationInfo info)
		{
			Info = info;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.GetCurrentActivity() is Activities.RgIdleAnimation)
				self.CancelActivity();
		}

		public void Idle(Actor self)
		{
			self.QueueActivity(new Activities.RgIdleAnimation(Info.Animations.Random(self.World.SharedRandom),
															Info.IdleWaitTicks));
		}
	}
}
