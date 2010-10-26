using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.Rg.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Rg.Widgets
{
	internal class RgBuildPaletteWidget : Widget
	{
		private const int paletteAnimationLength = 7;
		private static readonly float2 paletteOpenOrigin = new float2(Game.viewport.Width - 215, 280);
		private static readonly float2 paletteClosedOrigin = new float2(Game.viewport.Width - 16, 280);
		private static float2 paletteOrigin = paletteClosedOrigin;
		public readonly string BuildPaletteClose = "bleep13.aud";
		public readonly string BuildPaletteOpen = "bleep13.aud";
		public readonly string TabClick = "ramenu1.aud";
		private readonly List<RgProductionQueue> VisibleQueues = new List<RgProductionQueue>();

		private readonly List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle, Action<MouseInput>>>();
		private readonly List<Pair<Rectangle, Action<MouseInput>>> tabs = new List<Pair<Rectangle, Action<MouseInput>>>();
		private readonly World world;
		public int Columns = 3;
		private RgProductionQueue CurrentQueue;
		public int Rows = 5;
		private Animation cantBuild;
		private Animation clock;
		private Dictionary<string, Sprite> iconSprites;
		private int numActualRows;
		public bool paletteAnimating;
		private int paletteAnimationFrame;
		private int paletteHeight;
		public bool paletteOpen;
		private Animation ready;

		[ObjectCreator.UseCtorAttribute]
		public RgBuildPaletteWidget([ObjectCreator.ParamAttribute] World world)
		{
			this.world = world;
		}

		public override Rectangle EventBounds
		{
			get { return new Rectangle((int) (paletteOrigin.X) - 24, (int) (paletteOrigin.Y), 215, 48*numActualRows); }
		}

		public override void Initialize()
		{
			base.Initialize();

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");

			iconSprites = Rules.Info.Values
				.Where(u => u.Traits.Contains<BuildableInfo>() && u.Name[0] != '^')
				.ToDictionary(
					u => u.Name,
					u => SpriteSheetBuilder.LoadAllSprites(u.Traits.Get<TooltipInfo>().Icon ?? (u.Name + "icon"))[0]);

			IsVisible = () => { return CurrentQueue != null || (CurrentQueue == null && !paletteOpen); };
		}

		public override void Tick()
		{
			VisibleQueues.Clear();

			IEnumerable<RgProductionQueue> queues = world.Queries.WithTraitMultiple<RgProductionQueue>()
				.Where(
					p =>
					p.Actor.Owner.Stances[world.LocalPlayer] == Stance.Ally && p.Actor.Owner.PlayerRef.OwnsWorld &&
					!p.Actor.Owner.PlayerRef.NonCombatant)
				.Select(p => p.Trait);

			if (CurrentQueue != null && CurrentQueue.self.Destroyed)
				CurrentQueue = null;

			foreach (RgProductionQueue queue in queues)
			{
				if (queue.AllItems().Count() > 0)
					VisibleQueues.Add(queue);
				else if (CurrentQueue == queue)
					CurrentQueue = null;
			}
			if (CurrentQueue == null)
				CurrentQueue = VisibleQueues.FirstOrDefault();

			TickPaletteAnimation(world);

			base.Tick();
		}

		private void TickPaletteAnimation(World world)
		{
			if (!paletteAnimating)
				return;

			// Increment frame
			if (paletteOpen)
				paletteAnimationFrame++;
			else
				paletteAnimationFrame--;

			// Calculate palette position
			if (paletteAnimationFrame <= paletteAnimationLength)
				paletteOrigin = float2.Lerp(paletteClosedOrigin, paletteOpenOrigin,
				                            paletteAnimationFrame*1.0f/paletteAnimationLength);

			// Play palette-open sound at the start of the activate anim (open)
			if (paletteAnimationFrame == 1 && paletteOpen)
				Sound.Play(BuildPaletteOpen);

			// Play palette-close sound at the start of the activate anim (close)
			if (paletteAnimationFrame == paletteAnimationLength + -1 && !paletteOpen)
				Sound.Play(BuildPaletteClose);

			// Animation is complete
			if ((paletteAnimationFrame == 0 && !paletteOpen)
			    || (paletteAnimationFrame == paletteAnimationLength && paletteOpen))
			{
				paletteAnimating = false;
			}
		}

		public void SetCurrentTab(RgProductionQueue queue)
		{
			if (!paletteOpen)
				paletteAnimating = true;
			paletteOpen = true;
			CurrentQueue = queue;
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up) return false;

			if (e.KeyChar == '\t')
			{
				TabChange(e.Modifiers.HasModifier(Modifiers.Shift));
				return true;
			}

			return DoBuildingHotkey(Char.ToLowerInvariant(e.KeyChar), world);
		}

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event != MouseInputEvent.Down)
				return false;

			Action<MouseInput> action = tabs.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();
			if (action == null && paletteOpen)
				action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
					.Select(a => a.Second).FirstOrDefault();

			if (action == null)
				return false;

			action(mi);
			return true;
		}

		public override void DrawInner(WorldRenderer wr)
		{
			if (!IsVisible()) return;
			// todo: fix
			paletteHeight = DrawPalette(wr, world, CurrentQueue);
			DrawBuildTabs(world, paletteHeight);
		}

		private int DrawPalette(WorldRenderer wr, World world, RgProductionQueue queue)
		{
			buttons.Clear();
			if (queue == null) return 0;

			string paletteCollection = "palette-" + world.LocalPlayer.Country.Race;
			var origin = new float2(paletteOrigin.X + 9, paletteOrigin.Y + 9);

			// Collect info
			int x = 0;
			int y = 0;
			IOrderedEnumerable<ActorInfo> buildableItems =
				queue.BuildableItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);
			int totalMoney = world.LocalPlayer.PlayerActor.TraitOrDefault<PlayerResources>().Ore +
			                 world.LocalPlayer.PlayerActor.TraitOrDefault<PlayerResources>().Cash;


			var power = queue.self.Owner.PlayerActor.Trait<PowerManager>();
			bool lowpower = power.PowerState != PowerState.Normal;
			float costModifier = 1;

			if (lowpower)
				costModifier = 2; /* low power means double the cost */

			buildableItems =
				buildableItems.Where(
					a =>
					a.Traits.GetOrDefault<ValuedInfo>() != null &&
					a.Traits.GetOrDefault<ValuedInfo>().Cost*costModifier <= totalMoney).OrderBy(
						a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);

			/* now see if we arent too far! */

			/* find our current units */
			List<Actor> myUnits =
				world.Queries.OwnedBy[world.LocalPlayer].Where(
					a =>
					!a.Destroyed && a.IsInWorld && a.Info.Name != "c17" && a.Owner == world.LocalPlayer && a != a.Owner.PlayerActor).
					ToList();

			if (myUnits.Count == 0 || queue.self.Destroyed)
			{
				/* dont have a unit yet, cant build anything! */
				/* is actually buggy as in unit could be inside a tank, but its good that ppl cant buy stuff if they reside in one ^^ */
				buildableItems = queue.BuildableItems().Where(a => false).OrderBy(a => a);
			}
			else
			{
				/* check distance */
				float dis = (myUnits[0].CenterLocation - queue.self.CenterLocation).Length;

				if (dis > 125) /* too far away */
					buildableItems = queue.BuildableItems().Where(a => false).OrderBy(a => a);
			}

			IOrderedEnumerable<ActorInfo> allBuildables =
				queue.AllItems().OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder);

			var overlayBits = new List<Pair<Sprite, float2>>();
			numActualRows = Math.Max((allBuildables.Count() + Columns - 1)/Columns, Rows);

			// Palette Background
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "top"), new float2(origin.X - 9, origin.Y - 9));
			for (int w = 0; w < numActualRows; w++)
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage(paletteCollection, "bg-" + (w%4)),
					new float2(origin.X - 9, origin.Y + 48*w));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "bottom"),
			                     new float2(origin.X - 9, origin.Y - 1 + 48*numActualRows));


			// Icons
			string tooltipItem = null;
			bool isBuildingSomething = queue.CurrentItem() != null;
			foreach (ActorInfo item in allBuildables)
			{
				var rect = new RectangleF(origin.X + x*64, origin.Y + 48*y, 64, 48);
				var drawPos = new float2(rect.Location);
				WidgetUtils.DrawSHP(iconSprites[item.Name], drawPos, wr);

				RgProductionItem firstOfThis = queue.AllQueued().FirstOrDefault(a => a.Item == item.Name);

				if (rect.Contains(Viewport.LastMousePos.ToPoint()))
					tooltipItem = item.Name;

				float2 overlayPos = drawPos + new float2((64 - ready.Image.size.X)/2, 2);

				if (firstOfThis != null)
				{
					clock.PlayFetchIndex("idle",
					                     () => (firstOfThis.TotalTime - firstOfThis.RemainingTime)
					                           *(clock.CurrentSequence.Length - 1)/firstOfThis.TotalTime);
					clock.Tick();
					WidgetUtils.DrawSHP(clock.Image, drawPos, wr);

					if (firstOfThis.Done)
					{
						ready.Play("ready");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}
					else if (firstOfThis.Paused)
					{
						ready.Play("hold");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}

					int repeats = queue.AllQueued().Count(a => a.Item == item.Name);
					if (repeats > 1 || queue.CurrentItem() != firstOfThis)
					{
						int offset = -22;
						string digits = repeats.ToString();
						foreach (char d in digits)
						{
							ready.PlayFetchIndex("groups", () => d - '0');
							ready.Tick();
							overlayBits.Add(Pair.New(ready.Image, overlayPos + new float2(offset, 0)));
							offset += 6;
						}
					}
				}
				else if (!buildableItems.Any(a => a.Name == item.Name) || isBuildingSomething)
					overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				string closureName = buildableItems.Any(a => a.Name == item.Name) ? item.Name : null;
				buttons.Add(Pair.New(new Rectangle((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height),
				                     HandleClick(closureName, world)));

				if (++x == Columns)
				{
					x = 0;
					y++;
				}
			}
			if (x != 0) y++;

			foreach (var ob in overlayBits)
				WidgetUtils.DrawSHP(ob.First, ob.Second, wr);

			// Tooltip
			if (tooltipItem != null && !paletteAnimating && paletteOpen)
				DrawProductionTooltip(world, tooltipItem,
				                      new float2(Game.viewport.Width, origin.Y + numActualRows*48 + 9).ToInt2());

			// Palette Dock
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "dock-top"),
			                     new float2(Game.viewport.Width - 14, origin.Y - 23));

			for (int i = 0; i < numActualRows; i++)
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "dock-" + (i%4)),
				                     new float2(Game.viewport.Width - 14, origin.Y + 48*i));

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(paletteCollection, "dock-bottom"),
			                     new float2(Game.viewport.Width - 14, origin.Y - 1 + 48*numActualRows));

			return 48*y + 9;
		}

		private Action<MouseInput> HandleClick(string name, World world)
		{
			return mi =>
			       	{
			       		Sound.Play(TabClick);

			       		if (name != null)
			       			HandleBuildPalette(world, name, (mi.Button == MouseButton.Left));
			       	};
		}

		private Action<MouseInput> HandleTabClick(RgProductionQueue queue, World world)
		{
			return mi =>
			       	{
			       		if (mi.Button != MouseButton.Left)
			       			return;

			       		Sound.Play(TabClick);
			       		bool wasOpen = paletteOpen;
			       		paletteOpen = (CurrentQueue == queue && wasOpen) ? false : true;
			       		CurrentQueue = queue;
			       		if (wasOpen != paletteOpen)
			       			paletteAnimating = true;
			       	};
		}

		private static string Description(string a)
		{
			if (a[0] == '@')
				return "any " + a.Substring(1);
			else
				return Rules.Info[a.ToLowerInvariant()].Traits.Get<TooltipInfo>().Name;
		}

		private void HandleBuildPalette(World world, string item, bool isLmb)
		{
			ActorInfo unit = Rules.Info[item];
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			RgProductionItem producing = CurrentQueue.AllQueued().FirstOrDefault(a => a.Item == item);

			if (isLmb)
			{
				if (producing != null && producing == CurrentQueue.CurrentItem())
				{
					if (producing.Done)
					{
						if (unit.Traits.Contains<BuildingInfo>())
							world.OrderGenerator = new PlaceBuildingOrderGenerator(CurrentQueue.self, item);
						else
							StartProduction(world, item);
						return;
					}
					/*
                    if (producing.Paused)
                    {
                        world.IssueOrder(Order.PauseProduction(CurrentQueue.self, item, false));
                        return;
                    }*/
				}

				//RgAssignUnit

				StartProduction(world, item);
			}
			else
			{
				/* cant cancel ^^
                if (producing != null)
                {
                    // instant cancel of things we havent really started yet, and things that are finished
                    if (producing.Paused || producing.Done || producing.TotalCost == producing.RemainingCost)
                    {
                        Sound.Play(eva.CancelledAudio);
                        int numberToCancel = Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1;
                        if (Game.GetModifierKeys().HasModifier(Modifiers.Shift) &&
                            Game.GetModifierKeys().HasModifier(Modifiers.Ctrl))
                        {
                            numberToCancel = -1; //cancel all
                        }
                        world.IssueOrder(Order.CancelProduction(CurrentQueue.self, item, numberToCancel));
                    }
                    else
                    {
                        Sound.Play(eva.OnHoldAudio);
                        world.IssueOrder(Order.PauseProduction(CurrentQueue.self, item, true));
                    }
                }*/
			}
		}

		private void StartProduction(World world, string item)
		{
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			ActorInfo unit = Rules.Info[item];

			Sound.Play(unit.Traits.Contains<BuildingInfo>() ? eva.BuildingSelectAudio : eva.UnitSelectAudio);

			//world.LocalPlayer
			world.IssueOrder(OrderStartProduction(world, CurrentQueue.self, item, 1));
			/* MAX ONE
                Game.GetModifierKeys().HasModifier(Modifiers.Shift) ? 5 : 1));*/
		}

		public static Order OrderStartProduction(World world, Actor subject, string item, int count)
		{
			return new Order("RgStartProduction", subject, world.LocalPlayer.PlayerActor, new int2(count, 0), item, false);
		}

		private void DrawBuildTabs(World world, int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			float x = paletteOrigin.X - tabWidth;
			float y = paletteOrigin.Y + 9;

			tabs.Clear();

			foreach (RgProductionQueue queue in VisibleQueues)
			{
				string[] tabKeys = {"normal", "ready", "selected"};
				RgProductionItem producing = queue.CurrentItem();
				int index = queue == CurrentQueue ? 2 : (producing != null && producing.Done) ? 1 : 0;

				string race = world.LocalPlayer.Country.Race;
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("tabs-" + tabKeys[index], race + "-" + queue.Info.Type),
				                     new float2(x, y));

				var rect = new Rectangle((int) x, (int) y, tabWidth, tabHeight);
				tabs.Add(Pair.New(rect, HandleTabClick(queue, world)));

				if (rect.Contains(Viewport.LastMousePos.ToPoint()))
				{
					string text = queue.Info.Type;
					int2 sz = Game.Renderer.BoldFont.Measure(text);
					WidgetUtils.DrawPanelPartial("dialog4",
					                             Rectangle.FromLTRB(rect.Left - sz.X - 30, rect.Top, rect.Left - 5, rect.Bottom),
					                             PanelSides.All);

					Game.Renderer.BoldFont.DrawText(text,
					                                new float2(rect.Left - sz.X - 20, rect.Top + 12), Color.White);
				}

				y += tabHeight;
			}
		}

		private void DrawRightAligned(string text, int2 pos, Color c)
		{
			Game.Renderer.BoldFont.DrawText(text,
			                                pos - new int2(Game.Renderer.BoldFont.Measure(text).X, 0), c);
		}

		private void DrawProductionTooltip(World world, string unit, int2 pos)
		{
			pos.Y += 15;

			Player pl = world.LocalPlayer;
			float2 p = pos.ToFloat2() - new float2(297, -3);

			ActorInfo info = Rules.Info[unit];
			var tooltip = info.Traits.Get<TooltipInfo>();
			var buildable = info.Traits.Get<BuildableInfo>();
			int cost = info.Traits.Get<ValuedInfo>().Cost;
			bool canBuildThis = CurrentQueue.CanBuild(info);

			int longDescSize = Game.Renderer.RegularFont.Measure(tooltip.Description.Replace("\\n", "\n")).Y;
			if (!canBuildThis) longDescSize += 8;

			WidgetUtils.DrawPanel("dialog4", new Rectangle(Game.viewport.Width - 300, pos.Y, 300, longDescSize + 65));

			Game.Renderer.BoldFont.DrawText(
				tooltip.Name + ((buildable.Hotkey != null) ? " ({0})".F(buildable.Hotkey.ToUpper()) : ""),
				p.ToInt2() + new int2(5, 5), Color.White);

			var resources = pl.PlayerActor.Trait<PlayerResources>();

			//var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();

			/* find our cpu resources */
			// self.World.Queries.Ow
			Player plrr = null;
			foreach (var kv in world.players)
			{
				Player player = kv.Value;

				if (player.Stances[pl] == Stance.Ally && player.PlayerRef.OwnsWorld && !player.PlayerRef.NonCombatant)
				{
					resources = player.PlayerActor.Trait<PlayerResources>();
					plrr = player;
				}
			}
			bool lowpower = false;
			var power = pl.PlayerActor.Trait<PowerManager>();
			if (plrr != null)
			{
				power = plrr.PlayerActor.Trait<PowerManager>();

				lowpower = power.PowerState != PowerState.Normal;
				if (lowpower)
					cost *= 2; /* low power means double the cost */
			}
			DrawRightAligned("${0}".F(cost), pos + new int2(-5, 5),
			                 (resources.DisplayCash >= cost ? Color.White : Color.Red));

			int time = CurrentQueue.GetBuildTime(info.Name)
			           *((lowpower) ? CurrentQueue.Info.LowPowerSlowdown : 1);
			DrawRightAligned(WorldUtils.FormatTime(time), pos + new int2(-5, 35), lowpower ? Color.Red : Color.White);

			var bi = info.Traits.GetOrDefault<BuildingInfo>();
			if (bi != null)
				DrawRightAligned("{1}{0}".F(bi.Power, bi.Power > 0 ? "+" : ""), pos + new int2(-5, 20),
				                 ((power.PowerProvided - power.PowerDrained) >= -bi.Power || bi.Power > 0) ? Color.White : Color.Red);

			p += new int2(5, 35);
			if (!canBuildThis)
			{
				IEnumerable<string> prereqs = buildable.Prerequisites
					.Select(a => Description(a));
				Game.Renderer.RegularFont.DrawText(
					"Requires {0}".F(string.Join(", ", prereqs.ToArray())),
					p.ToInt2(),
					Color.White);

				p += new int2(0, 8);
			}

			p += new int2(0, 15);
			Game.Renderer.RegularFont.DrawText(tooltip.Description.Replace("\\n", "\n"),
			                                   p.ToInt2(), Color.White);
		}

		private bool DoBuildingHotkey(char c, World world)
		{
			if (!paletteOpen) return false;
			ActorInfo toBuild =
				CurrentQueue.BuildableItems().FirstOrDefault(b => b.Traits.Get<BuildableInfo>().Hotkey == c.ToString());

			if (toBuild != null)
			{
				Sound.Play(TabClick);
				HandleBuildPalette(world, toBuild.Name, true);
				return true;
			}

			return false;
		}

		private void TabChange(bool shift)
		{
			int size = VisibleQueues.Count();
			if (size > 0)
			{
				int current = VisibleQueues.IndexOf(CurrentQueue);
				if (!shift)
				{
					if (current + 1 >= size)
						SetCurrentTab(VisibleQueues.FirstOrDefault());
					else
						SetCurrentTab(VisibleQueues[current + 1]);
				}
				else
				{
					if (current - 1 < 0)
						SetCurrentTab(VisibleQueues.LastOrDefault());
					else
						SetCurrentTab(VisibleQueues[current - 1]);
				}
			}
		}
	}
}