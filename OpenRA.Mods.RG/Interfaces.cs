using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Graphics;

namespace OpenRA.Mods.Rg
{
    public interface IRgPostRender { void RgRenderAfterWorld(WorldRenderer wr, Actor self); }
    public interface IRgPreRender { void RgRenderBeforeWorld(WorldRenderer wr, Actor self); }
}
