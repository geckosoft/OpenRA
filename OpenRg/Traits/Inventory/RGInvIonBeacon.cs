namespace OpenRg.Traits.Inventory
{
	class RGInvIonBeaconInfo : RGInventoryItemInfo<RGInvIonBeacon>
	{

	}

	class RGInvIonBeacon : RGInventoryItem
	{
		public RGInvIonBeacon()
			: base("Ion Cannon Beacon", 1, 0)
		{
		}

		public override void OnInfoAssigned()
		{
			Settings = RGInventorySettings.OwnedByAvatar; /* only goes away if the avatar dies */
			ItemType = RGInventoryItemType.Beacon; /* just to mark it as a beacon */
		}
	}
}
