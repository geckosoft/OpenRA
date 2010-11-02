namespace OpenRA.Mods.Rg.Traits.Inventory
{
	class InvNukeBeaconInfo : InventoryItemInfo<InvNukeBeacon>
	{
		public readonly int Max = 1;
		public readonly int Amount = 0;
	}

	class InvNukeBeacon : InventoryItem
	{
		public InvNukeBeacon()
			: base("Nuke Beacon", 1, 0)
		{
			Settings = EInventorySettings.OwnedByAvatar; /* only goes away if the avatar dies */
			ItemType = EInventoryItem.Beacon; /* just to mark it as a beacon */
		}

		public override void OnInfoAssigned()
		{
		}
	}
}
