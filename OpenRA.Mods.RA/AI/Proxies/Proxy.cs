using OpenRA.Traits;
using SharpLua;

namespace OpenRA.Mods.RA.AI.Proxies
{
    public class Proxy
    {
        protected LuaVM VM;

        public Proxy(LuaVM vm)
        {
            VM = vm;
        }

        public object Get(object obj)
        {
            if (obj as Actor != null)
                return new ActorProxy((Actor)obj);

            if (obj is int2)
                return new Int2Proxy((int2)obj);

            if (obj as int2? != null)
                return new Int2Proxy((int2?)obj);

            if (obj as Player != null)
                return new PlayerProxy((Player)obj);

            if (obj as ProductionItem != null)
                return new ProductionItemProxy((ProductionItem)obj);

            if (obj as AttackInfo != null)
                return new AttackInfoProxy((AttackInfo)obj);

            return null;
        }

        public LuaUserData GetVar(object obj)
        {
            var i = Get(obj);
            if (i == null)
                return null;

            return VM.UserData.CreateVar(i, ((IProxy)i).ObjectType);
        }
    }
}

