﻿#region Copyright & License Information
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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public readonly float Range = 3f;

		public override object Create(ActorInitializer init) { return new IronCurtainPower(init.self, this); }
	}

	class IronCurtainPower : SupportPower, IResolveOrder
	{
		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, "ironchg1.aud"); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "ironrdy1.aud"); }
		protected override void OnActivate()
		{
			Self.World.OrderGenerator = new SelectTarget(Info as IronCurtainPowerInfo);
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsReady) return;

			if (order.OrderString == "IronCurtain")
			{
				var curtain = self.World.Queries.WithTrait<IronCurtain>()
					.Where(a => a.Actor.Owner != null)
					.FirstOrDefault().Actor;
				if (curtain != null)
					curtain.Trait<RenderBuilding>().PlayCustomAnim(curtain, "active");

				Sound.Play("ironcur9.aud", Game.CellSize * order.TargetLocation);

				var targets = SelectTarget.FindUnitsInCircle(self.World, order.TargetLocation, (Info as IronCurtainPowerInfo).Range);

				foreach (var target in targets)
				{
					if (target.HasTrait<IronCurtainable>())
						target.Trait<IronCurtainable>().Activate(target, (int)((Info as IronCurtainPowerInfo).Duration * 25 * 60));
				}

				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			IronCurtainPowerInfo _info;

			public SelectTarget(IronCurtainPowerInfo info) { _info = info; }

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					world.CancelInputMode();
				}

				world.Effects.ToArray().Where(e => e is HighlightTarget).Do(world.Remove); 

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var targetUnits = FindUnitsInCircle(world, xy, _info.Range);

					if( targetUnits.Any() )
						yield return new Order("IronCurtain", world.LocalPlayer.PlayerActor, xy, false);
				}
			}

			public static IEnumerable<Actor> FindUnitsInCircle(World world, int2 xy, float range)
			{
				return world.FindUnitsInCircle(xy * Game.CellSize, range * Game.CellSize)
						.Where(a => a.Owner != null
									&& a.HasTrait<IronCurtainable>()
									&& a.HasTrait<Selectable>());
			}

			public void Tick(World world)
			{
				var hasStructure = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<IronCurtain>()
					.Any();

				if (!hasStructure)
				{
					world.Effects.ToArray().Where(e => e is HighlightTarget).Do(world.Remove); 
					world.CancelInputMode();
				}
			}

			MouseInput? _lastMouseInput = null;

			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				if (_lastMouseInput == null) return;

				var targetUnits = FindUnitsInCircle(world, Game.viewport.ViewToWorld(_lastMouseInput.Value).ToInt2(), _info.Range);

				world.Effects.ToArray().Where(e => e is HighlightTarget).Do(world.Remove); 
				targetUnits.Do(a => world.Add(new HighlightTarget(a)));

				if (_info.Range >= 1f)
					wr.DrawRangeCircle(Color.Red, Game.viewport.Location + _lastMouseInput.Value.Location, _info.Range - 0.5f);
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world) { }


			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				_lastMouseInput = mi; // Store the mouse location so we can render a circles

				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? "ability" : "move-blocked";
			}
		}
	}

	// tag trait for the building
	class IronCurtainInfo : TraitInfo<IronCurtain> { }
	class IronCurtain { }
}
