using System.Linq;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRg.Traits;
using OpenRg.Traits.Abilities;

namespace OpenRg.Activities
{
	public class RGPlaceMine : CancelableActivity
	{
		public string Mine = "minp";

		public RGPlaceMine(string mine)
		{
			Mine = mine;
		}

		protected override bool OnCancel(Actor self)
		{
			return true;
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;

			var rgPlayer = (RGPlayer)self;

			if (rgPlayer == null)
				return NextActivity; /* should not occur! */

			var inv = rgPlayer.Inventory;
			var itm = inv.Get<RGInvMine>();
			if (itm == null || !itm.CanTake(1))
				return NextActivity;

			if (CanLayMine(self, self.Location))
				LayMine(self);

			return NextActivity;
		}

		bool CanLayMine(Actor self, int2 p)
		{
			// if there is no unit (other than me) here, we want to place a mine here
			return !self.World.WorldActor.Trait<UnitInfluence>()
				.GetUnitsAt(p).Any(a => a != self);
		}

		void LayMine(Actor self)
		{
			var inv = RGPlayer.Get(self).Inventory;
			var mines = inv.Get<RGInvMine>();
			if (mines.Amount == 0) return;

			self.World.AddFrameEndTask(
				w =>
					{
						w.CreateActor(Mine, new TypeDictionary
                    	{
                    		new LocationInit(self.Location),
                    		new OwnerInit( RGPlayer.Get(self).Faction) /* @todo Add self.Owner but this will require adding in an 'exception' for stuff like mines @ avatar detection */
                    	});
						mines.Take(1);
					});
		}
	}
}