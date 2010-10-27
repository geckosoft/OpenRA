namespace OpenRA.Mods.Rg.Traits.Inventory
{
	class InvMineInfo : InventoryItemInfo<InvMine>
	{

	}

	class InvMine : InventoryItem
	{
		public InvMine()
		{
		}

		public override void OnInfoAssigned()
		{
			Settings = EInventorySettings.OwnedByUnit; /* only goes away if the avatar dies */
			ItemType = EInventoryItem.Misc; /* just to mark it as a beacon */
			Sticky = false; // Force not-sticky
			AllowRefill = true; // Refilling allowed
		}
	}
}
