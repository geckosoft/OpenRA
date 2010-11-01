using System;
using System.Collections.Generic;

namespace OpenRA.Traits
{
	class ServerEventsInfo : TraitInfo<ServerEvents>, ITraitNotSynced /* fixme is the not synced needed on the trait a well? */
	{

	}

	class ServerEvents : ICrushable, INotifyIdle, IWorldLoaded, IAcceptSpy, ITraitNotSynced, INotifyDamage, INotifyCapture, INotifyAttack, INotifySold, INotifyBuildComplete, INotifyProduction, ITick
	{
		public bool CanTrigger<T>() where T : class
		{
			if (!Game.IsHost || !Game.Settings.Server.IsDedicated || Game.Settings.Server.Extension == null)
				return false;

			if ((Game.Settings.Server.Extension is T) == false)
				return false;

			return true;
		}

		public T Extension<T>() where T : class
		{
			return (Game.Settings.Server.Extension as T);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!CanTrigger<INotifyDamage>()) return;

			Extension<INotifyDamage>().Damaged(self, e);
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (!CanTrigger<INotifyCapture>()) return;

			Extension<INotifyCapture>().OnCapture(self, captor, oldOwner, newOwner);
		}

		public void Attacking(Actor self)
		{
			if (!CanTrigger<INotifyAttack>()) return;

			Extension<INotifyAttack>().Attacking(self);
		}

		public void Selling(Actor self)
		{
			if (!CanTrigger<INotifySold>()) return;

			Extension<INotifySold>().Selling(self);
		}

		public void Sold(Actor self)
		{
			if (!CanTrigger<INotifySold>()) return;

			Extension<INotifySold>().Sold(self);
		}

		public void BuildingComplete(Actor self)
		{
			if (!CanTrigger<INotifyBuildComplete>()) return;

			Extension<INotifyBuildComplete>().BuildingComplete(self);
		}

		public void UnitProduced(Actor self, Actor other, int2 exit)
		{
			if (!CanTrigger<INotifyProduction>()) return;

			Extension<INotifyProduction>().UnitProduced(self, other, exit);
		}

		public void Tick(Actor self)
		{
			if (!CanTrigger<ITick>()) return;

			Extension<ITick>().Tick(self);
		}

		public void OnInfiltrate(Actor self, Actor spy)
		{
			if (!CanTrigger<IAcceptSpy>()) return;

			Extension<IAcceptSpy>().OnInfiltrate(self, spy);
		}

		public void WorldLoaded(World w)
		{
			if (!CanTrigger<IWorldLoaded>()) return;

			Extension<IWorldLoaded>().WorldLoaded(w);
		}

		public void Idle(Actor self)
		{
			if (!CanTrigger<INotifyIdle>()) return;

			Extension<INotifyIdle>().Idle(self);
		}

		public void OnCrush(Actor self, Actor crusher)
		{
			if (!CanTrigger<ICrushable>()) return;

			Extension<ICrushable>().OnCrush(self, crusher);
		}

		public IEnumerable<string> CrushClasses
		{
			get { return new string[0]; }
		}
	}
}

