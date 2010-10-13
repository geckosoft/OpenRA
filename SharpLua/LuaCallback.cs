using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SharpLua.Attributes;
using Tao.Lua;

namespace SharpLua
{

    //public static LuaL_openlib(IntPtr L, string libname, )
    public class LuaCallback
    {
        public MethodInfo Method;
        public LuaFunctionAttribute Attribute;
        public Object Instance = null;

        public LuaCallback(MethodInfo method, LuaFunctionAttribute attr)
        {
            Method = method;
            Attribute = attr;

            if (!Method.IsStatic)
                throw new Exception("Expected Method to be static!");
        }

        public LuaCallback(MethodInfo method, LuaFunctionAttribute attr, Object instance)
        {
            Method = method;
            Attribute = attr;
            Instance = instance;

            //if (Instance != null)
            //    throw new Exception("Expected Method to not be static!");
            //if (Instance == null)
            //    throw new Exception("Expected Method to be static!");
        }

        public static Lua.lua_CFunction Wrap(MethodInfo method, LuaFunctionAttribute attr)
        {
            var cb = new LuaCallback(method, attr);

            return new Lua.lua_CFunction(cb.OnCall);
        }



        public static Lua.lua_CFunction Wrap(MethodInfo method, LuaFunctionAttribute attr, Object instance)
        {
            var cb = new LuaCallback(method, attr, instance);

            return new Lua.lua_CFunction(cb.OnCall);
        }

        protected int OnCall(IntPtr L)
        {
            var VM = LuaVM.GetInstance(L);
            IntPtr realL = L;
            if (VM != null)
                realL = VM.L;

            try
            {
                if (VM != null)
                    VM.L = L; /* work around coroutines */   
                int argc = Lua.lua_gettop(L);
                var pars = Method.GetParameters();

                if (argc > pars.Length)
                    throw new Exception("Invalid parameter count. Got " + argc + " expected max " + pars.Length);

                var callingParams = new List<object>();

                int n = 1;

                foreach (var par in pars)
                {

                    // Special case - if it expects a LuaVM, hack in the param ;)
                    // Cannot be specified from lua !
                    if (par.ParameterType == typeof (LuaVM))
                    {
                        callingParams.Add(VM);
                        continue;
                    }

                    if (par.ParameterType == typeof (IntPtr)) // Assume this function wants the Lua pointer
                    {
                        callingParams.Add(L);
                        continue;
                    }


                    if (n == argc + 1)
                    {
                        // Skip additional ones
                        callingParams.Add(null);
                        continue;
                    }
                    if (Lua.lua_isnil(L, n))
                    {
                        callingParams.Add(null);
                    }
                    else if (par.ParameterType == typeof (string))
                    {
                        callingParams.Add(Lua.lua_tostring(L, n));
                    }
                    else if (par.ParameterType == typeof (int))
                    {
                        callingParams.Add(Lua.lua_tointeger(L, n));
                    }
                    else if (par.ParameterType == typeof (double))
                    {
                        callingParams.Add(Lua.lua_tonumber(L, n));
                    }
                    else if (par.ParameterType == typeof (float))
                    {
                        callingParams.Add((float) Lua.lua_tonumber(L, n));
                    }
                    else if (par.ParameterType == typeof (bool))
                    {
                        callingParams.Add((float) Lua.lua_toboolean(L, n));
                    }
                    else if (par.ParameterType == typeof (LuaUserData))
                    {
                        callingParams.Add(VM.UserData.GetEntry(n));
                    }
                    else if (par.ParameterType is object) /* final chance */
                    {
                        callingParams.Add(VM.UserData.GetObject(n));
                    }
                    else
                    {
                        callingParams.Add(Lua.lua_tostring(L, n)); // Shall return null
                    }

                    n++;
                }

                var result = Method.Invoke(Instance, callingParams.ToArray());
                int resCount = 0;

                if (result != null && result.GetType().IsArray)
                {
                    var a = (Array) result;
                    /* push a table */
                    Lua.lua_newtable(L);

                    for (int i = 0; i < a.GetLength(0); i++)
                    {
                        var r = a.GetValue(i);
                        Lua.lua_pushinteger(L, i + 1); /* add the index */
                        PushResult(VM, r); /* add the value */
                        Lua.lua_settable(L, -3);
                    }
                    resCount++;
                }
                else
                {
                    PushResult(VM, result);
                    resCount++;
                }

                if (VM != null)
                    VM.L = realL;

                return resCount; // number of return values
                /* @todo Add support for custom userdata (already possible now - just make sure to implement ILuaObjectProxy so we know what 'type' it is! )*/
            }catch
            {

            }
            if (VM != null)
                VM.L = realL;

            return 0;
        }

        protected void PushResult(LuaVM L, object result)
        {
            if (result is string)
            {
                Lua.lua_pushstring(L, (string)result);
            }
            else if (result is bool)
            {
                if ((bool)result)
                    Lua.lua_pushboolean(L, 1);
                else
                    Lua.lua_pushboolean(L, 0);
            }
            else if (result is int)
            {
                Lua.lua_pushinteger(L, (int)result);
            }
            else if (result is double)
            {
                Lua.lua_pushnumber(L, (double)result);
            }
            else if (result is float)
            {
                Lua.lua_pushnumber(L, (float)result);
            }
            else if (result is LuaUserData)
            {
                var ud = result as LuaUserData;

                if (!ud.Created)
                {
                    ud.Create(L);
                }
            }
            else if (result is ILuaObjectProxy)
            {
                L.UserData.Create(result, ((ILuaObjectProxy)result).ObjectType);
            }
            else if (result is object)
            {
                L.UserData.Create(result);
            }
        }
    }

}
