using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRg.Traits;

namespace OpenRg.Activities
{
	public class RGExitSteerable : CancelableActivity
	{
		protected Player UnloadMyAvatar;

		public RGExitSteerable(Player unloadMyAvatar)
		{
			UnloadMyAvatar = unloadMyAvatar;
		}

		private int2? ChooseExitTile(Actor self, Actor cargo)
		{
			// is anyone still hogging this tile?
			if (self.World.WorldActor.Trait<UnitInfluence>().GetUnitsAt(self.Location).Count() > 1)
				return null;

			var mobile = cargo.Trait<Mobile>();

			for (int i = -1; i < 2; i++)
				for (int j = -1; j < 2; j++)
					if ((i != 0 || j != 0) &&
					    mobile.CanEnterCell(self.Location + new int2(i, j)))
						return self.Location + new int2(i, j);

			return null;
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;

			// if we're a thing that can turn, turn to the
			// right facing for the unload animation
			var facing = self.TraitOrDefault<IFacing>();
			// Dont use this anymore ^^
			/*var unloadFacing = self.Info.Traits.Get<RgSteerableInfo>().UnloadFacing;
			if (facing != null && facing.Facing != unloadFacing)
				return Util.SequenceActivities( new Turn(unloadFacing), this );
            */
			// todo: handle the BS of open/close sequences, which are inconsistent,
			//		for reasons that probably make good sense to the westwood guys.

			var cargo = self.Trait<RGSteerable>();
			if (cargo.IsEmpty(self))
				return NextActivity;

			var ru = self.TraitOrDefault<RenderUnit>();
			try
			{
				//if (ru != null)
				//	ru.PlayCustomAnimation(self, "unload", null);
			}
			catch
			{
			}
			int2? exitTile = ChooseExitTile(self, cargo.Peek(self));
			if (exitTile == null)
				return this;

			Actor actor = cargo.Unload(UnloadMyAvatar);

			if (actor != null)
			self.World.AddFrameEndTask(w =>
			                           	{
			                           		w.Add(actor);
											var rgplayer = (RGPlayer)actor;
			                           		if (rgplayer != null)
			                           		{
			                           			rgplayer.Container = null;
			                           		}

			                           		var mobile = self.Trait<Mobile>();

			                           		actor.TraitsImplementing<IMove>().FirstOrDefault().SetPosition(actor, self.Location);
			                           		actor.CancelActivity();
			                           		actor.QueueActivity(mobile.MoveTo(exitTile.Value, 0));

			                           		if (actor.Owner == self.World.LocalPlayer)
			                           		{
			                           			var line = actor.TraitOrDefault<DrawLineToTarget>();
			                           			if (line != null)
			                           				line.SetTargetSilently(self, Target.FromCell(exitTile.Value), Color.Green);
			                           		}
			                           	});

			return this;
		}
	}
}