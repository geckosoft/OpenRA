using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits.Inventory
{
	public class RGInventoryInfo : ITraitInfo
	{
		public object Create(ActorInitializer init)
		{
			return new RGInventory(init.self, this);
		}
	}
	/// <summary>
	/// 
	/// @todo Implement a way to sync-check the inventory (a synced field containing a custom checksum?)
	/// </summary>
	public class RGInventory : INotifyDamage, IRGNotifyUnitSwitched, ITick
	{
		public RGInventoryInfo Info;
		public Actor Owner;

		public readonly List<IRGInventoryItem> Owned = new List<IRGInventoryItem>();

		public RGInventory(Actor owner, RGInventoryInfo info)
		{
			Owner = owner;
			Info = info;
		}

		public bool Has<T>() where T : IRGInventoryItem
		{
			return Owned.Where(a => a is T).FirstOrDefault() != null;
		}

		public bool Has(IRGInventoryItem item)
		{
			return Owned.Where(a => a.GetType() == item.GetType()).FirstOrDefault() != null;
		}

		public IRGInventoryItem Get(IRGInventoryItem item)
		{
			return Owned.Where(a => a.GetType() == item.GetType()).FirstOrDefault();
		}

		public T Get<T>() where T : IRGInventoryItem
		{
			return (T)Owned.Where(a => a is T).FirstOrDefault();
		}

		public bool Add(IRGInventoryItem item)
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
		public List<IRGInventoryItem> OwnedByPlayer
		{
			get
			{
				return
					Owned.Where(i => (i.Settings & RGInventorySettings.OwnedByPlayer) == RGInventorySettings.OwnedByPlayer).ToList();
			}
		}

		public List<IRGInventoryItem> OwnedByAvatar
		{
			get
			{
				return
					Owned.Where(i => (i.Settings & RGInventorySettings.OwnedByAvatar) == RGInventorySettings.OwnedByAvatar).ToList();
			}
		}

		public List<IRGInventoryItem> OwnedByUnit
		{
			get
			{
				return
					Owned.Where(i => (i.Settings & RGInventorySettings.OwnedByUnit) == RGInventorySettings.OwnedByUnit).ToList();
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
			if (self.Owner != Owner.Owner)
				return;
			var rgplayer = (RGPlayer) self;

			if (rgplayer == null)
				return;

			/* if it is a player-owned unit, it ALWAYS is the avatar! */
			if (e.DamageStateChanged && e.DamageState == DamageState.Dead && self == rgplayer.Avatar)
			{
				/* remove avatar owned items */

				OwnedByAvatar.Where(a => !a.Sticky).Do(a => a.Removed = true);
				Owned.RemoveAll(a => a.Settings == RGInventorySettings.OwnedByAvatar && !a.Sticky);
				OwnedByAvatar.Do(a => a.TakeAll());
			}
		}

		/// <summary>
		/// Called when the player switched infantry type
		/// </summary>
		public void UnitSwitched(Actor oldMe, Actor newMe)
		{
			if (newMe.Owner != Owner.Owner)
				return;
			var rgplayer = ((RGPlayer) newMe);

			if (rgplayer == null)
				return;

			/* remove unit owned items */
			OwnedByUnit.Where(a => !a.Sticky).Do(a => a.Removed = true);
			Owned.RemoveAll(a => a.Settings == RGInventorySettings.OwnedByUnit && !a.Sticky);
			OwnedByUnit.Do(a => a.TakeAll());
		}

		public void Tick(Actor self)
		{
			var rgplayer = ((RGPlayer) self);
			if (rgplayer == null)
				return;
			var itms = self.TraitsImplementing<IRGInventoryItem>();

			if (rgplayer.Avatar != null && rgplayer.Avatar.IsInWorld && !rgplayer.Avatar.Destroyed)
			{
				itms = itms.Concat(rgplayer.Avatar.TraitsImplementing<IRGInventoryItem>());
			}
			/* this adds missing inv items added from traits */
			itms.Where(a => Get(a) == null && !a.Removed).Do(a => Add(a));
		}
	}
}
