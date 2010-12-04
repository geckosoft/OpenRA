using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGUnitOrderGeneratorInfo : ITraitInfo
	{
		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGUnitOrderGenerator();
		}

		#endregion
	}

	public class RGUnitOrderGenerator : ICustomUnitOrderGenerator
	{
		#region ICustomUnitOrderGenerator Members

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			var underCursor = world.FindUnitsAtMouse(mi.Location)
				.Where(a => a.HasTrait<ITargetable>())
				.OrderByDescending(
					a =>
					a.Info.Traits.Contains<SelectableInfo>()
						? a.Info.Traits.Get<SelectableInfo>().Priority
						: int.MinValue)
				.FirstOrDefault();

			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, xy, mi, underCursor))
				.Where(o => o != null)
				.ToArray();

			var actorsInvolved = orders.Select(o => o.self).Distinct();
			if (actorsInvolved.Any())
				yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor, false)
				{
					TargetString = string.Join(",", actorsInvolved.Select(a => a.ActorID.ToString()).ToArray())
				};

			foreach (var o in orders)
				yield return CheckSameOrder(o.iot, o.trait.IssueOrder(o.self, o.iot, o.target, mi.Modifiers.HasModifier(Modifiers.Shift)));
		}

		public void Tick(World world)
		{
		}

		public void RenderBeforeWorld(WorldRenderer wr, World world)
		{
			foreach (Actor a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (IPreRenderSelection t in a.TraitsImplementing<IPreRenderSelection>())
						t.RenderBeforeWorld(wr, a);

			/* custom rendering */
			foreach (Actor a in world.Actors)
				if (!a.Destroyed)
					foreach (IRGPreRender t in a.TraitsImplementing<IRGPreRender>())
						t.RGRenderBeforeWorld(wr, a);

			Game.Renderer.Flush();
		}

		public void RenderAfterWorld(WorldRenderer wr, World world)
		{
			foreach (Actor a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (IPostRenderSelection t in a.TraitsImplementing<IPostRenderSelection>())
						t.RenderAfterWorld(wr, a);

			/* custom rendering */
			foreach (Actor a in world.Actors)
				if (!a.Destroyed)
					foreach (IRGPostRender t in a.TraitsImplementing<IRGPostRender>())
						t.RGRenderAfterWorld(wr, a);

			Game.Renderer.Flush();
		}

		public string GetCursor(World world, int2 xy, MouseInput mi)
		{
			Actor underCursor = world.FindUnitsAtMouse(mi.Location)
				.Where(a => a.HasTrait<ITargetable>())
				.OrderByDescending(
					a => a.Info.Traits.Contains<SelectableInfo>() ? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue)
				.FirstOrDefault();

			if (mi.Modifiers.HasModifier(Modifiers.Shift) || !world.Selection.Actors.Any())
				if (underCursor != null)
					return "select";

			UnitOrderResult[] orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, xy, mi, underCursor))
				.Where(o => o != null)
				.ToArray();

			if (orders.Length == 0) return "default";

			return orders[0].cursor ?? "default";
		}

		#endregion

		private static UnitOrderResult OrderForUnit(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			/* the following should be modified to allow controlling other ppl their units! */
			/*if (underCursor == null && self.Owner != self.World.LocalPlayer)
                return null;
            */
			/*
            if (self.Owner.Stances[underCursor])
            */

			/* todo Allow movement of tanks that your ally dont own - OR switch ownership once you 'capture' a vehicle! */
			if (self.Destroyed)
				return null;

			if (self.World.LocalPlayer == null)
				return null;

			if (self.Owner != self.World.LocalPlayer && self.Owner.Stances[self.World.LocalPlayer] != Stance.Ally)
				return null; // Disallow for sure if we dont own this unit and its not an ally!


			if (self.Owner != self.World.LocalPlayer) /* not a local unit, lets see if we can control it */
			{
				var steerable = self.TraitOrDefault<RGSteerable>();
				if (steerable == null || steerable.Passengers.Count() == 0)
					return null; /* no */

				// See if there is a local-player owned unit inside the passengers list
				if (!steerable.Passengers.Any( p => p.Owner == self.World.LocalPlayer))
					return null; /* no*/

				/* yes! */
			}

			if (!self.World.Map.IsInMap(xy.X, xy.Y))
				return null;

			if (self.Destroyed)
				return null;

			if (mi.Button == MouseButton.Right)
			{
				bool forceAttack = mi.Modifiers.HasModifier(Modifiers.Ctrl);

				// Check if ANY visor is up
				var hasVisorUp = self.TraitsImplementing<IRGVisor>().Any(v => v.Enabled);
				//if (hasVisorUp && !forceAttack)
				//	forceAttack = true;

				var uim = self.World.WorldActor.Trait<UnitInfluence>();
				foreach (var o in self.TraitsImplementing<IIssueOrder>()
					.SelectMany(trait => trait.Orders
						.Select(x => new { Trait = trait, Order = x }))
					.OrderByDescending(x => x.Order.OrderPriority))
				{
					var actorsAt = uim.GetUnitsAt(xy).Where(a => !a.Destroyed && a.IsInWorld).ToList();

					string cursor = null;
					if (underCursor != null)
						if (o.Order.CanTargetUnit(self, underCursor, forceAttack, mi.Modifiers.HasModifier(Modifiers.Alt), false, ref cursor))
							return new UnitOrderResult(self, o.Order, o.Trait, cursor, Target.FromActor(underCursor));
					if (o.Order.CanTargetLocation(self, xy, actorsAt, forceAttack, mi.Modifiers.HasModifier(Modifiers.Alt), false, ref cursor))
						return new UnitOrderResult(self, o.Order, o.Trait, cursor, Target.FromCell(xy));
				}
			}

			return null;
		}

		private static Order CheckSameOrder(IOrderTargeter iot, Order order)
		{
			if (order == null && iot.OrderID != null)
				Game.Debug("BUG: in order targeter - decided on {0} but then didn't order", iot.OrderID);
			else if (iot.OrderID != order.OrderString)
				Game.Debug("BUG: in order targeter - decided on {0} but ordered {1}", iot.OrderID, order.OrderString);
			return order;
		}

		#region Nested type: UnitOrderResult

		private class UnitOrderResult
		{
			public readonly string cursor;
			public readonly IOrderTargeter iot;
			public readonly Actor self;
			public readonly Target target;
			public readonly IIssueOrder trait;

			public UnitOrderResult(Actor self, IOrderTargeter iot, IIssueOrder trait, string cursor, Target target)
			{
				this.self = self;
				this.iot = iot;
				this.trait = trait;
				this.cursor = cursor;
				this.target = target;
			}
		}

		#endregion
	}
}