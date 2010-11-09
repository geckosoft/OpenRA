using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.Graphics;

namespace OpenRg
{
	public interface IRGPostRender
	{
		void RGRenderAfterWorld(WorldRenderer wr, Actor self);
	}

	public interface IRGPreRender
	{
		void RGRenderBeforeWorld(WorldRenderer wr, Actor self);
	}

	public interface IRGAbility
	{

	}
	public interface IRGNotifyUnitSwitched
	{
		void UnitSwitched(Actor oldMe, Actor newMe);
	}

	public interface IRGNotifyProne
	{
		void OnProne(Actor self, bool prone);
	}
	public interface IRGVisor
	{
		bool Enabled { get; set; }
	}



	public interface IRGBeaconTarget
	{
		void DoDisarm(Player disarmer, float amount);
		void TriggerLaunch(Player launcher, Actor launchSite, int ticks);
		int TicksBeforeLaunch { get; }
	}

	[Flags]
	public enum RGInventoryItemType
	{
		Item,
		Weapon,
		Armor,
		Beacon,
		PowerUp,
		Deployable,
		Misc,
	}

	[Flags]
	public enum RGInventorySettings
	{
		IsAnItem,
		OwnedByPlayer, // Player always has access to this item
		OwnedByAvatar, // If the avatar dies, the items are lost
		OwnedByUnit, // If the unit dies, the items are lost 
	}

	public interface IRGInventoryItem
	{
		string Name { get; set; }
		string Description { get; set; }
		string Icon { get; set; }
		int Max { get; set; }
		int Amount { get; set; }
		bool IsHidden { get; set; }
		RGInventoryItemType ItemType { get; set; }
		RGInventorySettings Settings { get; set; }
		void TakeAll();
		bool Sticky { get; set; }
		bool AllowRefill { get; set; }
		void Refill();
		bool Removed { get; set; }
	}
}
