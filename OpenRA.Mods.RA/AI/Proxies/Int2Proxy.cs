using System;
using SharpLua;
using SharpLua.Attributes;
using SharpLua.Objects;

namespace OpenRA.Mods.RA.AI.Proxies
{
    [LuaObject("Int2")]
    public class Int2Proxy : IProxy
    {
        protected int2? Field;
        public string ObjectType { get { return "Int2";  } }

        public Int2Proxy(int2 field)
        {
            Field = field;
        }

        public Int2Proxy(int2? field)
        {
            Field = field;
        }

        private Int2Proxy(int x, int y)
        {
            Field = new int2(x, y);
        }

        [LuaFunction("new", RequireObject = false)]
        public static LuaUserData CreateObject(LuaVM vm, int x, int y)
        {
            return vm.UserData.Create(new Int2Proxy(x, y), "Int2");
        }

        [LuaFunction("getX", RequireObject = true)]
        public static int GetX(Int2Proxy self)
        {
            return self.Field.Value.X;
        }

        [LuaFunction("getY", RequireObject = true)]
        public static int GetY(Int2Proxy self)
        {
            return self.Field.Value.Y;
        }


        public static implicit operator int2(Int2Proxy d)
        {
            return d.Field.Value;
        }

        public static implicit operator int2?(Int2Proxy d)
        {
            return d.Field;
        }

        public static implicit operator Int2Proxy(int2 d)
        {
            return new Int2Proxy(d);
        }

        public static implicit operator Int2Proxy(int2? d)
        {
            return new Int2Proxy(d);
        }
    }
}
