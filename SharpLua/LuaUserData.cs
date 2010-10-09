using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Tao.Lua;

namespace SharpLua
{
    public class LuaUserData
    {
        public Object Object;
        public ulong Index;
        static public LuaUserData Zero = new LuaUserData();
        //public uint References = 0;
        public IntPtr Pointer;
        public bool Created = false;
        public string ObjectType = "";

        public LuaUserData()
        {
            
        }

        public LuaUserData(Object obj, ulong index)
        {
            Object = obj;
            Index = index;
        }

        public void Create(LuaVM vm)
        {
            if (ObjectType != "")
            {
                var ptr = Lua.lua_newuserdata(vm, 8);
                Marshal.Copy(BitConverter.GetBytes(Index), 0, ptr, 8);
                Pointer = ptr;

                Lua.luaL_getmetatable(vm, ObjectType);
                Lua.lua_setmetatable(vm, -2);
            }else
            {
                var ptr = Lua.lua_newuserdata(vm, 8);
                Marshal.Copy(BitConverter.GetBytes(Index), 0, ptr, 8);
                Pointer = ptr;
            }

            Created = true;
        }
    }
}
