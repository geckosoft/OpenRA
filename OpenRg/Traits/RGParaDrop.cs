using System.Collections.Generic;
using OpenRA;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;
using OpenRg.Activities;
using OpenRg.Effects;

namespace OpenRg.Traits
{
	public class RGParaDropInfo : TraitInfo<RGParaDrop>
	{
		public readonly string ChuteSound = "";
		public readonly int LZRange = 4;
	}

	public class RGParaDrop : ITick
	{
		private readonly List<int2> _droppedAt = new List<int2>();
		private Actor _flare;
		private int2 _lz;

		#region ITick Members

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<RGParaDropInfo>();
			int r = info.LZRange;

			if ((self.Location - _lz).LengthSquared <= r * r && !_droppedAt.Contains(self.Location))
			{
				var cargo = self.Trait<Cargo>();
				if (cargo.IsEmpty(self))
					FinishedDropping(self);
				else
				{
					if (!IsSuitableCell(cargo.Peek(self), self.Location))
						return;

					// unload a dude here
					_droppedAt.Add(self.Location);

					var a = cargo.Unload(self);
					var rs = a.Trait<RenderSimple>();

					var aircraft = self.Trait<IMove>();

					self.World.AddFrameEndTask(w => w.Add(
						new RGFakeParachute(self.Owner, rs.anim.Name,
											Util.CenterOfCell(Util.CellContaining(self.CenterLocation)),
											aircraft.Altitude / 2, a)));

					Sound.Play(info.ChuteSound, self.CenterLocation);
				}
			}
		}

		#endregion

		public void SetLZ(int2 lz, Actor flare)
		{
			_lz = lz;
			_flare = flare;
			_droppedAt.Clear();
		}

		public static bool IsSuitableCell(Actor actorToDrop, int2 p)
		{
			return actorToDrop.Trait<ITeleportable>().CanEnterCell(p);
		}

		private void FinishedDropping(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new FlyOffMap { Interruptible = false });
			self.QueueActivity(new RGRemoveSelf());

			if (_flare != null)
			{
				_flare.CancelActivity();
				_flare.QueueActivity(new Wait(300));
				_flare.QueueActivity(new RGRemoveSelf());
			}
		}
	}
}