using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OpenRA.Mods.Rg.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgPlayerSerialInfo : ITraitInfo
	{
		public string Prefix = "RG_KEY";

		public object Create(ActorInitializer init)
		{
			return new RgPlayerSerial(init, this);
		}
	}

	public class RgPlayerSerial : IResolveOrder
	{
		public string PublicFile = "";
		public string PrivateFile = "";
		public string PublicKey = "";
		public string PrivateKey = "";
		public string Serial = "";
		public const string REQUEST_ORDER = "RG_PLAYER_SERIAL_REQUEST";
		public const string REPLY_ORDER = "RG_PLAYER_SERIAL_REPLAY";
		public const string VALIDATE_ORDER = "RG_PLAYER_SERIAL_VALIDATE";
		public UTF8Encoding Encoding = new UTF8Encoding();
		public RgPlayerSerial(ActorInitializer init, RgPlayerSerialInfo info)
		{
			PublicFile = Game.SupportDir + info.Prefix + ".pkey";
			PrivateFile = Game.SupportDir + info.Prefix + ".key";

			if (!File.Exists(PublicFile) || !File.Exists(PrivateFile))
			{
				GenerateKeys();
			}else
			{
				LoadKeys();
			}

			if (Game.IsHost)
			{
				SendServerKeys(init.self);
			}
		}

		private void SendServerKeys(Actor self)
		{
			self.World.IssueOrder(new Order(REQUEST_ORDER, self, PublicKey));
		}

		private void LoadKeys()
		{
			PublicKey = File.ReadAllText(PublicFile, global::System.Text.Encoding.UTF8);
			PrivateKey = File.ReadAllText(PrivateFile, global::System.Text.Encoding.UTF8);

			var publicKey = Encoding.GetBytes(PublicKey);

			SHA512 shaM = new SHA512Managed();
			var result = shaM.ComputeHash(publicKey);

			Serial = result.ToHex();
		}

		private void GenerateKeys()
		{
			RSACrypt.GeneratePair(ref PublicKey, ref PrivateKey);

			File.WriteAllText(PublicFile, PublicKey, global::System.Text.Encoding.UTF8);
			File.WriteAllText(PrivateFile, PrivateKey, global::System.Text.Encoding.UTF8);

			var publicKey = Encoding.GetBytes(PublicKey);

			SHA512 shaM = new SHA512Managed();
			var result = shaM.ComputeHash(publicKey);

			Serial = result.ToHex();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == REQUEST_ORDER && !Game.IsHost)
			{

			}
		}
	}
}