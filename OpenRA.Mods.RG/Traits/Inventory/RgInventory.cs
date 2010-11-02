using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Inventory
{
	public class RgInventoryInfo : ITraitInfo
	{
		public object Create(ActorInitializer init)
		{
			return new RgInventory(init.self, this);
		}
	}
	/// <summary>
	/// 
	/// @todo Implement a way to sync-check the inventory (a synced field containing a custom checksum?)
	/// </summary>
	public class RgInventory : INotifyDamage, IRNotifyUnitSwitch, ITick
	{
		public RgInventoryInfo Info;
		public Actor Self;

		public readonly List<IInventoryItem> Owned = new List<IInventoryItem>();

		public RgInventory(Actor self, RgInventoryInfo info)
		{
			Self = self;
			Info = info;
		}

		public bool Has<T>() where T : IInventoryItem
		{
			return Owned.Where(a => a is T).FirstOrDefault() != null;
		}

		public bool Has(IInventoryItem item)
		{
			return Owned.Where(a => a.GetType() == item.GetType()).FirstOrDefault() != null;
		}

		public IInventoryItem Get(IInventoryItem item)
		{
			return Owned.Where(a => a.GetType() == item.GetType()).FirstOrDefault();
		}

		public T Get<T>() where T : IInventoryItem
		{
			return (T)Owned.Where(a => a is T).FirstOrDefault();
		}

		public bool Add(IInventoryItem item)
		{
			if (!Has(item))
			{
				Owned.Add(item);

				return true;
			}

			return false; /* cant add existing items! Access them directy pls by using Get<>() */
		}

		/// <summary>
		/// These items never go away (use it for example for scoring, ...)
		/// </summary>
		public List<IInventoryItem> OwnedByPlayer
		{
			get
			{
				return
					Owned.Where(i => (i.Settings & EInventorySettings.OwnedByPlayer) == EInventorySettings.OwnedByPlayer).ToList();
			}
		}

		public List<IInventoryItem> OwnedByAvatar
		{
			get
			{
				return
					Owned.Where(i => (i.Settings & EInventorySettings.OwnedByAvatar) == EInventorySettings.OwnedByAvatar).ToList();
			}
		}

		public List<IInventoryItem> OwnedByUnit
		{
			get
			{
				return
					Owned.Where(i => (i.Settings & EInventorySettings.OwnedByUnit) == EInventorySettings.OwnedByAvatar).ToList();
			}
		}
		/// <summary>
		/// Refills all items where refilling is allowed
		/// </summary>
		public void Refill()
		{
			Owned.Where(a => a.AllowRefill).Do(a => a.Refill());
		}

		/// <summary>
		/// Called when any unit receives damage. Only does something when the player his avatar dies.
		/// </summary>
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.Owner != Self.Owner)
				return;
			var rgplayer = self.Owner.PlayerActor.TraitOrDefault<RgPlayer>();
			
			if (rgplayer == null)
				return;

			/* if it is a player-owned unit, it ALWAYS is the avatar! */
			if (e.DamageStateChanged && e.DamageState == DamageState.Dead && self == rgplayer.Avatar)
			{
				/* remove avatar owned items */

				OwnedByAvatar.Where(a => !a.Sticky).Do(a => a.Removed = true); 
				Owned.RemoveAll(a => a.Settings == EInventorySettings.OwnedByAvatar && !a.Sticky);
				OwnedByAvatar.Do(a => a.TakeAll());
			}
		}

		/// <summary>
		/// Called when the player switched infantry type
		/// </summary>
		public void UnitSwitched(Actor oldMe, Actor newMe)
		{
			if (newMe.Owner != Self.Owner)
				return;
			var rgplayer = newMe.Owner.PlayerActor.TraitOrDefault<RgPlayer>();

			if (rgplayer == null)
				return;

			/* remove unit owned items */
			OwnedByUnit.Where(a => !a.Sticky).Do(a => a.Removed = true);
			Owned.RemoveAll(a => a.Settings == EInventorySettings.OwnedByUnit && !a.Sticky);
			OwnedByUnit.Do(a => a.TakeAll());
		}

		public void Tick(Actor self)
		{
			var rgplayer = RgPlayer.Get(self);
			if (rgplayer == null)
				return;
			var itms = self.TraitsImplementing<IInventoryItem>();

			if (rgplayer.Avatar != null && rgplayer.Avatar.IsInWorld && !rgplayer.Avatar.Destroyed)
			{
				itms = itms.Concat(rgplayer.Avatar.TraitsImplementing<IInventoryItem>());
			}
			/* this adds missing inv items added from traits */
			itms.Where(a => Get(a) == null && !a.Removed).Do(a => Add(a));
		}
	}
}
