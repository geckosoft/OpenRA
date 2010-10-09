using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Modules
{
    [LuaObject("Engine", "Engine")]
    public class ModEngine
    {
        public LuaBot Bot = null;
        public ModEngine(LuaBot bot)
        {
            Bot = bot;
        }

        [LuaFunction("debug")]
        public void Print(string text)
        {
            Bot.Debug(text);
        }
    }
}
