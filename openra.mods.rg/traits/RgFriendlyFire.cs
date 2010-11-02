using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgFriendlyFireInfo : ITraitInfo
	{
		public readonly bool Allow;

		/* only because we have other ctors */

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgFriendlyFire(init.self, this);
		}

		#endregion
	}

	public class RgFriendlyFire : IDamageModifier
	{
		public RgFriendlyFireInfo Info;
		public Actor Self;

		public RgFriendlyFire(Actor self, RgFriendlyFireInfo info)
		{
			Self = self;
			Info = info;
		}

		#region IDamageModifier Members

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			if (!Info.Allow)
			{
				/* Disallow dmg to allies, EXCEPT to self */
				if (attacker.Owner.Stances[Self.Owner] == Stance.Ally && Self != attacker)
					return 0;
			}

			return 1;
		}

		#endregion
	}
}