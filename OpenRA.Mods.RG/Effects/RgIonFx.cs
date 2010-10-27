using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Effects
{
	class RgIonFxInfo : TraitInfo<RgIonFx> { }

	public class RgIonFx : IPaletteModifier, ITick
	{
		const int FxLength = 20;
		int _remainingFrames;

		public void Enable()
		{
			_remainingFrames = FxLength;
		}

		public void Tick(Actor self)
		{
			if (_remainingFrames > 0)
				_remainingFrames--;
		}

		public void AdjustPalette(Dictionary<string, Palette> palettes)
		{
			if (_remainingFrames == 0)
				return;

			var frac = (float)_remainingFrames / FxLength;

			var excludePalettes = new List<string>() { "cursor", "chrome", "colorpicker" };
			foreach (var pal in palettes)
			{
				if (excludePalettes.Contains(pal.Key))
					continue;

				for (var x = 0; x < 256; x++)
				{
					var orig = pal.Value.GetColor(x);
					var blue = Color.FromArgb(orig.A/2, Color.Blue.R, Color.Blue.G, Color.Blue.B);
					pal.Value.SetColor(x, OpenRA.Graphics.Util.Lerp(frac, orig, blue));
				}
			}
		}
	}
}
