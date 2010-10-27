using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits.Abilities
{
	public abstract class RgAbilityInfo : ITraitInfo
	{
		public readonly bool RequiresPower = true;
		public readonly bool OneShot = false;
		public readonly float ChargeTime = 0;
		public readonly string Image = null;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		//[ActorReference]
		/*public readonly string[] Prerequisites = { };*/
		//public readonly bool GivenAuto = true;

		public readonly string OrderName;

		public readonly string BeginChargeSound = null;
		public readonly string EndChargeSound = null;
		public readonly string SelectTargetSound = null;
		public readonly string LaunchSound = null;

		public abstract object Create(ActorInitializer init);

		public RgAbilityInfo() { OrderName = GetType().Name + "Order"; }
	}

	public class RgAbility : ITick
	{
		public readonly RgAbilityInfo Info;
		public virtual int RemainingTime { get; private set; }
		public virtual int TotalTime { get { return (int)(Info.ChargeTime * 60 * 25); } }
		public virtual bool IsUsed { get; set; }
		public virtual bool IsAvailable { get { return !Info.OneShot || Info.OneShot && !IsUsed; } }
		public virtual bool IsReady { get { return IsAvailable && RemainingTime == 0; } }

		protected readonly Actor Self;
		protected readonly Player Owner;

		bool notifiedCharging;
		bool notifiedReady;

		public virtual string Details
		{
			get { return ""; }
		}

		//readonly PowerManager PlayerPower;
		public RgAbility(Actor self, RgAbilityInfo info)
		{
			Info = info;
			RemainingTime = TotalTime;
			Self = self;
			Owner = self.Owner;
			/*PlayerPower = self.Trait<PowerManager>();*/
		}

		public void Tick(Actor self)
		{
			if (self.Destroyed || !self.IsInWorld) /* when in a container for example  */
				return;
			if (Info.OneShot && IsUsed)
				return;

			if (IsAvailable)
			{
				if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().FastCharge) RemainingTime = 0;

				if (RemainingTime > 0) --RemainingTime;
				if (!notifiedCharging)
				{
					Sound.PlayToPlayer(Owner, Info.BeginChargeSound);
					OnBeginCharging();
					notifiedCharging = true;
				}
			}

			if (RemainingTime == 0
				&& !notifiedReady)
			{
				Sound.PlayToPlayer(Owner, Info.EndChargeSound);
				OnFinishCharging();
				notifiedReady = true;
			}
		}

		public void FinishActivate()
		{
			if (Info.OneShot)
			{
				IsUsed = true;
				/*IsAvailable = false;*/
			}
			RemainingTime = TotalTime;
			notifiedReady = false;
			notifiedCharging = false;
		}

		/*
		public void Give(float charge)
		{
			IsAvailable = true;
			IsUsed = false;
			RemainingTime = (int)(charge * TotalTime);
		}
		*/
		protected virtual void OnBeginCharging() { }
		protected virtual void OnFinishCharging() { }
		protected virtual void OnActivate() { }

		public void Activate()
		{
			if (!IsAvailable || !IsReady)
				return;

			Sound.PlayToPlayer(Owner, Info.SelectTargetSound);
			OnActivate();
		}
	}
}
