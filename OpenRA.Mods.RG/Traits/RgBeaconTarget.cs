using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Mods.Rg.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{

    class RgBeaconTargetInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new RgBeaconTarget(init.self); }
    }

    class RgBeaconTarget : ITick, IRgPostRender
    {
        public Queue<Player> PendingOrders = new Queue<Player>();
        public Actor Self = null;
        public Player BeaconOwner = null;
        public Actor LaunchSite = null;
        public int TicksBeforeLaunch = 0;
        public int TotalTicksBeforeLaunch = 0;
        public float DisarmProgress = 0f;
        public ISound CountDown = null;

        public RgBeaconTarget(Actor self)
        {
            Self = self;
        }

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

                return; /* launch site is gone ! */
            }

            TicksBeforeLaunch--;

            if (TicksBeforeLaunch == 25 * 10 && CountDown == null) /* after around 9 seconds)*/
            {
                if (LaunchSite.Info.Name == "tmpl") /* nuke silo */
                {
                    LaunchSite.Trait<RenderBuilding>().PlayCustomAnim(self, "active");
                    Game.AddChatLine(Color.OrangeRed, "EVA", "Nuclear strike approaching!");
                    CountDown = Sound.Play("nuke_warmup.aud", Self.CenterLocation);
                }
                else if (LaunchSite.Info.Name == "eye") /* ion cannon */
                {
                    Game.AddChatLine(Color.OrangeRed, "EVA", "Ion Cannon approaching!");
                    CountDown = Sound.Play("ion_warmup.aud", Self.CenterLocation);
                }
            }

            if ( TicksBeforeLaunch == 0)
            {
                /* Launch! */
                if (LaunchSite.Info.Name == "tmpl") /* nuke silo */
                {
                    LaunchSite.Trait<RenderBuilding>().PlayCustomAnim(self, "active");

                    self.World.AddFrameEndTask(w =>
                    {
                        /*Game.AddChatLine(Color.OrangeRed, "EVA", "Nuclear launch detected!"); */
                        //FIRE ZE MISSILES
                        /*Sound.Play("nukemisl.aud", Game.CellSize * LaunchSite.CenterLocation);*/
                        w.Add(new RgNukeLaunch(BeaconOwner.PlayerActor, LaunchSite, "RgNuke", int2.Zero, self.Location + new int2(1, 1)));

                        Sound.StopSound(CountDown);
                    });
                }else if (LaunchSite.Info.Name == "eye") /* ion cannon */
                {
                    self.World.AddFrameEndTask(w =>
                    {
                        /*Game.AddChatLine(Color.OrangeRed, "EVA", "Ion Cannon launch detected!"); */

                        Sound.Play("ion1.aud", Self.CenterLocation);
                        w.Add(new RgIonCannon(BeaconOwner.PlayerActor, w, self.Location + new int2(1, 1)));
                        Sound.StopSound(CountDown);
                    });
                }
            }
        }

        public void DoDisarm(Player disarmer, float amount)
        {
            if (DisarmProgress < 1f)
            {
                DisarmProgress += amount;
                if (DisarmProgress >= 1f)
                {
                    /* player finished the disarming! */
                    /* @todo give credits */

                    Game.AddChatLine(Color.OrangeRed, "EVA", "Beacon disarmed!");
                }
            }
        }

        public void TriggerLaunch (Player launcher, Actor launchSite, int ticks)
        {
            BeaconOwner = launcher;
            TicksBeforeLaunch = ticks;
            TotalTicksBeforeLaunch = ticks;
            LaunchSite = launchSite;
            var beaconType = "a Unknown Super Power";

            if (LaunchSite.Info.Name == "eye")
            {
                beaconType = "an Ion Cannon";
            }else if (LaunchSite.Info.Name == "tmpl")
            {
                beaconType = "a Nuke";
            }

            Game.AddChatLine(Color.OrangeRed, "EVA", "{0} has placed {1} beacon on the {2}.".F(launcher.PlayerName, beaconType, Self.Info.Traits.GetOrDefault<TooltipInfo>().Name)); 
        }

        public void RgRenderAfterWorld(WorldRenderer wr, Actor self)
        {
            var bounds = self.GetBounds(true);

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

        void DrawDisarmBar(Actor self, float2 xY, float2 XY)
        {
            if (!self.IsInWorld) return;
            if (TicksBeforeLaunch == 0) return;

            var timeToTick = this.TotalTicksBeforeLaunch;
            var timeLeft = this.TicksBeforeLaunch;

            xY += new float2(0, -4);
            XY += new float2(0, -4);

            var c = Color.Gray;
            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), xY + new float2(0, -4), c, c);
            Game.Renderer.LineRenderer.DrawLine(XY + new float2(0, -2), XY + new float2(0, -4), c, c);

            var barColor = Color.DodgerBlue;

            var barColor2 = Color.FromArgb(
                255,
                barColor.R / 2,
                barColor.G / 2,
                barColor.B / 2);
            /* hp * 1f / MaxHP*/
            var z = float2.Lerp(xY, XY, DisarmProgress);

            Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -4), XY + new float2(0, -4), c, c);
            Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -2), XY + new float2(0, -2), c, c);

            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -3), z + new float2(0, -3), barColor, barColor);
            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), z + new float2(0, -2), barColor2, barColor2);
            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -4), z + new float2(0, -4), barColor2, barColor2);
        }

        void DrawBeaconBar(Actor self, float2 xY, float2 XY)
        {
            if (!self.IsInWorld) return;
            if (TicksBeforeLaunch == 0) return;

            var timeToTick = this.TotalTicksBeforeLaunch;
            var timeLeft = this.TicksBeforeLaunch;

            var c = Color.Gray;
            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), xY + new float2(0, -4), c, c);
            Game.Renderer.LineRenderer.DrawLine(XY + new float2(0, -2), XY + new float2(0, -4), c, c);

            var barColor = Color.Orange;

            var barColor2 = Color.FromArgb(
                255,
                barColor.R / 2,
                barColor.G / 2,
                barColor.B / 2);
            /* hp * 1f / MaxHP*/
            var z = float2.Lerp(xY, XY, timeLeft * 1f / timeToTick);

            Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -4), XY + new float2(0, -4), c, c);
            Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -2), XY + new float2(0, -2), c, c);

            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -3), z + new float2(0, -3), barColor, barColor);
            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -2), z + new float2(0, -2), barColor2, barColor2);
            Game.Renderer.LineRenderer.DrawLine(xY + new float2(0, -4), z + new float2(0, -4), barColor2, barColor2);
        }

        void DrawSelectionBox(Actor self, float2 xy, float2 Xy, float2 xY, float2 XY, Color c)
        {
            Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(4, 0), c, c);
            Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(0, 4), c, c);
            Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
            Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(0, 4), c, c);

            Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(4, 0), c, c);
            Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(0, -4), c, c);
            Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(-4, 0), c, c);
            Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(0, -4), c, c);
        }
    }
}
