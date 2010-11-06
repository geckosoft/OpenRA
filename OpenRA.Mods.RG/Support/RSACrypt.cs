using System;
using System.IO;
using System.Security.Cryptography;

namespace OpenRA.Mods.Rg.Support
{
	public class RSACrypt
	{
		protected string Key;
		public RSACryptoServiceProvider RSA;

		public RSACrypt( string key )
		{
			LoadRSA();

			RSA.FromXmlString(key);
			Key = key;
		}

		public string GetKey( bool privateAlso )
		{
			return RSA.ToXmlString(privateAlso);
		}

		public void LoadRSA()
		{
			const int PROVIDER_RSA_FULL = 1;
			const string CONTAINER_NAME = "RSACryptContainer";
			var cspParams = new CspParameters(PROVIDER_RSA_FULL)
	                          	{
	                          		KeyContainerName = CONTAINER_NAME,
	                          		Flags = CspProviderFlags.UseMachineKeyStore,
	                          		ProviderName = "Microsoft Strong Cryptographic Provider"
	                          	};
			RSA = new RSACryptoServiceProvider(cspParams);
		}

		public byte[] Encrypt( byte[] data )
		{
			if (data.Length <= 100)
			{
				return RSA.Encrypt(data, false);
			}

			var result = new byte[0];
			byte[] currentset;
			int curpos = 0;
			while (curpos < data.Length)
			{
				currentset = Bytes.CopyNew(ref data, curpos, 100);
				curpos += currentset.Length;
				currentset = RSA.Encrypt(currentset, false);
				int resLen = result.Length;
				Bytes.SetSize(ref result, result.Length + currentset.Length);
				Array.Copy(currentset, 0, result, resLen, currentset.Length);
			}

			return result;
		}

		public byte[] Decrypt( byte[] data )
		{
			if (data.Length <= 128)
			{
				return RSA.Decrypt(data, false);
			}

			var result = new byte[0];
			byte[] currentset;
			int curpos = 0;
			while (curpos < data.Length)
			{
				currentset = Bytes.CopyNew(ref data, curpos, 128);
				curpos += currentset.Length;
				currentset = RSA.Decrypt(currentset, false);
				int resLen = result.Length;
				Bytes.SetSize(ref result, result.Length + currentset.Length);
				Array.Copy(currentset, 0, result, resLen, currentset.Length);
			}

			return result;
		}

		public static bool GeneratePair( string publicKey, string privateKey )
		{
			try
			{
				RSACryptoServiceProvider rsa;
				const int PROVIDER_RSA_FULL = 1;
				const string CONTAINER_NAME = "RSACryptContainer";
				var cspParams = new CspParameters(PROVIDER_RSA_FULL)
				                          {
				                          	KeyContainerName = CONTAINER_NAME,
				                          	Flags = CspProviderFlags.UseMachineKeyStore,
				                          	ProviderName = "Microsoft Strong Cryptographic Provider"
				                          };
				rsa = new RSACryptoServiceProvider(cspParams);

				//provide public and private RSA params
				var writer = new StreamWriter(privateKey);
				string publicPrivateKeyXML = rsa.ToXmlString(true);
				writer.Write(publicPrivateKeyXML);
				writer.Close();

				//provide public only RSA params
				writer = new StreamWriter(publicKey);
				string publicOnlyKeyXML = rsa.ToXmlString(false);
				writer.Write(publicOnlyKeyXML);
				writer.Close();

				return true;
			}
			catch
			{
				return false;
			}
		}

		public static bool GeneratePair( ref string publicKey, ref string privateKey )
		{
			try
			{
				RSACryptoServiceProvider rsa;
				const int PROVIDER_RSA_FULL = 1;
				const string CONTAINER_NAME = "RSACryptContainer";
				var cspParams = new CspParameters(PROVIDER_RSA_FULL)
				                          {
				                          	KeyContainerName = CONTAINER_NAME,
				                          	Flags = CspProviderFlags.UseMachineKeyStore,
				                          	ProviderName = "Microsoft Strong Cryptographic Provider"
				                          };
				rsa = new RSACryptoServiceProvider(cspParams);
				privateKey = rsa.ToXmlString(true);
				publicKey = rsa.ToXmlString(false);

				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}