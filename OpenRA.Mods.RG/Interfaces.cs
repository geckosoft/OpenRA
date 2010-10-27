using System;
using OpenRA.Graphics;

namespace OpenRA.Mods.Rg
{
	public interface IRgPostRender
	{
		void RgRenderAfterWorld(WorldRenderer wr, Actor self);
	}

	public interface IRgPreRender
	{
		void RgRenderBeforeWorld(WorldRenderer wr, Actor self);
	}

	public interface IRgAbility
	{
		
	}

	[Flags]
	public enum EInventoryItem
	{
		Item,
		Weapon,
		Armor,
		Beacon ,
		PowerUp ,
		Deployable,
		Misc,
	}

	[Flags]
	public enum EInventorySettings
	{
		IsAnItem,
		OwnedByPlayer, // Player always has access to this item
		OwnedByAvatar, // If the avatar dies, the items are lost
		OwnedByUnit, // If the unit dies, the items are lost 
	}

	public interface IInventoryItem
	{
		string Name { get; set; }
		string Description { get; set; }
		string Icon { get; set; }
		int Max { get; set; }
		int Amount { get; set; }
		bool IsHidden { get; set; }
		EInventoryItem ItemType { get; set; }
		EInventorySettings Settings { get; set; }
		void TakeAll();
		bool Sticky { get; set; }
		bool AllowRefill { get; set; }
		void Refill();
		bool Removed { get; set; }
	}

	public interface IRNotifyUnitSwitch
	{
		void UnitSwitched(Actor oldMe, Actor newMe);
	}
}