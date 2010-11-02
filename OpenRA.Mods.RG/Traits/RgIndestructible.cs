using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgIndestructibleInfo : ITraitInfo
	{
		/* only because we have other ctors */
		public readonly bool AllowSelfDamage = true;

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgIndestructible(init.self, this);
		}

		#endregion
	}

	public class RgIndestructible : IDamageModifier
	{
		public RgIndestructibleInfo Info;
		public Actor Self;

		public RgIndestructible(Actor self, RgIndestructibleInfo info)
		{
			Self = self;
			Info = info;
		}

		#region IDamageModifier Members

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			if (attacker == Self && Info.AllowSelfDamage)
				return 1;

			return 0;
		}

		#endregion
	}
}