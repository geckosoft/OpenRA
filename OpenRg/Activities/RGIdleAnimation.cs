using OpenRA;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRg.Traits.Render;

namespace OpenRg.Activities
{
	public class RGIdleAnimation : Idle
	{
		private readonly string _sequence;
		private bool _active = true;
		private int _delay;

		public RGIdleAnimation(string sequence, int delay)
		{
			_sequence = sequence;
			_delay = delay;
		}

		public override IActivity Tick(Actor self)
		{
			if (!_active) return NextActivity;

			if (_delay > 0 && --_delay == 0)
				self.Trait<RGRenderInfantry>().anim.PlayThen(_sequence, () => _active = false);

			return this;
		}

		protected override bool OnCancel(Actor self)
		{
			_active = false;

			return true;
		}
	}
}