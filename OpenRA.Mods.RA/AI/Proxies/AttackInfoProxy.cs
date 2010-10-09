using OpenRA.Traits;
using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Proxies
{
    [LuaObject("AttackInfo")]
    public class AttackInfoProxy : IProxy
    {
        protected AttackInfo Field = null;
        public string ObjectType { get { return "AttackInfo"; } }

        public AttackInfoProxy(AttackInfo field)
        {
            Field = field;
        }

        [LuaFunction("getDamage", RequireObject = true)]
        public static int GetName(AttackInfoProxy self)
        {
            return self.Field.Damage;
        }

        [LuaFunction("getAttacker", RequireObject = true)]
        public static ActorProxy GetAttacker(AttackInfoProxy self)
        {
            return self.Field.Attacker;
        }

        public static implicit operator AttackInfo(AttackInfoProxy d)
        {
            return d.Field;
        }

        public static implicit operator AttackInfoProxy(AttackInfo d)
        {
            return new AttackInfoProxy(d);
        }
    }
}
