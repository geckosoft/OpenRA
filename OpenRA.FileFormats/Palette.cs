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
using System.Linq;

namespace OpenRA.FileFormats
{
	public class Palette
	{
		uint[] colors;
		public Color GetColor(int index)
		{
			return Color.FromArgb((int)colors[index]);
		}
		
		public uint GetNearestColorRGBA(Color original)
		{
			return Values.OrderBy(v => GetColorDistance(original, Color.FromArgb((int) v))).First();
		}


		public int GetNearestColorIndex(Color color)
		{
			return Values.ToList().IndexOf(GetNearestColorRGBA(color));
		}

		public static int GetColorDistance(Color original, Color target)
		{
			var redDistance = original.R - target.R;
			var greenDistance = original.G - target.G;
			var blueDistance = original.B - target.B;

			return (redDistance * redDistance) + (greenDistance * greenDistance) + (blueDistance * blueDistance);
		}

		public void SetColor(int index, Color color)
		{
			colors[index] = (uint)color.ToArgb();
		}
		
		public void SetColor(int index, uint color)
		{
			colors[index] = (uint)color;
		}
				
		public uint[] Values
		{			
			get { return colors; }
		}

		public Palette(Stream s, bool remapTransparent)
		{
			colors = new uint[256];
			
			using (BinaryReader reader = new BinaryReader(s))
			{
				for (int i = 0; i < 256; i++)
				{
					byte r = (byte)(reader.ReadByte() << 2);
					byte g = (byte)(reader.ReadByte() << 2);
					byte b = (byte)(reader.ReadByte() << 2);
					colors[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
				}
			}

			colors[0] = 0;
			if (remapTransparent)
			{
				colors[1] = 178u << 24; // Hack for d2k; may have side effects
				colors[3] = 178u << 24;
				colors[4] = 140u << 24;
			}
		}

		public Palette(Palette p, IPaletteRemap r)
		{
			colors = new uint[256];
			for(int i = 0; i < 256; i++)
				colors[i] = (uint)r.GetRemappedColor(Color.FromArgb((int)p.colors[i]),i).ToArgb();
		}
		
		public Palette(Palette p)
		{
			colors = (uint[])p.colors.Clone();
		}

		public static ColorPalette CreateSystemPalette(Palette p)
		{
			ColorPalette pal;
			using (var b = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
				pal = b.Palette;

			for (var i = 0; i < 256; i++)
				pal.Entries[i] = p.GetColor(i);
			return pal;
		}
	}

	public interface IPaletteRemap { Color GetRemappedColor(Color original, int index);	}
}
