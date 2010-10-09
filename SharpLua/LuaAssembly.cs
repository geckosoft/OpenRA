using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SharpLua.Attributes;
using Tao.Lua;

namespace SharpLua
{
    public class LuaAssembly
    {
        /*
        public static void RegisterClass(LuaVM L, Type t)
        {
            //var t = obj.GetType();

            var methods = t.GetMethods().Where(a => a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).Length > 0).ToList();

            foreach (var methodInfo in methods)
            {
                var ca = methodInfo.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault() as LuaFunctionAttribute;

                if (methodInfo.IsStatic)
                {
                    if (ca != null) Lua.lua_register(L, ca.FunctionName, LuaCallback.Wrap(methodInfo, ca));
                }
                else if (!methodInfo.IsStatic)
                {

                   // if (ca != null) Lua.lua_register(L, ca.FunctionName, LuaCallback.Wrap(methodInfo, ca, T));
                }
            }
        }
        */
        /// <summary>
        /// Registers methods found in type T
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static void RegisterFunctions(IntPtr L,  Type t)
        {
            var methods = t.GetMethods().Where(a => a.GetCustomAttributes(typeof (LuaFunctionAttribute), false).Length > 0).ToList();
            var vm = LuaVM.GetInstance(L);

            foreach (var methodInfo in methods)
            {
                if (methodInfo.IsStatic)
                {
                    var ca = methodInfo.GetCustomAttributes(typeof (LuaFunctionAttribute), false).SingleOrDefault() as LuaFunctionAttribute;
                    var cb = LuaCallback.Wrap(methodInfo, ca);
                    vm.CallbackReferences.Add(cb); // Prevent GC
                    Lua.lua_register(L, ca.FunctionName, cb);
                }
            }
        }

        /*
        public static void RegisterFunctions(IntPtr L, Object obj)
        {
            var t = obj.GetType();

            var methods = t.GetMethods().Where(a => a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).Length > 0).ToList();

            foreach (var methodInfo in methods)
            {
                var ca = methodInfo.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault() as LuaFunctionAttribute;

                if (methodInfo.IsStatic)
                {
                    Lua.lua_register(L, ca.FunctionName, LuaCallback.Wrap(methodInfo, ca));
                }
                else if (!methodInfo.IsStatic)
                {
                    
                    Lua.lua_register(L, ca.FunctionName, LuaCallback.Wrap(methodInfo, ca, obj));
                }
            }
        }
        */

        public static void WrapLibrary(IntPtr L, Type t, string libName)
        {
            var methods =
                t.GetMethods().Where(a => a.GetCustomAttributes(typeof (LuaFunctionAttribute), false).Length > 0).ToList
                    ();

            Lua.luaL_Reg e;
            var entries = new List<Lua.luaL_Reg>();
            foreach (var methodInfo in methods)
            {
                if (methodInfo.IsStatic)
                {
                    var ca =
                        methodInfo.GetCustomAttributes(typeof (LuaFunctionAttribute), false).SingleOrDefault() as
                        LuaFunctionAttribute;

                    if (ca != null)
                    {
                        e = new Lua.luaL_Reg();
                        e.name = ca.FunctionName;
                        e.func = LuaCallback.Wrap(methodInfo, ca);

                        entries.Add(e);
                    }
                }
            }

            /*e = new Lua.luaL_Reg();
            e.name = "dd";
            e.func = null
            entries.Add(e);*/
            var e2 = entries.ToArray();
            LuaVM.luaL_module(L, libName, e2, 0);
        }

        public static int test(IntPtr intPtr)
        {
            return 0;
        }
    }
}
