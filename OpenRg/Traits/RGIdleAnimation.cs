using System;
using OpenRA;
using OpenRA.Mods.RA.Render;
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
		enum IdleState
		{
			None,
			Waiting,
			Active
		};

		string sequence;
		int delay;
		IdleState state;

		public readonly RGIdleAnimationInfo Info;

		public RGIdleAnimation(RGIdleAnimationInfo info)
		{
			Info = info;
		}

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (state != IdleState.None)
				self.CancelActivity();
		}

		#endregion

		public void Tick(Actor self)
		{
			if (!self.IsIdle)
			{
				state = IdleState.None;
				return;
			}

			if (state == IdleState.Active)
				return;

			if (delay > 0 && --delay == 0)
			{
				state = IdleState.Active;
				self.Trait<RenderInfantry>().anim.PlayThen(sequence, () => state = IdleState.None);
			}
		}

		public void TickIdle(Actor self)
		{
			if (state != IdleState.None)
				return;

			state = IdleState.Waiting;
			sequence = Info.Animations.Random(self.World.SharedRandom);
			delay = Info.IdleWaitTicks;
		}
	}
}