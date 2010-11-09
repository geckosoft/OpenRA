using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRg.Traits
{
	public class RGSniperVisorInfo : TraitInfo<RGSniperVisor>
	{

	}

	public class RGSniperVisor : IRGPostRender, IRGVisor, IRGNotifyProne, ITick
	{
		public bool Enabled { get; set; }

		public void RGRenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (RGGame.LocalPlayer == null || self != RGGame.LocalPlayer.Actor)
				return;

			DrawVisor(self);
		}

		public void Flip()
		{
			Enabled = !Enabled;
		}

		private void DrawVisor(Actor self)
		{
			if (Enabled)
			WidgetUtils.DrawRGBA(
				ChromeProvider.GetImage("visors", "sniper"),
				Viewport.LastMousePos - new int2(1024, 1024)); // - new float2(size.X/4, -2)
		}

		public void OnProne(Actor self, bool prone)
		{
			Enabled = prone;
		}

		public void Tick(Actor self)
		{
			if (self.GetCurrentActivity() is Attack && !Enabled)
			{
				self.CancelActivity();
			}
		}
	}
}
