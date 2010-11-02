using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgProductionInfo : ITraitInfo
	{
		public readonly string[] Produces = {};

		#region ITraitInfo Members

		public virtual object Create(ActorInitializer init)
		{
			return new RgProduction(this);
		}

		#endregion
	}

	public class RgProduction
	{
		public RgProductionInfo Info;

		public RgProduction(RgProductionInfo info)
		{
			Info = info;
		}

		public Actor DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, Order order)
		{
			Actor newUnit = self.World.CreateActor(false, producee.Name, new TypeDictionary
			                                                             	{
			                                                             		new OwnerInit(self.Owner),
			                                                             	});

			int2 exit = self.Location + exitinfo.ExitCell;
			int2 spawn = self.Trait<IHasLocation>().PxPosition + exitinfo.SpawnOffset;

			var mobile = newUnit.Trait<Mobile>();
			var facing = newUnit.TraitOrDefault<IFacing>();

			// Set the physical position of the unit as the exit cell
			mobile.SetPosition(newUnit, exit);
			int2 to = Util.CenterOfCell(exit);
			mobile.PxPosition = spawn;
			if (facing != null)
				facing.Facing = exitinfo.Facing < 0 ? Util.GetFacing(to - spawn, facing.Facing) : exitinfo.Facing;
			self.World.Add(newUnit);

			// Animate the spawn -> exit transition
			float speed = mobile.MovementSpeedForCell(self, exit);
			int length = speed > 0 ? (int) ((to - spawn).Length*3/speed) : 0;
			newUnit.QueueActivity(new Drag(spawn, to, length));

			//			Log.Write("debug", "length={0} facing={1} exit={2} spawn={3}", length, facing.Facing, exit, spawn);

			// For the target line
			int2 target = exit;
			var rp = self.TraitOrDefault<RallyPoint>();
			if (rp != null)
			{
				target = rp.rallyPoint;
				// Todo: Move implies unit has Mobile
				newUnit.QueueActivity(mobile.MoveTo(target, 1));
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

			if (order != null && order.TargetActor != null)
			{
				var assigner = self.TraitOrDefault<RgAssignUnit>();
				if (assigner != null)
				{
					assigner.Enqueue(order.TargetActor.Owner);
				}
			}

			foreach (INotifyProduction t in self.TraitsImplementing<INotifyProduction>())
				t.UnitProduced(self, newUnit, exit);

			return newUnit;
			//Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);
		}

		public virtual Actor Produce(Actor self, ActorInfo producee, Order order)
		{
			// Todo: remove assumption on Mobile; 
			// required for 3-arg CanEnterCell
			//var mobile = newUnit.Trait<Mobile>();
			var mobileInfo = producee.Traits.Get<MobileInfo>();
			var bim = self.World.WorldActor.Trait<BuildingInfluence>();
			var uim = self.World.WorldActor.Trait<UnitInfluence>();

			// Pick a spawn/exit point pair
			// Todo: Reorder in a synced random way
			foreach (ExitInfo s in self.Info.Traits.WithInterface<ExitInfo>())
				if (Mobile.CanEnterCell(mobileInfo, self.World, uim, bim, self.Location + s.ExitCell, self, true))
				{
					return DoProduction(self, producee, s, order);
				}
			return null;
		}
	}
}