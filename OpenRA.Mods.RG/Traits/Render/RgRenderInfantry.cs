using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Render
{
	public class RgRenderInfantryInfo : RenderSimpleInfo
	{
		public override object Create(ActorInitializer init) { return new RgRenderInfantry(init.self); }
	}

	public class RgRenderInfantry : RenderSimple, INotifyAttack, INotifyDamage
	{
		public RgRenderInfantry(Actor self)
			: base(self, () => self.Trait<IFacing>().Facing)
		{
			anim.Play("stand");
		}

		bool ChooseMoveAnim(Actor self)
		{
			var mobile = self.Trait<Mobile>();
			if (!mobile.IsMoving || (IsProne(self) && GetProne(self).Info.Speed <= 0f)) return false;

			if (float2.WithinEpsilon(self.CenterLocation, Util.CenterOfCell(mobile.toCell), 2)) return false;

			var seq = IsProne(self) ? GetProne(self).Info.Animation : "run";

			if (anim.CurrentSequence.Name != seq)
				anim.PlayRepeating(seq);

			return true;
		}

		bool inAttack = false;
		bool IsProne(Actor self)
		{
			var takeCover = self.TraitOrDefault<RgProne>();
			return takeCover != null && takeCover.IsProne;
		}

		RgProne GetProne(Actor self)
		{
			return self.TraitOrDefault<RgProne>();
		}

		public void Attacking(Actor self)
		{
			inAttack = true;

			var seq = IsProne(self) ? GetProne(self).Info.ShootAnimation : "shoot";

			if (anim.HasSequence(seq))
				anim.PlayThen(seq, () => inAttack = false);
			else if (anim.HasSequence("heal"))
				anim.PlayThen("heal", () => inAttack = false);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (inAttack) return;
			if (self.GetCurrentActivity() is IdleAnimation) return;
			if (ChooseMoveAnim(self)) return;

			if (IsProne(self))
				anim.PlayFetchIndex(GetProne(self).Info.Animation, () => 0);			/* what a hack. */
			else
				anim.Play("stand");
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				var death = e.Warhead != null ? e.Warhead.InfDeath : 0;
				Sound.PlayVoice("Die", self, self.Owner.Country.Race);
				self.World.AddFrameEndTask(w => w.Add(new Corpse(self, death)));
			}
		}
	}
}
