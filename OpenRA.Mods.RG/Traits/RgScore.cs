using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgScoreInfo : ITraitInfo
	{
		/* only because we have other ctors */

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgScore(init.self, this);
		}

		#endregion
	}

	public class RgScore : INotifyDamage
	{
		public RgScoreInfo Info;
		public long Score;
		public Actor Self;

		public RgScore(Actor self, RgScoreInfo info)
		{
			Self = self;
			Info = info;
		}

		#region INotifyDamage Members

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!self.IsIdle) return;
			/*if (e.Attacker.Destroyed) return;*/

			if (self.TraitOrDefault<RgScore>() == null) return;
			if (self.Owner.Stances[e.Attacker.Owner] == Stance.Ally && e.Damage > 0)
				return; /* dont give score to players who tk */

			if (e.Damage < 0 && self.Owner.Stances[e.Attacker.Owner] == Stance.Ally)
			{
				/* someone is healing you ! Give score! :) */
				int score = e.Health - e.PreviousHealth;
				if (score <= 0) return;
				e.Attacker.Owner.PlayerActor.TraitOrDefault<RgScore>().Score += score;

				/* and give cash */
				e.Attacker.Owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(score);
			}
			else if (e.Damage > 0)
			{
				int score = e.PreviousHealth - e.Health;
				if (score <= 0) return;
				e.Attacker.Owner.PlayerActor.TraitOrDefault<RgScore>().Score += score;

				/* and give cash */
				e.Attacker.Owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(score);
			}
		}

		#endregion
	}
}