using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.Rg.Traits;
using OpenRA.Mods.Rg.Traits.Abilities;
using OpenRA.Widgets;

namespace OpenRA.Mods.Rg.Widgets
{
	class RgAbilitiesWidget : Widget
	{
		static Dictionary<string, Sprite> spsprites = new Dictionary<string, Sprite>();
		Animation ready;
		Animation clock;
		readonly List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle, Action<MouseInput>>>();

		readonly World world;
		[ObjectCreator.UseCtor]
		public RgAbilitiesWidget([ObjectCreator.Param] World world)
		{
			this.world = world;
		}

		public override void Initialize()
		{
			base.Initialize();

			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
		}

		public override Rectangle EventBounds
		{
			get { return buttons.Any() ? buttons.Select(b => b.First).Aggregate(Rectangle.Union) : Bounds; }
		}

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
			{
				var action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();
				if (action == null)
					return false;

				action(mi);
				return true;
			}

			return false;
		}

		public override void DrawInner(WorldRenderer wr)
		{
			buttons.Clear();

			if (world.LocalPlayer == null) return;
			var rgplayer = world.LocalPlayer.PlayerActor.TraitOrDefault<RgPlayer>();
			if (rgplayer == null)
				return;

			Actor unit = null;

			if (rgplayer.Container != null  && rgplayer.Container.IsInWorld && !rgplayer.Container.Destroyed)
			{
				if (rgplayer.Container.TraitOrDefault<RgSteerable>() == null || rgplayer.Container.TraitOrDefault<RgSteerable>().Passengers.FirstOrDefault() != rgplayer.Avatar)
					return; // not driving it :/ 
				unit = rgplayer.Container;
			}else
			{
				unit = rgplayer.Avatar;
			}

			if (unit == null)
				return;
			if (!unit.IsInWorld || unit.Destroyed)
				return;

			// @todo Make this more efficient
			spsprites = Rules.Info.Values.Where(a => a.Name == unit.Info.Name).SelectMany(u => u.Traits.WithInterface<RgAbilityInfo>())
				.ToDictionary(
					u => u.Image,
					u => SpriteSheetBuilder.LoadAllSprites(u.Image)[0]);

			var powers = unit.TraitsImplementing<RgAbility>();
			var numPowers = powers.Count(p => p.IsAvailable);
			if (numPowers == 0) return;
			var rectBounds = RenderBounds;
			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-top"), new float2(rectBounds.X, rectBounds.Y));
			for (var i = 1; i < numPowers; i++)
				WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-middle"), new float2(rectBounds.X, rectBounds.Y + i * 51));
			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-bottom"), new float2(rectBounds.X, rectBounds.Y + numPowers * 51));

			// Hack Hack Hack
			rectBounds.Width = 69;
			rectBounds.Height = 10 + numPowers * 51 + 21;

			var y = rectBounds.Y + 10;
			foreach (var sp in powers)
			{
				var image = spsprites[sp.Info.Image];
				if (sp.IsAvailable)
				{
					var drawPos = new float2(rectBounds.X + 5, y);
					var rect = new Rectangle(rectBounds.X + 5, y, 64, 48);

					if (rect.Contains(Viewport.LastMousePos.ToPoint()))
					{
						var pos = drawPos.ToInt2();
						var tl = new int2(pos.X - 3, pos.Y - 3);
						var m = new int2(pos.X + 64 + 3, pos.Y + 48 + 3);
						var br = tl + new int2(64 + 3 + 20, 60);
						string description = sp.Info.LongDesc;
						if (description == null)
							description = "";
						if (!string.IsNullOrEmpty(sp.Details))
						{
							description += "\n\n" + sp.Details;
						}

						if (!string.IsNullOrEmpty(description))
							br += Game.Renderer.RegularFont.Measure(description.Replace("\\n", "\n"));
						else
							br += new int2(300, 0);

						var border = WidgetUtils.GetBorderSizes("dialog4");

						WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(tl.X, tl.Y, m.X + border[3], m.Y),
							PanelSides.Left | PanelSides.Top | PanelSides.Bottom);
						WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(m.X - border[2], tl.Y, br.X, m.Y + border[1]),
							PanelSides.Top | PanelSides.Right);
						WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(m.X, m.Y - border[1], br.X, br.Y),
							PanelSides.Left | PanelSides.Right | PanelSides.Bottom);

						pos += new int2(77, 5);
						Game.Renderer.BoldFont.DrawText(sp.Info.Description, pos, Color.White);

						pos += new int2(0, 20);
						if (WorldUtils.FormatTime(sp.TotalTime) != WorldUtils.FormatTime(0))
						{
							Game.Renderer.BoldFont.DrawText(WorldUtils.FormatTime(sp.RemainingTime).ToString(), pos, Color.White);
							Game.Renderer.BoldFont.DrawText("/ {0}".F(WorldUtils.FormatTime(sp.TotalTime)), pos + new int2(45, 0),
							                                Color.White);
						}

						if (!string.IsNullOrEmpty(description))
						{
							pos += new int2(0, 20);
							Game.Renderer.RegularFont.DrawText(description.Replace("\\n", "\n"), pos, Color.White);
						}
					}

					WidgetUtils.DrawSHP(image, drawPos, wr);

					clock.PlayFetchIndex("idle",
						() => (sp.TotalTime - sp.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / sp.TotalTime);
					clock.Tick();

					WidgetUtils.DrawSHP(clock.Image, drawPos, wr);

					if (sp.IsReady)
					{
						ready.Play("ready");
						WidgetUtils.DrawSHP(ready.Image, drawPos + new float2((64 - ready.Image.size.X) / 2, 2), wr);
					}
					else if (sp.RemainingTime == 0)/* if (Math.Round( (float)sp.TotalTime, 2) == 0)*/
					{
						clock.PlayFetchIndex("idle",
						   () => 0);
						clock.Tick();

						WidgetUtils.DrawSHP(clock.Image, drawPos, wr);
					}

					buttons.Add(Pair.New(rect, HandleSupportPower(sp)));

					y += 51;
				}
			}
		}

		Action<MouseInput> HandleSupportPower(RgAbility sp)
		{
			return mi => { if (mi.Button == MouseButton.Left) sp.Activate(); };
		}
	}
}
