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
using OpenRA.FileFormats.Graphics;

namespace OpenRA.FileFormats
{
	public static class Format00
	{
		public static Action<int,  string> OnConvert = (a,b) => { };

		public static byte[] Process(Stream stream, ImageHeader h)
		{
			stream.Position = h.Offset;

			var reader = new BinaryReader(stream);

			var count = reader.ReadInt32();

			return reader.ReadBytes(count);
		}

		internal static Bitmap ConvertTo32bppRgb(Bitmap b1, Palette palette)
		{
			if (b1.PixelFormat == PixelFormat.Format32bppRgb)
				return b1;

			var b2 = new Bitmap(b1.Size.Width, b1.Size.Height, PixelFormat.Format32bppRgb);
			var g = System.Drawing.Graphics.FromImage(b2);
			g.DrawImage(b1,new Point(0,0));
			g.Dispose();

			return b2;
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

				var bitmap = ConvertTo32bppRgb((Bitmap)Image.FromFile(sourceFile), palette);

				if (textureSize == null)
				{
					if (bitmap.Size.Width > ushort.MaxValue || bitmap.Size.Height > ushort.MaxValue)
						throw new BadImageFormatException("Image too large.", sourceFile);

					textureSize = bitmap.Size;
					shpWriter = new ShpWriter((ushort)bitmap.Size.Width, (ushort)bitmap.Size.Height, Format.Format00);
				}
				else if ((textureSize.Value.Height != bitmap.Size.Height) || (textureSize.Value.Width != bitmap.Size.Width))
				{
					throw new BadImageFormatException("All source files must have an equal size.", sourceFile);
				}

				shpWriter.Headers.Add(new ImageHeader{Format = Format.Format00, Image = Convert(palette, bitmap) });
				i++;
			}

			return shpWriter;
		}

		public static ShpWriter Convert(string paletteFile, string[] sourceFiles)
		{
			return Convert(paletteFile, sourceFiles, true);
		}


		internal static byte[] Convert(Palette palette, Bitmap bitmap)
		{
			var originalData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

			var q = new byte[bitmap.Width * bitmap.Height]; 
			unsafe
			{
				var qo = (byte*)originalData.Scan0.ToPointer();
				var stride = originalData.Stride;

				for (int counter = 0; counter < bitmap.Width * bitmap.Height; counter += 1)
				{
					var c = Color.FromArgb(255, qo[counter * 4 +2], qo[counter * 4 + 1], qo[counter * 4 + 0]);
					var nc = palette.GetNearestColorIndex(c);

					q[counter] = (byte)nc;
				}
			}

			return q;
		}
	}
}
