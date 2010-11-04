using System;
using System.IO;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgUniqueIdInfo : ITraitInfo
	{
		public string Filename = "RgSerial.txt";

		public object Create(ActorInitializer init)
		{
			return new RgUniqueId(init, this);
		}
	}

	public class RgUniqueId : IResolveOrder
	{
		public string SerialFile = "";
		public string Serial = "";
		public const string ORDER = "RgUniqueId";
		public RgUniqueId(ActorInitializer init, RgUniqueIdInfo info)
		{
			SerialFile = Game.SupportDir + info.Filename;

			if (!File.Exists(SerialFile))
			{
				GenerateSerial();
			}else
			{
				LoadSerial();
			}

			SendSerial(init.self);
		}

		private void SendSerial(Actor self)
		{
			self.World.IssueOrder(new Order(ORDER, self, Serial));
		}

		private void LoadSerial()
		{
			Serial = File.ReadAllText(SerialFile, Encoding.UTF8);
		}

		private void GenerateSerial()
		{
			Serial = Guid.NewGuid().ToString();

			File.WriteAllText(SerialFile, Serial, Encoding.UTF8);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == ORDER && Serial == "")
			{
				Serial = order.OrderString;
			}
		}
	}
}