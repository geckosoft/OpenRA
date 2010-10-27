using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Inventory
{

	public class InventoryItemInfo<T> : InventoryItemInfo where T : InventoryItem, new()
	{
		public override object Create(ActorInitializer init)
		{
			var x = new T();
			x.AssignInfo(init.self, this);

			return x;
		}
	}

	public class InventoryItemInfo : ITraitInfo
	{

		public readonly int Max = 1;
		public readonly int Amount = 0;
		public readonly bool AllowRefill = false;

		public virtual object Create(ActorInitializer init)
		{
			var x = new InventoryItem();
			x.AssignInfo(init.self, this);

			return x;
		}
	}

	public class InventoryItem : IInventoryItem
	{
		public string Name { get; set; }
		public InventoryItemInfo Info;
		public Actor Self = null;
		
		public void AssignInfo(Actor self, InventoryItemInfo info)
		{
			Self = self;
			Info = info;


			Max = Info.Max;
			Amount = Info.Amount;
			AllowRefill = Info.AllowRefill;

			OnInfoAssigned();
		}

		public InventoryItem() : this("", 1, 0, "", "")
		{
			
		}

		public virtual void OnInfoAssigned()
		{
			
		}
		public string Description
		{
			get;
			set;
		}

		public string Icon
		{
			get;
			set;
		}

		public int Max
		{
			get;
			set;
		}

		public int Amount
		{
			get;
			set;
		}

		public bool IsHidden { get; set; }

		public EInventoryItem ItemType { get; set; }

		public EInventorySettings Settings { get; set; }
		public void TakeAll()
		{
			Amount = 0;
		}

		public bool Sticky { get; set; }

		public bool AllowRefill { get; set; }
		public void Refill()
		{
			Amount = Max;
		}

		public bool Removed { get; set; }

		public InventoryItem(string name, int max, int amount)
			: this(name, max, amount, "", "")
		{

		}

		public InventoryItem(string name, int max, int amount, string description) : this(name, max, amount, description, "")
		{

		}

		public InventoryItem(string name, int max, int amount, string description, string icon)
		{
			Name = name;
			Max = max;
			Amount = amount;
			Description = description;
			Icon = icon;

			ItemType = EInventoryItem.Item;
			Settings = EInventorySettings.IsAnItem;
			IsHidden = false;
			Sticky = true;
		}

		public bool CanTake(int amount)
		{
			if (Amount < amount)
				return false;

			return true;
		}

		public bool Take(int amount)
		{
			if (Amount < amount)
				return false;

			Amount -= amount;
			return true;
		}

		/// <summary>
		/// Returns how many items were given (keeping in mind the MAX)
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public void Give(int amount)
		{
			if (Amount == Max)
				return;

			Amount += amount;

			if (Amount > Max)
				Amount = Max;
		}
	}
}
