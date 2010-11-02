namespace OpenRA.Mods.Rg.Traits.Inventory
{
	class InvIonBeaconInfo : InventoryItemInfo<InvIonBeacon>
	{
		public readonly int Max = 1;
		public readonly int Amount = 0;
	}

	class InvIonBeacon : InventoryItem
	{
		public InvIonBeacon() : base("Ion Cannon Beacon", 1, 0)
		{
			Settings = EInventorySettings.OwnedByAvatar; /* only goes away if the avatar dies */
			ItemType = EInventoryItem.Beacon; /* just to mark it as a beacon */
		}

		public override void OnInfoAssigned()
		{
			Max = ((InvIonBeaconInfo)Info).Max;
			Amount = ((InvIonBeaconInfo)Info).Amount;
		}
	}
}
