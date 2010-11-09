using OpenRA.GameRules;
using OpenRg.Traits.Inventory;

namespace OpenRg.Traits.Abilities
{
	class RGInvMineInfo : RGInventoryItemInfo<RGInvMine>
	{

	}

	class RGInvMine : RGInventoryItem
	{
		public override void OnInfoAssigned()
		{
			Settings = RGInventorySettings.OwnedByUnit; /* only goes away if the avatar dies */
			ItemType = RGInventoryItemType.Misc; /* just to mark it as a beacon */
			Sticky = false; // Force not-sticky
			AllowRefill = true; // Refilling allowed
		}
	}
}
