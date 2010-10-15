#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	public class ProductionInfo : ITraitInfo
	{
		public readonly string[] Produces = { };
		
		public virtual object Create(ActorInitializer init) { return new Production(this); }
	}

	public class ExitInfo : TraitInfo<Exit>
	{
		public readonly int2 SpawnOffset = int2.Zero; // in px relative to CenterLocation
		public readonly int2 ExitCell = int2.Zero; // in cells relative to TopLeft
		public readonly int Facing = -1;
	}
	public class Exit {}
	
	public class Production
	{		
		public ProductionInfo Info;
		public Production(ProductionInfo info)
		{
			Info = info;
		}
		
		public void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo)
		{
			var newUnit = self.World.CreateActor(false, producee.Name, new TypeDictionary
			{
			    new OwnerInit( self.Owner ),
			});

			var exit = self.Location + exitinfo.ExitCell;
			var spawn = self.Trait<IHasLocation>().PxPosition + exitinfo.SpawnOffset;

			var mobile = newUnit.Trait<Mobile>();
			var facing = newUnit.TraitOrDefault<IFacing>();
			
			// Set the physical position of the unit as the exit cell
			mobile.SetPosition(newUnit,exit);
			var to = Util.CenterOfCell(exit);
			mobile.PxPosition = spawn;
			if (facing != null)
				facing.Facing = exitinfo.Facing < 0 ? Util.GetFacing(to - spawn, facing.Facing) : exitinfo.Facing;
			self.World.Add(newUnit);

			// Animate the spawn -> exit transition
			var speed = mobile.MovementSpeedForCell(self, exit);
			var length = speed > 0 ? (int)( ( to - spawn ).Length*3 / speed ) : 0;
			newUnit.QueueActivity(new Drag(spawn, to, length));

//			Log.Write("debug", "length={0} facing={1} exit={2} spawn={3}", length, facing.Facing, exit, spawn);
			
			// For the target line
			var target = exit;
			var rp = self.TraitOrDefault<RallyPoint>();
			if (rp != null)
            {
				target = rp.rallyPoint;
				// Todo: Move implies unit has Mobile

                target = newUnit.TraitOrDefault<Mobile>().NearestMoveableCell(target); // Potential fix for rally points

				newUnit.QueueActivity(new Move(target, 5));
			}
			
			if (newUnit.Owner == self.World.LocalPlayer)
			{
				self.World.AddFrameEndTask(w =>
				{
					var line = newUnit.TraitOrDefault<DrawLineToTarget>();
					if (line != null)
						line.SetTargetSilently(newUnit, Target.FromCell(target), Color.Green);
				});
			}

			foreach (var t in self.TraitsImplementing<INotifyProduction>())
				t.UnitProduced(self, newUnit, exit);

			//Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);
		}
		
		public virtual bool Produce( Actor self, ActorInfo producee )
		{			
			// Todo: remove assumption on Mobile; 
			// required for 3-arg CanEnterCell
			//var mobile = newUnit.Trait<Mobile>();
			var mobileInfo = producee.Traits.Get<MobileInfo>();
			var bim = self.World.WorldActor.Trait<BuildingInfluence>();
			var uim = self.World.WorldActor.Trait<UnitInfluence>();
			
			// Pick a spawn/exit point pair
			// Todo: Reorder in a synced random way
			foreach (var s in self.Info.Traits.WithInterface<ExitInfo>())
				if( Mobile.CanEnterCell( mobileInfo, self.World, uim, bim, self.Location + s.ExitCell,self,true ) )
				{
					DoProduction(self, producee, s);
					return true;
				}
			return false;
		}


	}
}
