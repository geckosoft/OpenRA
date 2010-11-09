using OpenRA;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGIndestructibleInfo : ITraitInfo
	{
		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGIndestructible(init.self, this);
		}

		#endregion
	}

	public class RGIndestructible : IDamageModifier
	{
		public RGIndestructibleInfo Info;
		public Actor Self;

		public RGIndestructible(Actor self, RGIndestructibleInfo info)
		{
			Self = self;
			Info = info;
		}

		#region IDamageModifier Members

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return 0;
		}

		#endregion
	}
}