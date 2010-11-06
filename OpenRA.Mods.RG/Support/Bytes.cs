using System;

namespace OpenRA.Mods.Rg.Support
{
	public class Bytes
	{
		public static void SetSize(ref byte[] bytes, int size)
		{
			var tempReDim = new byte[size];
			if (bytes != null)
				Array.Copy(bytes, tempReDim,
			Math.Min(bytes.Length, tempReDim.Length));
			bytes = tempReDim;
		}

		public static byte[] CopyNew(ref byte[] srcBytes, int srcOffset)
		{
			if (srcBytes.Length - srcOffset < 0)
				throw new Exception("Cannot create a new array of bytes with size < 0");
			var tempReDim = new byte[srcBytes.Length - srcOffset];

			Array.Copy(srcBytes, srcOffset, tempReDim, 0, srcBytes.Length - srcOffset); // + 1); // Is the +1 part correct? Can anyone check pls? :)

			return tempReDim;
		}

		public static byte[] CopyNew(ref byte[] srcBytes, int srcOffset, int length)
		{
			if ((srcBytes.Length - srcOffset < 0) || (length < 0))
				throw new Exception("Cannot create a new array of bytes with size < 0");

			if (length > srcBytes.Length - srcOffset)
			{
				length = srcBytes.Length - srcOffset;
			}

			var tempReDim = new byte[length];
			Array.Copy(srcBytes, srcOffset, tempReDim, 0, length);

			return tempReDim;
		}

		public static byte[] ConvertFromASCII(string myData)
		{
			var encoding = new System.Text.ASCIIEncoding();
			return encoding.GetBytes(myData);
		}
	}
}
