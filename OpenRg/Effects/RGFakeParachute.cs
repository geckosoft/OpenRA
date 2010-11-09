using System.Collections.Generic;
using OpenRA;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;
using Util = OpenRA.Traits.Util;

namespace OpenRg.Effects
{
	public class RGFakeParachute : IEffect
	{
		private const float FallRate = .3f;
		private readonly Animation _anim;
		private readonly Actor _cargo;
		private readonly float2 _location;
		private readonly Player _owner;

		private float _altitude;

		public RGFakeParachute(Player owner, string image, float2 location, int altitude, Actor cargo)
		{
			_location = location;
			_altitude = altitude;
			_cargo = cargo;
			_owner = owner;

			_anim = new Animation(image);
			_anim.PlayFetchIndex(_anim.HasSequence("idle") ? "idle" : "stand", () => 0);
			_anim.Tick();
			/*
				paraAnim = new Animation("parach");
				paraAnim.PlayThen("open", () => paraAnim.PlayRepeating("idle"));
			*/
		}

		#region IEffect Members

		public void Tick(World world)
		{
			// paraAnim.Tick();
			_altitude -= FallRate;

			if (_altitude <= 0)
				world.AddFrameEndTask(w =>
				{
					w.Remove(this);
					int2 loc = Util.CellContaining(_location);
					_cargo.CancelActivity();
					_cargo.Trait<ITeleportable>().SetPosition(_cargo, loc);
					w.Add(_cargo);
				});
		}

		public IEnumerable<Renderable> Render()
		{
			float2 pos = _location - new float2(0, _altitude);
			yield return new Renderable(_anim.Image, _location - .5f * _anim.Image.size, "shadow", 0);
			yield return new Renderable(_anim.Image, pos - .5f * _anim.Image.size, _owner.Palette, 2);
			/* yield return new Renderable(paraAnim.Image, pos - .5f * paraAnim.Image.size, owner.Palette, 3);*/
		}

		#endregion
	}
}