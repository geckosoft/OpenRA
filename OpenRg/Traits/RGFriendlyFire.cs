using OpenRA;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGFriendlyFireInfo : ITraitInfo
	{
		public readonly bool Allow;

		/* only because we have other ctors */

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGFriendlyFire(init.self, this);
		}

		#endregion
	}

	public class RGFriendlyFire : IDamageModifier
	{
		public RGFriendlyFireInfo Info;
		public Actor Self;

		public RGFriendlyFire(Actor self, RGFriendlyFireInfo info)
		{
			Self = self;
			Info = info;
		}

		#region IDamageModifier Members

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			if (!Info.Allow)
			{
				/* Disallow dmg to allies, EXCEPT to self and EXCEPT if healing done */
				if ((attacker.Owner.Stances[Self.Owner] == Stance.Ally && Self != attacker) && (warhead.Damage > 0))
					return 0;

				if (warhead.Damage < 0 && Self == attacker)
				{
					return 0; // Cannot heal self! // TODO move to its own trait!
				}
			}

			return 1;
		}

		#endregion
	}
}