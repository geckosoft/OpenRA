#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;
using System.Diagnostics;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	public class MobileInfo : ITraitInfo
	{
		[FieldLoader.LoadUsing( "LoadSpeeds" )]
		public readonly Dictionary<string, TerrainInfo> TerrainSpeeds;
		[FieldLoader.Load] public readonly string[] Crushes;
		[FieldLoader.Load] public readonly int WaitAverage = 60;
		[FieldLoader.Load] public readonly int WaitSpread = 20;
		[FieldLoader.Load] public readonly int InitialFacing = 128;
		[FieldLoader.Load] public readonly int ROT = 255;
		[FieldLoader.Load] public readonly int Speed = 1;
		[FieldLoader.Load] public readonly bool OnRails = false;

		public virtual object Create(ActorInitializer init) { return new Mobile(init, this); }
		
		static object LoadSpeeds( MiniYaml y )
		{
			Dictionary<string,TerrainInfo> ret = new Dictionary<string, TerrainInfo>();
			foreach (var t in y.NodesDict["TerrainSpeeds"].Nodes)
			{
				var speed = (decimal)FieldLoader.GetValue("speed", typeof(decimal),t.Value.Value);
				var cost = t.Value.NodesDict.ContainsKey("PathingCost") ? (int)FieldLoader.GetValue("cost", typeof(int), t.Value.NodesDict["PathingCost"].Value) : (int)(10000/speed);
				ret.Add(t.Key, new TerrainInfo{Speed = speed, Cost = cost});
			}
			
			return ret;
		}
		
		public class TerrainInfo
		{
			public int Cost = int.MaxValue;
			public decimal Speed = 0;
		}
	}
	
	public class Mobile : IIssueOrder, IResolveOrder, IOrderVoice, IOccupySpace, IMove, IFacing, INudge
	{
		public readonly Actor self;
		public readonly MobileInfo Info;
		public bool IsMoving { get; internal set; }

		int __facing;
		int2 __fromCell, __toCell;
		int __altitude;

		[Sync]
		public int Facing
		{
			get { return __facing; }
			set { __facing = value; }
		}

		[Sync]
		public int Altitude
		{
			get { return __altitude; }
			set { __altitude = value; }
		}
		
		public int ROT { get { return Info.ROT; } }
		public int InitialFacing { get { return Info.InitialFacing; } }

		[Sync]
		public int2 PxPosition { get; set; }
		[Sync]
		public int2 fromCell { get { return __fromCell; } }
		[Sync]
		public int2 toCell { get { return __toCell; } }

		[Sync]
		public int PathHash;	// written by Move.EvalPath, to temporarily debug this crap.

		public void SetLocation(int2 from, int2 to)
		{
			if (fromCell == from && toCell == to) return;
			RemoveInfluence();
			__fromCell = from;
			__toCell = to;
			AddInfluence();
		}

		UnitInfluence uim;

		const int avgTicksBeforePathing = 5;
		const int spreadTicksBeforePathing = 5;
		internal int ticksBeforePathing = 0;
		
		public Mobile(ActorInitializer init, MobileInfo info)
		{
			this.self = init.self;
			this.Info = info;
			
			uim = self.World.WorldActor.Trait<UnitInfluence>();
			
			if (init.Contains<LocationInit>())
			{
				this.__fromCell = this.__toCell = init.Get<LocationInit,int2>();
				this.PxPosition = Util.CenterOfCell( fromCell );
			}
			
			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : info.InitialFacing;
			this.Altitude = init.Contains<AltitudeInit>() ? init.Get<AltitudeInit,int>() : 0;
		}

		public void SetPosition(Actor self, int2 cell)
		{
			SetLocation( cell, cell );
			PxPosition = Util.CenterOfCell(fromCell);
		}

		public void SetPxPosition( Actor self, int2 px )
		{
			var cell = Util.CellContaining( px );
			SetLocation( cell, cell );
			PxPosition = px;
		}

		public IEnumerable<IOrderTargeter> Orders { get { yield return new MoveOrderTargeter( Info ); } }

		// Note: Returns a valid order even if the unit can't move to the target
		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order is MoveOrderTargeter )
			{
				if( Info.OnRails ) return null;
				return new Order("Move", self, Util.CellContaining(target.CenterLocation), order.IsQueued);
			}
			return null;
		}

		public int2 NearestMoveableCell(int2 target)
		{
			if (CanEnterCell(target))
				return target;
			
			var searched = new List<int2>(){};
			// Limit search to a radius of 10 tiles
			for (int r = 1; r < 10; r++)
				foreach (var tile in self.World.FindTilesInCircle(target,r).Except(searched))
				{
					if (CanEnterCell(tile))
						return tile;
					
					searched.Add(tile);
				}
			
			// Couldn't find a cell
			return target;
		}

		protected void PerformMove(Actor self, int2 targetLocation, bool queued)
		{

			// Show feedback (flashes the target or the location)
			if (self.Owner == self.World.LocalPlayer)
				self.World.AddFrameEndTask(
					w =>
					{
						if (self.Destroyed) return;

						w.Add(new MoveFlash(w, targetLocation));
					});

			var ph = new QueuedActivity(
				(qa) =>
				{
					int2 currentLocation = NearestMoveableCell(targetLocation);

					if (!CanEnterCell(currentLocation))
					{
						if (queued) self.CancelActivity();
						return;
					}

					if (!queued) self.CancelActivity();

					ticksBeforePathing = avgTicksBeforePathing + self.World.SharedRandom.Next(-spreadTicksBeforePathing, spreadTicksBeforePathing);

					qa.Insert(new Move(currentLocation, 8));
					
					if (self.Owner == self.World.LocalPlayer)
						self.World.AddFrameEndTask(
							w =>
							{
								if (self.Destroyed) return;

								var line = self.TraitOrDefault<DrawLineToTarget>();
								if (line != null)
									line.SetTarget(self, Target.FromCell(currentLocation),
									               Color.Green);
							});
				});

			self.QueueActivity(queued ? ph : ph.Run(self));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
				PerformMove(self, order.TargetLocation, order.Queued && !self.IsIdle);
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
				return "Move";
			return null;
		}

		public int2 TopLeft { get { return toCell; } }

		public IEnumerable<int2> OccupiedCells()
		{
			return (fromCell == toCell)
				? new[] { fromCell }
				: CanEnterCell(toCell)
					? new[] { toCell }
					: new[] { fromCell, toCell };
		}

		public bool CanEnterCell(int2 p)
		{
			return CanEnterCell( p, null, true);
		}

		public static bool CanEnterCell( World world, MobileInfo mi, int2 cell, Actor ignoreActor, bool checkTransientActors )
		{
			var uim = world.WorldActor.Trait<UnitInfluence>();
			return Mobile.CanEnterCell( mi, world, uim, cell, ignoreActor, checkTransientActors );
		}
		
		public bool CanEnterCell( int2 cell, Actor ignoreActor, bool checkTransientActors )
		{
			var uim = self.World.WorldActor.Trait<UnitInfluence>();
			return CanEnterCell( Info, self.World, uim, cell, ignoreActor, checkTransientActors );
		}

		public static bool CanEnterCell( MobileInfo mobileInfo, World world, UnitInfluence uim, int2 cell, Actor ignoreActor, bool checkTransientActors )
		{
			if (MovementCostForCell(mobileInfo, world, cell) == int.MaxValue)
				return false;

			var blockingActors = uim.GetUnitsAt( cell ).Where( x => x != ignoreActor ).ToList();
			if (checkTransientActors && blockingActors.Count > 0)
			{
				// We can enter a cell with nonshareable units only if we can crush all of them
				if (mobileInfo.Crushes == null)
					return false;

				if (blockingActors.Any(a => !(a.HasTrait<ICrushable>() &&
											 a.TraitsImplementing<ICrushable>().Any(b => b.CrushClasses.Intersect(mobileInfo.Crushes).Any()))))
					return false;
			}

			return true;
		}
		
		public void FinishedMoving(Actor self)
		{
			var crushable = uim.GetUnitsAt(toCell).Where(a => a != self && a.HasTrait<ICrushable>());
			foreach (var a in crushable)
			{
				var crushActions = a.TraitsImplementing<ICrushable>().Where(b => b.CrushClasses.Intersect(Info.Crushes).Any());
				foreach (var b in crushActions)
					b.OnCrush(self);
			}
		}

		public static int MovementCostForCell(MobileInfo info, World world, int2 cell)
		{
			if (!world.Map.IsInMap(cell.X,cell.Y))
				return int.MaxValue;

			var type = world.GetTerrainType(cell);
			if (!info.TerrainSpeeds.ContainsKey(type))
				return int.MaxValue;
			
			return info.TerrainSpeeds[type].Cost;
		}

		public int MovementSpeedForCell(Actor self, int2 cell)
		{
			var type = self.World.GetTerrainType(cell);
			
			if (!Info.TerrainSpeeds.ContainsKey(type))
				return 0;

			decimal speed = Info.Speed * Info.TerrainSpeeds[type].Speed;
			foreach( var t in self.TraitsImplementing<ISpeedModifier>() )
				speed *= t.GetSpeedModifier();
			return (int)(speed / 100);
		}
		
		public void AddInfluence()
		{
			if( self.IsInWorld )
				uim.Add( self, this );
		}
		
		public void RemoveInfluence()
		{
			if( self.IsInWorld )
				uim.Remove( self, this );
		}

		public void OnNudge(Actor self, Actor nudger)
		{
			/* initial fairly braindead implementation. */

			if (self.Owner.Stances[nudger.Owner] != Stance.Ally)
				return;		/* don't allow ourselves to be pushed around
							 * by the enemy! */

			if (!self.IsIdle)
				return;		/* don't nudge if we're busy doing something! */

			// pick an adjacent available cell.
			var availCells = new List<int2>();
			var notStupidCells = new List<int2>();

			for( var i = -1; i < 2; i++ )
				for (var j = -1; j < 2; j++)
				{
					var p = toCell + new int2(i, j);
					if (CanEnterCell(p))
						availCells.Add(p);
					else
						if (p != nudger.Location && p != toCell)
							notStupidCells.Add(p);
				}

			var moveTo = availCells.Any() ? availCells.Random(self.World.SharedRandom) :
				notStupidCells.Any() ? notStupidCells.Random(self.World.SharedRandom) : (int2?)null;

			if (moveTo.HasValue)
			{
				self.CancelActivity();
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTargetSilently(self, Target.FromCell(moveTo.Value), Color.Green);
					});
				self.QueueActivity(new Move(moveTo.Value, 0));

				Log.Write("debug", "OnNudge #{0} from {1} to {2}",
					self.ActorID, self.Location, moveTo.Value);
			}
			else
				Log.Write("debug", "OnNudge #{0} refuses at {1}",
					self.ActorID, self.Location);
		}

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly MobileInfo unitType;

			public MoveOrderTargeter( MobileInfo unitType )
			{
				this.unitType = unitType;
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }
			public bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
			{
				return false;
			}

			public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
			{
				IsQueued = forceQueued;

				cursor = "move";
				if( self.World.LocalPlayer.Shroud.IsVisible( location ) && !self.Trait<Mobile>().CanEnterCell( location ) )
					cursor = "move-blocked";
				return true;
			}
		}

		public IActivity ScriptedMove( int2 cell ) { return new Move( cell ); }
		public IActivity MoveTo( int2 cell, int nearEnough ) { return new Move( cell, nearEnough ); }
		public IActivity MoveTo( int2 cell, Actor ignoredActor ) { return new Move( cell, ignoredActor ); }
		public IActivity MoveWithinRange( Actor target, int range ) { return new Move( target, range ); }
		public IActivity MoveWithinRange( Target target, int range ) { return new Move( target, range ); }
		public IActivity MoveTo( Func<List<int2>> pathFunc ) { return new Move( pathFunc ); }
	}
}
