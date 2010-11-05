using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OpenRA.Support
{
	public static class Encryption
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="encryptedData">base64 encrypted data</param>
		/// <param name="encryptionKey">length must be 32</param>
		/// <param name="iv">length must be 16</param>
		/// <returns></returns>
		public static string Decrypt(string encryptedData, string encryptionKey, string iv)
		{
			var key = Encoding.Default.GetBytes(encryptionKey);
			var IV = Encoding.Default.GetBytes(iv);

			String result;

			var rijn = new RijndaelManaged();
			rijn.Mode = CipherMode.ECB;
			rijn.Padding = PaddingMode.Zeros;
			rijn.BlockSize = 256;

			using (var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedData)))
			{
				using (ICryptoTransform decryptor = rijn.CreateDecryptor(key, IV))
				{
					using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (var swDecrypt = new StreamReader(csDecrypt))
						{
							result = swDecrypt.ReadToEnd();
						}
					}
				}
			}
			rijn.Clear();

			return result.Trim();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>		
		/// <param name="encryptionKey">length must be 32</param>
		/// <param name="iv">length must be 16</param>
		/// <returns>base64 encoded encrypted version of message</returns>
		public static string Encrypt(string message, string encryptionKey, string iv)
		{
			var key = Encoding.Default.GetBytes(encryptionKey);
			var IV = Encoding.Default.GetBytes(iv);
			String result;

			var rijn = new RijndaelManaged();
			rijn.Mode = CipherMode.ECB;
			rijn.Padding = PaddingMode.Zeros;
			rijn.BlockSize = 256;

			using (var msEncrypt = new MemoryStream())
			{
				using (ICryptoTransform encryptor = rijn.CreateEncryptor(key, IV))
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (var swEncrypt = new StreamWriter(csEncrypt))
						{
							swEncrypt.Write(message);
						}
					}
				}
				result = Convert.ToBase64String(msEncrypt.ToArray());
			}
			rijn.Clear();

			return result;
		}
	}
}
