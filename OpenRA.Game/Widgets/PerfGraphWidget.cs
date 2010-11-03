#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class PerfGraphWidget : Widget
	{
		public PerfGraphWidget() : base() { }

		public override void DrawInner( WorldRenderer wr )
		{
			var rect = RenderBounds;
			float2 origin = Game.viewport.Location + new float2(rect.Right, rect.Bottom);
			float2 basis = new float2(-rect.Width / 100, -rect.Height / 100);

			Game.Renderer.LineRenderer.DrawLine(origin, origin + new float2(100, 0) * basis, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin + new float2(100, 0) * basis, origin + new float2(100, 100) * basis, Color.White, Color.White);

			foreach (var item in PerfHistory.items.Values)
			{
				int n = 0;
				item.Samples().Aggregate((a, b) =>
				{
					Game.Renderer.LineRenderer.DrawLine(
						origin + new float2(n, (float)a) * basis,
						origin + new float2(n + 1, (float)b) * basis,
						item.c, item.c);
					++n;
					return b;
				});
			}
		}
	}
}