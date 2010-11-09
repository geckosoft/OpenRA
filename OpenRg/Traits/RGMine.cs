using System.Collections.Generic;
using OpenRA;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	class RGMineInfo : ITraitInfo
	{
		public readonly string[] CrushClasses = { };
		[WeaponReference]
		public readonly string Weapon = "ATMine";
		//public readonly bool AvoidFriendly = true;

		public object Create(ActorInitializer init) { return new RGMine(init, this); }
	}

	class RGMine : ICrushable, IOccupySpace
	{
		public readonly Actor Self;
		public readonly RGMineInfo Info;
		[Sync]
		public readonly int2 Location;

		public RGMine(ActorInitializer init, RGMineInfo info)
		{
			Self = init.self;
			Info = info;
			Location = init.Get<LocationInit, int2>();
			Self.World.WorldActor.Trait<UnitInfluence>().Add(Self, this);
		}

		public void OnCrush(Actor crusher)
		{
			if (crusher.HasTrait<RGMineImmune>() && crusher.Owner == Self.Owner)
				return;
			if (crusher.Owner.Stances[Self.Owner] != Stance.Enemy)
				return;

			Combat.DoExplosion(Self, Info.Weapon, crusher.CenterLocation, 0);
			Self.QueueActivity(new RemoveSelf());
		}

		// TODO: Re-implement friendly-mine avoidance
		public IEnumerable<string> CrushClasses { get { return Info.CrushClasses; } }

		public int2 TopLeft { get { return Location; } }

		public IEnumerable<int2> OccupiedCells() { yield return TopLeft; }
		public int2 PxPosition { get { return Util.CenterOfCell(Location); } }
	}

}
