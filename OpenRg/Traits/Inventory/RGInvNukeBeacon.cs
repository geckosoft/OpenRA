namespace OpenRg.Traits.Inventory
{
	class RGInvNukeBeaconInfo : RGInventoryItemInfo<RGInvNukeBeacon>
	{

	}

	class RGInvNukeBeacon : RGInventoryItem
	{
		public RGInvNukeBeacon()
			: base("Nuke Beacon", 1, 0)
		{
		}

		public override void OnInfoAssigned()
		{
			Settings = RGInventorySettings.OwnedByAvatar; /* only goes away if the avatar dies */
			ItemType = RGInventoryItemType.Beacon; /* just to mark it as a beacon */
		}
	}
}
