using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGDamageRulesInfo : TraitInfo<RGDamageRules>
	{

	}

	public class RGDamageRules : IDamageModifier
	{
		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			var modifier = 1f;

			if (!attacker.Destroyed && attacker.TraitsImplementing<RGSniperVisor>().Any() && !attacker.TraitsImplementing<RGSniperVisor>().Where(t => t.Enabled).Any())
			{
				// has a sniper visor AND is not enabled (ie not prone)
				modifier *= 0.25f; // Only 25% damage when not prone with the sniper

			}

			return modifier;
		}
	}
}
