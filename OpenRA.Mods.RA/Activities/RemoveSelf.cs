﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class RemoveSelf : CancelableActivity
	{
		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			self.Destroy();
			return null;
		}
	}
}
