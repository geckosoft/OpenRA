
using System;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Inventory
{
	class InvArmorInfo : InventoryItemInfo<InvArmor>
	{
		public readonly string ArmorType = "Flesh"; // Be used later on when rendering the hud
	}

	class InvArmor : InventoryItem, IDamageModifier
	{
		public InvArmor()
		{

		}

		public string ArmorType = "";

		public new InvArmorInfo Info
		{
			get { return (InvArmorInfo) base.Info; }
		}
		public override void OnInfoAssigned()
		{
			Settings = EInventorySettings.OwnedByUnit;
			ItemType = EInventoryItem.Armor;
			Sticky = false; // Force not-sticky (gets deleted from the inventory when 'removed')
			// Refilling allowed (ie infantry refill)
			AllowRefill = true; 


			ArmorType = Info.ArmorType;
		}

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			if (Amount == 0)
				return 1; /* change nothing */

			/* can take the full blow */
			if (warhead.Damage <= Amount)
			{
				Take( warhead.Damage);

				return 0;
			}

			// see how much is covered
			float covered = 1f/warhead.Damage*Amount;
			TakeAll();

			return 1f - covered;
		}
	}
}
