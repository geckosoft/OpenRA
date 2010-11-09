using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGIsEngineerInfo : TraitInfo<RGIsEngineer> { }
	public class RGIsEngineer { }

	public class RGIonControlInfo : TraitInfo<RGIonControl> { }
	public class RGIonControl { }

	public class RGMustBeDestroyedInfo : TraitInfo<RGMustBeDestroyed> { }
	public class RGMustBeDestroyed { }

	public class RGMineImmuneInfo : TraitInfo<RGMineImmune> { }
	public class RGMineImmune { }

	/// <summary>
	/// Use RGNotAnAvatar so the player eventually CAN own more than 1 unit!
	/// </summary>
	public class RGNotAnAvatarInfo : TraitInfo<RGNotAnAvatar> { }
	public class RGNotAnAvatar { }
}