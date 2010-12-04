using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGInvisibleInfo : ITraitInfo
	{
		public readonly bool Visible = false;

		public object Create(ActorInitializer init)
		{
			return new RGInvisible(init.self, this);
		}
	}

	public class RGInvisible : IRenderModifier, IVisibilityModifier
	{
		[Sync]
		private bool _visible;

		public RGInvisible(Actor self, RGInvisibleInfo info)
		{
			Visible = info.Visible;
		}

		public bool Visible
		{
			get { return _visible; }
			set { _visible = value; }
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			if (Visible)
				return r;

			return new Renderable[] {};
		}

		#region Implementation of IVisibilityModifier

		public bool IsVisible(Actor self)
		{
			return Visible;
		}

		#endregion
	}
}
