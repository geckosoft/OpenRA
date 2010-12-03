using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;

namespace gfx2shp
{
	class Program
	{
		static List<string> Files = new List<string>();

		static void Main(string[] args)
		{
			Format00.OnConvert = OnConvert;
			Format01.OnConvert = OnConvert;

			Console.WriteLine();
			Console.WriteLine("GFX2SHP - V" + Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("Written by Gecko");
			Console.WriteLine();

			if (args.Count() < 4 || (!"0 1".Contains(args[0])))
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("gfx2shp.exe format(0|1) paletteFile targetShp srcgfx*");
				Console.WriteLine();
				return;
			}
			var format = (Format)int.Parse(args[0]);

			Files = args.ToList();
			Files.RemoveRange(0, 3);

			Console.WriteLine("Converting:");
			var writer = ShpWriter.Convert(format, args[1], Files.ToArray());
			Console.WriteLine();
			Console.WriteLine("Exporting shp to " + args[2]);
			writer.Save(args[2]);
			Console.WriteLine();
			Console.WriteLine("Conversion done!");
		}

		static void OnConvert(int cur, string file)
		{
			Console.WriteLine("[" + (cur+1) + "/" + Files.Count + "] " + file);
		}
	}
}
