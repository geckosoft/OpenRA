using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRg.Traits
{
	public class RGRenderNameInfo : TraitInfo<RGRenderName>
	{

	}

	public class RGRenderName : IRGPostRender
	{
		public void RGRenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (self.Destroyed || !self.IsInWorld || self.Owner.NonCombatant || self.Owner.PlayerRef.OwnsWorld)
				return;

			RectangleF bounds = self.GetBounds(true);

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

			DrawName(self, xy, Xy);
		}

		public static void DrawName(Actor self, float2 xY, float2 xy)
		{
			var toRender = self.Owner.PlayerName;
			var size = Game.Renderer.BoldFont.Measure(toRender);
			var dist = xy.X - xY.X;

			var loc = new float2(xY.X + dist / 2 - size.X / 2, xy.Y - size.Y);
			var color = Color.Red;
			if (self.Owner.Country.Race.ToLower() == "gdi")
				color = Color.Yellow;
			bool shouldRender = false;
			if (RGGame.LocalPlayer == null || self == RGGame.LocalPlayer.Actor) // spectator
			{
				shouldRender = true;
			}else if (self.IsVisible(RGGame.LocalPlayer.Player) && RGGame.LocalPlayer.Actor != null)
			{
				if ((RGGame.LocalPlayer.Actor.TraitOrDefault<RGRevealsShroud>() == null) || (RGGame.LocalPlayer.Actor.TraitOrDefault<RGRevealsShroud>().RevealRange <= (RGGame.LocalPlayer.Actor.Location - self.Location).Length))
				{
					shouldRender = true;
				}
			}

			if (shouldRender)
			{
				Game.Renderer.BoldFont.DrawText(toRender, loc - Game.viewport.Location, color);

				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("flags", self.Owner.Country.Race),
					loc - new float2(30, -2) - Game.viewport.Location); // - new float2(size.X/4, -2)
			}

		}
	}
}
