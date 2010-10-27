using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	class RgMineInfo : ITraitInfo
	{
		public readonly string[] CrushClasses = { };
		[WeaponReference]
		public readonly string Weapon = "ATMine";
		//public readonly bool AvoidFriendly = true;

		public object Create(ActorInitializer init) { return new RgMine(init, this); }
	}

	class RgMine : ICrushable, IOccupySpace
	{
		public readonly Actor Self;
		public readonly RgMineInfo Info;
		[Sync]
		public readonly int2 Location;

		public RgMine(ActorInitializer init, RgMineInfo info)
		{
			Self = init.self;
			Info = info;
			Location = init.Get<LocationInit, int2>();
			Self.World.WorldActor.Trait<UnitInfluence>().Add(Self, this);
		}

		public void OnCrush(Actor crusher)
		{
			if (crusher.HasTrait<RgMineImmune>() && crusher.Owner == Self.Owner)
				return;
			if (crusher.Owner.Stances[Self.Owner] != Stance.Enemy)
				return;

			Combat.DoExplosion(Self, Info.Weapon, crusher.CenterLocation, 0);
			Self.QueueActivity(new RemoveSelf());
		}

		// TODO: Re-implement friendly-mine avoidance
		public IEnumerable<string> CrushClasses { get { return Info.CrushClasses; } }

		public int2 TopLeft { get { return Location; } }

		public IEnumerable<int2> OccupiedCells() { yield return TopLeft;}
		public int2 PxPosition { get { return Util.CenterOfCell(Location); } }
	}

	/* tag trait for stuff that shouldnt trigger mines */
	class RgMineImmuneInfo : TraitInfo<RgMineImmune> { }
	class RgMineImmune { }
}
