using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Rg.Widgets
{
    class RgMoneyBinWidget : Widget
    {

        readonly World world;
        [ObjectCreator.UseCtor]
        public RgMoneyBinWidget([ObjectCreator.Param] World world)
        {
            this.world = world;
        }

        public override void DrawInner(WorldRenderer wr)
        {
            if (world.LocalPlayer == null) return;

            var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();

            /* find our cpu resources */
            // self.World.Queries.Ow
            /* not needed anymore! :)
            foreach (var kv in world.players)
            {
                var player = kv.Value;

                if (player.Stances[world.LocalPlayer] == Stance.Ally && player.PlayerRef.OwnsWorld && !player.PlayerRef.NonCombatant)
                {
                    playerResources = player.PlayerActor.Trait<PlayerResources>();
                }
            }
            */

            var digitCollection = "digits-" + world.LocalPlayer.Country.Race;
            var chromeCollection = "chrome-" + world.LocalPlayer.Country.Race;

            Game.Renderer.RgbaSpriteRenderer.DrawSprite(
                ChromeProvider.GetImage(chromeCollection, "moneybin"),
                new float2(Bounds.Left, 0));

            // Cash
            var cashDigits = (playerResources.DisplayCash + playerResources.DisplayOre).ToString();

            var x = Bounds.Right - 65;

            foreach (var d in cashDigits.Reverse())
            {
                Game.Renderer.RgbaSpriteRenderer.DrawSprite(
                    ChromeProvider.GetImage(digitCollection, (d - '0').ToString()),
                    new float2(x, 6));
                x -= 14;
            }

            /*
            if (SplitOreAndCash)
            {
                x -= 14;
                // Ore
                var oreDigits = playerResources.DisplayOre.ToString();

                foreach (var d in oreDigits.Reverse())
                {
                    Game.Renderer.RgbaSpriteRenderer.DrawSprite(
                        ChromeProvider.GetImage(digitCollection, (d - '0').ToString()),
                        new float2(x, 6));
                    x -= 14;
                }
            }
             */
        }
    }
}
