using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Mods.Rg.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	internal class RgNukeBeaconTargetInfo : ITraitInfo
	{
		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgNukeBeaconTarget(init.self);
		}

		#endregion
	}

	internal class RgNukeBeaconTarget : ITick, IRgPostRender
	{
		public Player BeaconOwner;
		public ISound CountDown;
		public float DisarmProgress;
		public Actor LaunchSite;
		public Queue<Player> PendingOrders = new Queue<Player>();
		public Actor Self;
		public int TicksBeforeLaunch;
		public int TotalTicksBeforeLaunch;

		public RgNukeBeaconTarget(Actor self)
		{
			Self = self;
		}

		#region IRgPostRender Members

		public void RgRenderAfterWorld(WorldRenderer wr, Actor self)
		{
			RectangleF bounds = self.GetBounds(true);

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

			/*DrawSelectionBox(self, xy, Xy, xY, XY, Color.White);*/
			DrawBeaconBar(self, xY, XY);
			if (DisarmProgress > 0)
			{
				DrawDisarmBar(self, xY, XY);
			}
		}

		#endregion

		#region ITick Members

		public void Tick(Actor self)
		{
			if (TicksBeforeLaunch == 0)
			{
				CountDown = null;
				return;
			}

			if (DisarmProgress >= 1f)
			{
				/* Cancel launch! */

				Sound.StopSound(CountDown);
				TicksBeforeLaunch = 0;
				DisarmProgress = 0f;
				return;
			}

			if (LaunchSite == null || LaunchSite.Destroyed || !LaunchSite.IsInWorld)
			{
				if (CountDown != null)
					Sound.StopSound(CountDown);

				CountDown = null;

				return; /* launch site is gone ! */ // @todo Allow it to be gone!
			}

			TicksBeforeLaunch--;

			if (TicksBeforeLaunch == 25 * 10 && CountDown == null) /* after around 9 seconds)*/
			{
				Game.AddChatLine(Color.OrangeRed, "EVA", "Nuclear strike approaching!");
				CountDown = Sound.Play("nuke_warmup.aud", Self.CenterLocation);
			}

			if (TicksBeforeLaunch == 0)
			{
				self.World.AddFrameEndTask(w =>
				{
					w.Add(new RgNukeLaunch(BeaconOwner.PlayerActor, LaunchSite, "RgNuke", int2.Zero,
										   self.Location + new int2(1, 1)));

					Sound.StopSound(CountDown);
				});
			}
		}

		#endregion

		public void DoDisarm(Player disarmer, float amount)
		{
			if (DisarmProgress < 1f)
			{
				DisarmProgress += amount;
				if (DisarmProgress >= 1f)
				{
					/* player finished the disarming! */
					/* @todo give credits */

					Game.AddChatLine(Color.OrangeRed, "EVA", "Nuke Beacon disarmed!");
				}
			}
		}

		public void TriggerLaunch(Player launcher, Actor launchSite, int ticks)
		{
			BeaconOwner = launcher;
			TicksBeforeLaunch = ticks;
			TotalTicksBeforeLaunch = ticks;
			LaunchSite = launchSite;

			Game.AddChatLine(Color.OrangeRed, "EVA",
							 "{0} has placed a Nuke Beacon on the {1}.".F(launcher.PlayerName,
																	   Self.Info.Traits.GetOrDefault<TooltipInfo>().Name));
		}

		private void DrawDisarmBar(Actor self, float2 xY, float2 XY)
		{
			if (!self.IsInWorld) return;
			if (TicksBeforeLaunch == 0) return;

			xY += new float2(0, -4);
			XY += new float2(0, -4);

			Color c = Color.Gray;
			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), xY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY + new float2(0, -2), XY + new float2(0, -4), c, c);

			Color barColor = Color.DodgerBlue;

			Color barColor2 = Color.FromArgb(
				255,
				barColor.R / 2,
				barColor.G / 2,
				barColor.B / 2);
			/* hp * 1f / MaxHP*/
			float2 z = float2.Lerp(xY, XY, DisarmProgress);

			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -4), XY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -2), XY + new float2(0, -2), c, c);

			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -3), z + new float2(0, -3), barColor, barColor);
			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), z + new float2(0, -2), barColor2, barColor2);
			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -4), z + new float2(0, -4), barColor2, barColor2);
		}

		private void DrawBeaconBar(Actor self, float2 xY, float2 XY)
		{
			if (!self.IsInWorld) return;
			if (TicksBeforeLaunch == 0) return;

			int timeToTick = TotalTicksBeforeLaunch;
			int timeLeft = TicksBeforeLaunch;

			Color c = Color.Gray;
			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), xY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY + new float2(0, -2), XY + new float2(0, -4), c, c);

			Color barColor = Color.Orange;

			Color barColor2 = Color.FromArgb(
				255,
				barColor.R / 2,
				barColor.G / 2,
				barColor.B / 2);
			/* hp * 1f / MaxHP*/
			float2 z = float2.Lerp(xY, XY, timeLeft * 1f / timeToTick);

			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -4), XY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -2), XY + new float2(0, -2), c, c);

			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -3), z + new float2(0, -3), barColor, barColor);
			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), z + new float2(0, -2), barColor2, barColor2);
			Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -4), z + new float2(0, -4), barColor2, barColor2);
		}
	}
}