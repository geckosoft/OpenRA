#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.FileFormats
{
	public static class Format01
	{
		public static Action<int, string> OnConvert = (a, b) => { };
		static byte[] Decompress(byte[] data)
		{
			using (var compressedStream = new MemoryStream(data))
			using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
			using (var resultStream = new MemoryStream())
			{
				var buffer = new byte[4096];
				int read;

				while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
						resultStream.Write(buffer, 0, read);

				return resultStream.ToArray();
			}
		}

		public static byte[] Process(Stream stream, ImageHeader h)
		{
			stream.Position = h.Offset;

			var reader = new BinaryReader(stream);

			var count = reader.ReadInt32();

			var data = reader.ReadBytes(count);

			return Decompress(data);
		}

		static byte[] Convert(Palette palette, Bitmap bitmap)
		{
			var originalData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

			var q = new byte[bitmap.Width * bitmap.Height];
			unsafe
			{
				var qo = (byte*)originalData.Scan0.ToPointer();
				var stride = originalData.Stride;

				for (int counter = 0; counter < bitmap.Width * bitmap.Height; counter += 1)
				{
					var c = Color.FromArgb(255, qo[counter * 4 + 2], qo[counter * 4 + 1], qo[counter * 4 + 0]);
					var nc = palette.GetNearestColorIndex(c);

					q[counter] = (byte)nc;
				}
			}
			using (var ms = new MemoryStream())
			{
				using (var zip = new GZipStream(ms, CompressionMode.Compress))
				{
					using (var w = new BinaryWriter(zip))
						w.Write(q);
				}

				return ms.ToArray();
			}
		}

		public static ShpWriter Convert(string paletteFile, string[] sourceFiles, bool remapTransparant)
		{
			// Load the palette
			var palette = new Palette(File.OpenRead(paletteFile), remapTransparant);

			Size? textureSize = null;

			// Load & process all sources
			ShpWriter shpWriter = null;

			int i = 0;
			foreach (var sourceFile in sourceFiles)
			{
				if (OnConvert != null)
					OnConvert(i, sourceFile);
				var bitmap = Format00.ConvertTo32bppRgb((Bitmap)Image.FromFile(sourceFile), palette);

				if (textureSize == null)
				{
					if (bitmap.Size.Width > ushort.MaxValue || bitmap.Size.Height > ushort.MaxValue)
						throw new BadImageFormatException("Image too large.", sourceFile);

					textureSize = bitmap.Size;
					shpWriter = new ShpWriter((ushort)bitmap.Size.Width, (ushort)bitmap.Size.Height, Format.Format01);
				}
				else if ((textureSize.Value.Height != bitmap.Size.Height) || (textureSize.Value.Width != bitmap.Size.Width))
				{
					throw new BadImageFormatException("All source files must have an equal size.", sourceFile);
				}

				shpWriter.Headers.Add(new ImageHeader{Format = Format.Format01, Image = Convert(palette, bitmap) });
				i++;
			}

			return shpWriter;
		}

		public static ShpWriter Convert(string paletteFile, string[] sourceFiles)
		{
			return Convert(paletteFile, sourceFiles, true);
		}
	}
}
