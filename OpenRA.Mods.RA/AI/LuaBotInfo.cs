using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
    public class LuaBotInfo : TraitInfo<LuaBot>, IBotInfo
    {
        [FieldLoader.LoadUsing("StoreYaml")]
        public MiniYaml Yaml = null;

        [FieldLoader.Load]
        public string Script = "";

        public LuaBotInfo(MiniYaml yaml)
        {
            Yaml = yaml;

            FieldLoader.Load(this, yaml);
        }

        public LuaBotInfo()
        {

        }

        public static object StoreYaml(MiniYaml y)
        {
            return y;
        }

        protected LuaBot Engine;

        #region Implementation of IBotInfo
        [FieldLoader.Load]
        public readonly string InternalName = "Lua Bot";
        [FieldLoader.Load]
        public readonly string InternalIdentifier = "LUA_BOT";

        public string Name
        {
            get { return InternalName; }
        }

        public string Identifier
        {
            get { return InternalIdentifier; }
        }

        public override object Create(ActorInitializer init)
        {
            return new LuaBot(init, this);
        }

        #endregion

        public virtual object Clone(IBot engine)
        {
            Engine = (LuaBot)engine;

            var res = new LuaBotInfo(Yaml);
            res.Engine = (LuaBot)engine;

            return res;
        }
    }
}