using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgDestroyedInfo : ITraitInfo
	{
		/* only because we have other ctors */

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgDestroyed(init.self, this);
		}

		#endregion
	}

	public class RgDestroyed : IDamageModifier
	{
		public RgDestroyedInfo Info;
		public Actor Self;

		public RgDestroyed(Actor self, RgDestroyedInfo info)
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