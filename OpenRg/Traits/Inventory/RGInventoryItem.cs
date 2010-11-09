using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits.Inventory
{

	public class RGInventoryItemInfo<T> : RGInventoryItemInfo where T : RGInventoryItem, new()
	{
		public override object Create(ActorInitializer init)
		{
			var x = new T();
			x.AssignInfo(init.self, this);

			return x;
		}
	}

	public class RGInventoryItemInfo : ITraitInfo
	{

		public readonly int Max = 1;
		public readonly int Amount = 0;
		public readonly bool AllowRefill = false;

		public virtual object Create(ActorInitializer init)
		{
			var x = new RGInventoryItem();
			x.AssignInfo(init.self, this);

			return x;
		}
	}

	public class RGInventoryItem : IRGInventoryItem
	{
		public string Name { get; set; }
		public RGInventoryItemInfo Info;
		public Actor Owner;

		public void AssignInfo(Actor self, RGInventoryItemInfo info)
		{
			Owner = self;
			Info = info;


			Max = Info.Max;
			Amount = Info.Amount;
			AllowRefill = Info.AllowRefill;

			OnInfoAssigned();
		}

		public RGInventoryItem()
			: this("", 1, 0, "", "")
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

		public RGInventoryItemType ItemType { get; set; }

		public RGInventorySettings Settings { get; set; }
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

		public RGInventoryItem(string name, int max, int amount)
			: this(name, max, amount, "", "")
		{

		}

		public RGInventoryItem(string name, int max, int amount, string description)
			: this(name, max, amount, description, "")
		{

		}

		public RGInventoryItem(string name, int max, int amount, string description, string icon)
		{
			Name = name;
			Max = max;
			Amount = amount;
			Description = description;
			Icon = icon;

			ItemType = RGInventoryItemType.Item;
			Settings = RGInventorySettings.IsAnItem;
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
