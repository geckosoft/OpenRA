#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ChronoshiftPowerInfo : SupportPowerInfo
	{
		public readonly float Range = 2;

		public override object Create(ActorInitializer init) { return new ChronoshiftPower(init.self,this); }
	}

	class ChronoshiftPower : SupportPower, IResolveOrder
	{	
		public ChronoshiftPower(Actor self, ChronoshiftPowerInfo info) : base(self, info) { }
		protected override void OnActivate() { Self.World.OrderGenerator = new SelectTarget((ChronoshiftPowerInfo) Info); }

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsReady) return;

			if (order.OrderString == "ChronosphereSelect" && self.Owner == self.World.LocalPlayer)
			{
				//self.World.OrderGenerator = new SelectDestination(order.TargetActor);
			}

			if (order is ChronoshiftOrder && order.OrderString == "ChronosphereActivate")
			{
				var chronosphere = self.World.Queries
					.OwnedBy[self.Owner]
					.WithTrait<Chronosphere>()
					.Select(x => x.Actor).FirstOrDefault();

				if (chronosphere == null) return;

				var corder = order as ChronoshiftOrder;

				var units = FindUnitsInCircle(self.World, corder.SourceLocation, ((ChronoshiftPowerInfo) Info).Range);

				foreach (var unit in units)
				{
					var tl = order.TargetLocation;

					var diff = corder.SourceLocation - unit.Location ;

					var movement = unit.TraitOrDefault<IMove>();
					if (movement == null || !movement.CanEnterCell(tl - diff))
						continue;

					chronosphere.Trait<Chronosphere>().Teleport(unit, tl - diff);
				}

				FinishActivate();
			}
		}

		public static IEnumerable<Actor> FindUnitsInCircle(World world, int2 xy, float range)
		{
			return world.FindUnitsInCircle(xy * Game.CellSize, range * Game.CellSize)
					.Where(a => a.HasTrait<Chronoshiftable>()
								&& a.HasTrait<Selectable>());
		}

		class SelectTarget : IOrderGenerator
		{
			ChronoshiftPowerInfo info;

			public SelectTarget(ChronoshiftPowerInfo info) { this.info = info; }

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					world.CancelInputMode();

				var ret = OrderInner( world, xy, mi ).ToList();
				foreach( var order in ret )
				{
					world.OrderGenerator = new SelectDestination(xy, info.Range);
					break;
				}
				return ret;
			}


			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					if (FindUnitsInCircle(world, xy, info.Range).Any())
						yield return new Order("ChronosphereSelect", world.LocalPlayer.PlayerActor, "", false);
				}

				yield break;
			}

			public void Tick( World world )
			{
				var hasChronosphere = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<Chronosphere>()
					.Any();

				if (!hasChronosphere)
				{
					world.CancelInputMode();
				}
			}

			public void RenderAfterWorld( WorldRenderer wr, World world )
			{
				if (_lastMouseInput == null) return;

				var targetUnits = FindUnitsInCircle(world, Game.viewport.ViewToWorld(_lastMouseInput.Value).ToInt2(), info.Range);

				if (info.Range >= 1f)
					wr.DrawRangeCircle(Color.Green, Game.viewport.Location + _lastMouseInput.Value.Location, info.Range - 0.5f);
				
			}
			public void RenderBeforeWorld( WorldRenderer wr, World world ) { }

			MouseInput? _lastMouseInput = null;

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				_lastMouseInput = mi;
				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? "chrono-select" : "move-blocked";
			}
			public IEnumerable<Renderable> Render(World world)
			{
				if (_lastMouseInput == null) yield break;

				var targetUnits = FindUnitsInCircle(world, Game.viewport.ViewToWorld(_lastMouseInput.Value).ToInt2(), info.Range);

				foreach (var r in targetUnits.SelectMany(a => a.Render()))
				{
					yield return r.WithPalette("highlight");
				}

				yield break;
			}
		}

		class SelectDestination : IOrderGenerator
		{
			readonly int2 _sourceLocation;
			readonly float _range;

			public SelectDestination(int2 source, float range)
			{
				_sourceLocation = source;
				_range = range;
			}
			
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					world.CancelInputMode();

				var ret = OrderInner( world, xy, mi ).ToList();
				if (ret.Count > 0)
					world.CancelInputMode();
				return ret;
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					// Cannot chronoshift into unexplored location
					if (world.LocalPlayer.Shroud.IsExplored(xy))
					{
						yield return new ChronoshiftOrder("ChronosphereActivate", world.LocalPlayer.PlayerActor, _sourceLocation, xy);
					}
				}
			}

			public void Tick(World world)
			{
				var hasChronosphere = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<Chronosphere>()
					.Any();

				if (!hasChronosphere)
					world.CancelInputMode();

				// TODO: Check if the selected unit is still alive
			}
			
			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				var units = FindUnitsInCircle(world, _sourceLocation, _range);

				foreach (var unit in units)
				{
					wr.DrawSelectionBox(unit, Color.Yellow);
				}
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				if (!world.LocalPlayer.Shroud.IsExplored(xy))
					return "move-blocked";

				var units = FindUnitsInCircle(world, _sourceLocation, _range);

				foreach (var unit in units)
				{
					var tl = xy;

					var diff = _sourceLocation - unit.Location;

					var movement = unit.TraitOrDefault<IMove>();
					if (movement == null || !movement.CanEnterCell(tl - diff))
						continue;

					return "chrono-target";
				}

				return "move-blocked";
			}
			public IEnumerable<Renderable> Render(World world) { yield break; }
		}
	}
	
	// tag trait for the building
	class ChronosphereInfo : ITraitInfo
	{
		public readonly int Duration = 30;
		public readonly bool KillCargo = true;
		public object Create(ActorInitializer init) { return new Chronosphere(init.self); }
	}
	
	class Chronosphere
	{
		Actor self;
		public Chronosphere(Actor self)
		{
			this.self = self;
		}
		
		public void Teleport(Actor targetActor, int2 targetLocation)
		{
			var info = self.Info.Traits.Get<ChronosphereInfo>();
			bool success = targetActor.Trait<Chronoshiftable>().Activate(targetActor, targetLocation, info.Duration * 25, info.KillCargo, self);
			
			if (success)
			{
				Sound.Play("chrono2.aud", self.CenterLocation);
				Sound.Play("chrono2.aud", targetActor.CenterLocation);
				
				// Trigger screen desaturate effect
				foreach (var a in self.World.Queries.WithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();

				self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");
			}
		}
	}

	public class ChronoshiftOrder : CustomOrder
	{
		public int2 SourceLocation { get; protected set; }

		public ChronoshiftOrder() // required
		{

		}

		public ChronoshiftOrder(string orderString, Actor subject, int2 src, int2 target)
			: base(orderString, subject)
		{
			SourceLocation = src;
			TargetLocation = target;
		}

		public override void OnSerialize(BinaryWriter w)
		{
			Write(w, SourceLocation);
			Write(w, TargetLocation);
		}

		public override bool OnDeserialize(World world, BinaryReader r)
		{
			SourceLocation = ReadInt2(r);
			TargetLocation = ReadInt2(r);

			return true;
		}
	}
}
