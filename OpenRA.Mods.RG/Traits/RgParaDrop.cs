using System.Collections.Generic;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.Rg.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgParaDropInfo : TraitInfo<RgParaDrop>
	{
		public readonly string ChuteSound = "";
		public readonly int LZRange = 4;
	}

	public class RgParaDrop : ITick
	{
		private readonly List<int2> droppedAt = new List<int2>();
		private Actor flare;
		private int2 lz;

		#region ITick Members

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<RgParaDropInfo>();
			int r = info.LZRange;

			if ((self.Location - lz).LengthSquared <= r*r && !droppedAt.Contains(self.Location))
			{
				var cargo = self.Trait<Cargo>();
				if (cargo.IsEmpty(self))
					FinishedDropping(self);
				else
				{
					if (!IsSuitableCell(cargo.Peek(self), self.Location))
						return;

					// unload a dude here
					droppedAt.Add(self.Location);

					Actor a = cargo.Unload(self);
					var rs = a.Trait<RenderSimple>();

					var aircraft = self.Trait<IMove>();

					self.World.AddFrameEndTask(w => w.Add(
						new RgFakeParachute(self.Owner, rs.anim.Name,
						                    Util.CenterOfCell(Util.CellContaining(self.CenterLocation)),
						                    aircraft.Altitude/2, a)));

					Sound.Play(info.ChuteSound, self.CenterLocation);
				}
			}
		}

		#endregion

		public void SetLZ(int2 lz, Actor flare)
		{
			this.lz = lz;
			this.flare = flare;
			droppedAt.Clear();
		}

		private bool IsSuitableCell(Actor actorToDrop, int2 p)
		{
			return actorToDrop.Trait<ITeleportable>().CanEnterCell(p);
		}

		private void FinishedDropping(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new FlyOffMap {Interruptible = false});
			self.QueueActivity(new RemoveSelf());

			if (flare != null)
			{
				flare.CancelActivity();
				flare.QueueActivity(new Wait(300));
				flare.QueueActivity(new RemoveSelf());
			}
		}
	}
}