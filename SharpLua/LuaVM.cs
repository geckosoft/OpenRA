using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharpLua.Attributes;
using Tao.Lua;

namespace SharpLua
{
    public class LuaVM
    {
        public IntPtr L;
        public LuaUserDataManager UserData = null;
        public List<Lua.lua_CFunction> CallbackReferences = new List<Lua.lua_CFunction>();
        public static Lua.lua_CFunction GcFunction = new Lua.lua_CFunction(OnGc);

        public void RegisterObject(string objectType, string tableName, Lua.luaL_Reg[] methods, Lua.luaL_Reg[] functions)
        {
            Lua.luaL_newmetatable(L, "CSharpObject." + objectType);
            Lua.lua_pushstring(L, "__index");
            Lua.lua_pushvalue(L, -2);  /* pushes the metatable */
            Lua.lua_settable(L, -3);  /* metatable.__index = metatable */


            var gc = new Lua.luaL_Reg();
            gc.name = "__gc";
            gc.func = GcFunction;

            var mt = methods.ToList();
            mt.Add(gc); /* hack in our __gc automagic */
            methods = mt.ToArray();

            // Register the methods for referenced objects
            luaL_module(L, null, methods, 0);

            // Register the 'static' functions 
            luaL_module(L, tableName, functions, 0);

            return;
        }
        
        static int OnGc(IntPtr L)
        {
            var vm = GetInstance(L);
            if (vm != null)
            {
                var entry = vm.UserData.GetEntry(1);

                vm.UserData.Remove(entry.Index);
            }

            return 0;
        }

        public void RegisterObject(string objectType, string tableName, Lua.luaL_Reg[] methods, Lua.lua_CFunction createFunction)
        {
            Lua.luaL_newmetatable(L, "CSharpObject." + objectType);
            Lua.lua_pushstring(L, "__index");
            Lua.lua_pushvalue(L, -2);  /* pushes the metatable */
            Lua.lua_settable(L, -3);  /* metatable.__index = metatable */
            var functions = new List<Lua.luaL_Reg>();
            var ent = new Lua.luaL_Reg();
            ent.name = "new";
            ent.func = createFunction;
            functions.Add(ent);



            var gc = new Lua.luaL_Reg();
            gc.name = "__gc";
            gc.func = GcFunction;

            var mt = methods.ToList();
            mt.Add(gc); /* hack in our __gc automagic */
            methods = mt.ToArray();

            // Register the methods for referenced objects
            luaL_module(L, null, methods, 0);

            // Register the 'static' functions 
            luaL_module(L, tableName, functions.ToArray(), 0);

            return;
        }
        /*
        public Object GetUserObject(string objectType)
        {
            var e = GetUserData(objectType);
            if (e == null)
                return null;

            return e.Object;
        }

        public LuaUserData GetUserData(string objectType)
        {
            var ot = "CSharpObject." + objectType;
            var pt = Lua.luaL_checkudata(L, 1, ot);
            var err = (pt == IntPtr.Zero) ? 0 : 1;

            Lua.luaL_argcheck(L, err, 1, ot);

            if (err == 0)
                return null;

            // got some data !
            return UserData.GetEntry(pt);
        }
        *//*
        public void RegisterFunctions(Type t)
        {
            LuaAssembly.RegisterFunctions(L, t);
        }

        public void RegisterFunctions(Object t)
        {
            LuaAssembly.RegisterFunctions(L, t);
        }
        */
        public static implicit operator IntPtr(LuaVM vm)
        {
            return vm.L;
        }

        public void CheckStatus(int status)
        {
            ReportErrors(L, status);
        }

        protected object CallFunction (object[] vars, Type returnType)
        {
            // Generate arguments
            foreach (var obj in vars)
            {
                if (obj == null)
                {
                    Lua.lua_pushnil(L);
                }else if (obj is string)
                {
                    Lua.lua_pushstring(L, (string)obj);
                }
                else if ((obj is int) || (obj is uint))
                {
                    Lua.lua_pushinteger(L, (int)obj);
                }
                else if ((obj is double) || (obj is float))
                {
                    Lua.lua_pushnumber(L, (double)obj);
                }
                else if (obj is Lua.lua_CFunction)
                {
                    /* please.. dont use this except in RARE cases! */
                    /* allow a nicer way to specify 'callbacks' */
                    Lua.lua_pushcfunction(L, (Lua.lua_CFunction)obj);
                }
                else if (obj is bool)
                {
                    if ((bool)obj)
                        Lua.lua_pushboolean(L, 1);
                    else
                        Lua.lua_pushboolean(L, 0);
                }
                else if (obj is LuaUserData) /* wrap it up, make sure to use CreateDelayed to create such instances! */
                {
                    var ud = (LuaUserData) obj;
                    if (ud.Object == null)
                        Lua.lua_pushnil(L);
                    else
                    {
                        UserData.Create(ud.Object, ud.ObjectType);
                    }
                }
                else if (obj is object) /* wrap it up */
                {
                    UserData.Create(obj);
                }
            }

            var s = Lua.lua_pcall(L, vars.Length, 1, 0); /* only accept one return value for now ! */
            if (s != 0)
            {
                ReportErrors(L, s); // throws exception
            }

            var res = GetCallResult(returnType);
            Lua.lua_pop(L, 1);  /* pop returned value */

            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="returnType"></param>
        /// <returns>The accepted return type (string, int, double, bool, ...)</returns>
        private object GetCallResult(Type returnType)
        {
            // @todo Add support for returning tables!
            if (Lua.lua_isnil(L, -1))
            {
                return null;
            }
            else if (returnType == typeof(bool) && Lua.lua_isboolean(L, -1))
            {
                return (Lua.lua_toboolean(L, -1) > 0);
            }
            else if (returnType == typeof(string) && Lua.lua_isstring(L, 1) > 0)
            {
                return Lua.lua_tostring(L, -1);
            }
            else if (returnType == typeof(double) && Lua.lua_isnumber(L, 1) > 0)
            {
                return Lua.lua_tonumber(L, -1);
            }
            else if (returnType == typeof(float) && Lua.lua_isnumber(L, 1) > 0)
            {
                return (long)Lua.lua_tonumber(L, -1);
            }
            else if (returnType == typeof(long) && Lua.lua_isnumber(L, 1) > 0)
            {
                return (long)Lua.lua_tonumber(L, -1);
            }
            else if (returnType == typeof(int) && Lua.lua_isnumber(L, -1) > 0)
            {
                return (int)Lua.lua_tonumber(L, -1);
            }
            else if (returnType == typeof(LuaUserData) && Lua.lua_isuserdata(L, -1) > 0)
            {
                var ptr = Lua.lua_touserdata(L, -1);

                return UserData.GetEntry(ptr);
            }
            else if (Lua.lua_isuserdata(L, -1) > 0)
            {
                var ptr = Lua.lua_touserdata(L, -1);
                var obj = UserData.GetObject(ptr);
                if (obj.GetType() != returnType)
                    return null;
                else
                    return obj;
            }else
            {
                return null;
            }
        }

        public object CallFunction(string method, object[] vars, Type returnType)
        {
            var depth = 1;
            var p = method.Split('.');

            Lua.lua_getglobal(L, p[0]);
            var t = Lua.lua_type(L, -1);

            if (p.Length == 1)
            {
                if (t != Lua.LUA_TFUNCTION)
                {
                    Lua.lua_pop(L, depth);
                    return null;
                }

                return CallFunction(vars, returnType); // No need to clean up
                
            }

            if (t != Lua.LUA_TTABLE)
            {
                Lua.lua_pop(L, depth);
                return null;
            }

            for (int i = 1; i < p.Length - 1; i++)
            {
                depth++;

                Lua.lua_getfield(L, -1, p[i]);
                t = Lua.lua_type(L, -1);

                if (t != Lua.LUA_TTABLE)
                {
                    Lua.lua_pop(L, depth);
                    return null;
                }
            }

            Lua.lua_getfield(L, -1, p[p.Length - 1]);

            if (!Lua.lua_isfunction(L, -1))
            {
                Lua.lua_pop(L, depth);
                return null;
            }

            // Clean up the stack
            for (int i = 0; i < depth-1; i++)
            {
                Lua.lua_remove(L, -2);
            }

            return CallFunction(vars, returnType); ;
        }

        public bool HasFunction(string method)
        {
            var depth = 1;
            var p = method.Split('.');

            Lua.lua_getglobal(L, p[0]);
            var t = Lua.lua_type(L, -1);


            if (p.Length == 1)
            {
                if (t != Lua.LUA_TFUNCTION)
                {
                    Lua.lua_pop(L, depth);
                    return false;
                }
                return true;
            }

            if (t != Lua.LUA_TTABLE)
            {
                Lua.lua_pop(L, depth);
                return false;
            }

            for (int i = 1;i < p.Length-1; i ++)
            {
                depth++;

                Lua.lua_getfield(L, -1, p[i]);
                t = Lua.lua_type(L, -1);

                if (t != Lua.LUA_TTABLE)
                {
                    Lua.lua_pop(L, depth);
                    return false;
                }
            }

            Lua.lua_getfield(L, -1, p[p.Length - 1]);

            if (!Lua.lua_isfunction(L, -1))
            {
                Lua.lua_pop(L, depth);
                return false;
            }
            Lua.lua_pop(L, depth);
            return true;
        }

        protected void ReportErrors(IntPtr L, int status)
        {
            if (status != 0)
            {
                var msg = Lua.lua_tostring(L, -1);
                Console.WriteLine("-- " + msg);
                Lua.lua_pop(L, 1); // remove error message

                throw new Exception("Lua error:" + msg);
            }
        }

        private static Dictionary<Int32, LuaVM> Instances = new Dictionary<Int32, LuaVM>();

        public void Open()
        {
            lock (Instances)
            {
                L = Lua.luaL_newstate();
                if (Instances.ContainsKey(L.ToInt32()))
                    Instances.Remove(L.ToInt32());

                CallbackReferences.Clear();

                UserData = new LuaUserDataManager(this);

                Instances.Add(L.ToInt32(), this);
            }
        }

        public static LuaVM GetInstance(IntPtr L)
        {
            lock (Instances)
            {
                if (!Instances.ContainsKey(L.ToInt32()))
                    return null;
                return Instances[L.ToInt32()];
            }
        }



        public static LuaVM LinkInstance(IntPtr L, IntPtr newL)
        {
            lock (Instances)
            {
                if (!Instances.ContainsKey(L.ToInt32()))
                    return null;

                if (Instances.ContainsKey(newL.ToInt32()))
                    return null;

                Instances.Add(newL.ToInt32(), Instances[L.ToInt32()]); // Link them together

                return Instances[L.ToInt32()];
            }
        }

        public static LuaVM GetInstance(int L)
        {
            lock (Instances)
            {
                if (!Instances.ContainsKey(L))
                    return null;
                return Instances[L];
            }
        }

        public void OpenLibs()
        {
            Lua.luaL_openlibs(L);
        }

        public void Close()
        {
            lock (Instances)
            {


                //report_errors(L, s);
                Lua.lua_close(L);
                UserData.Clear();
                CallbackReferences.Clear();
                if (Instances.ContainsKey(L.ToInt32()))
                    Instances.Remove(L.ToInt32());
            }
        }

        public static void RemoveInstance(IntPtr L)
        {
            if (Instances.ContainsKey(L.ToInt32()))
                Instances.Remove(L.ToInt32());
        }
        protected static int echo(IntPtr L)
        {
            int argc = Lua.lua_gettop(L);

            for (int n = 1; n <= argc; n++)
            {
                Console.WriteLine(Lua.lua_tostring(L, n));
            }

            //Lua.lua_pushnumber(L, 123); // return value
            return 0; // number of return values
        }

        public int Run(string path)
        {
            int s = Lua.luaL_loadfile(L, path);

            if (s == 0)
            {
                // execute Lua program
                s = Lua.lua_pcall(L, 0, Lua.LUA_MULTRET, 0);
            }

            return s;
        }


        public static void getfield(IntPtr L, string name)
        {
            Lua.lua_pushvalue(L, Lua.LUA_GLOBALSINDEX);
            foreach (var s in name.Split('.'))
            {
                Lua.lua_pushstring(L, name);
                Lua.lua_gettable(L, -2);
                Lua.lua_remove(L, -2);
                if (Lua.lua_isnil(L, -1)) return;
            }
        }

        public static void setfield(IntPtr L, string name)
        {
            Lua.lua_pushvalue(L, Lua.LUA_GLOBALSINDEX);
            var p = name.Split('.');

            for (int i = 0; i < p.Length - 1; i++)
            {
                var s = p[i];
                Lua.lua_pushstring(L, name);
                Lua.lua_gettable(L, -2);

                if (Lua.lua_isnil(L, -1))
                {
                    Lua.lua_pop(L, 1);
                    Lua.lua_newtable(L);
                    Lua.lua_pushstring(L, s);
                    Lua.lua_pushvalue(L, -2);
                    Lua.lua_settable(L, -4);

                }
                Lua.lua_remove(L, -2);
            }
            Lua.lua_pushstring(L, p[p.Length - 1]);
            Lua.lua_pushvalue(L, -3);
            Lua.lua_settable(L, -3);
            Lua.lua_pop(L, 2);
        }

        public static void luaL_module(IntPtr L, string libname, Lua.luaL_Reg[] regs, int nup)
        {
            if (!string.IsNullOrEmpty(libname))
            {
                getfield(L, libname);  /* check whether lib already exists */
                if (Lua.lua_isnil(L, -1))
                {
                    Lua.lua_pop(L, 1); /* get rid of nil */
                    Lua.lua_newtable(L); /* create namespace for lib */
                    getfield(L, "package.loaded"); /* get package.loaded table or create it */
                    if (Lua.lua_isnil(L, -1))
                    {
                        Lua.lua_pop(L, 1);
                        Lua.lua_newtable(L);
                        Lua.lua_pushvalue(L, -1);
                        setfield(L, "package.loaded");
                    }
                    else if (!Lua.lua_istable(L, -1))
                        Lua.lua_error(L); // @todo fixme , "name conflict for library `%s'", libname);

                    Lua.lua_pushstring(L, libname);
                    Lua.lua_pushvalue(L, -3);
                    Lua.lua_settable(L, -3); /* store namespace in package.loaded table */
                    Lua.lua_pop(L, 1); /* get rid of package.loaded table */
                    Lua.lua_pushvalue(L, -1);
                    setfield(L, libname);  /* store namespace it in globals table */
                }
                Lua.lua_insert(L, -(nup + 1));
            }

            for (int i = 0; i < regs.Length; i++)
            {
                var r = regs[i];
                Lua.lua_pushstring(L, r.name);
                for (int ix = 0; ix < nup; ix++)  /* copy upvalues to the top */
                    Lua.lua_pushvalue(L, -(nup + 1));
                Lua.lua_pushcclosure(L, r.func, nup);
                Lua.lua_settable(L, -(nup + 3));
            }
            Lua.lua_pop(L, nup);  /* remove upvalues */
        }

        public void RegisterObject(Object o)
        {
            string objectType = "";
            string tableName = "";
            var t = o.GetType();

            var staticFunctions = new List<Lua.luaL_Reg>();
            var objectFunctions = new List<Lua.luaL_Reg>();


            // Get the object name
            var objInfo = (LuaObjectAttribute)t.GetCustomAttributes(typeof(LuaObjectAttribute), false).SingleOrDefault();

            if (objInfo == null)
            {
                objectType = "" + t.FullName.GetHashCode();
                tableName = t.Name;
            }
            else
            {
                objectType = objInfo.TypeName;
                tableName = objInfo.Name;
            }

            var functions =
                t.GetMethods().Where(
                    a =>
                    a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).Length > 0 &&
                    ((LuaFunctionAttribute)
                     a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault()).RequireObject ==
                    false);


            var methods =
                t.GetMethods().Where(
                    a =>
                    a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).Length > 0 && (
                    ((LuaFunctionAttribute)
                     a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault()).RequireObject ==
                    true ||
                    ((LuaFunctionAttribute)
                     a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault()).ForceMethod ==
                    true));

            foreach (var mi in functions)
            {
                var ca =
                    mi.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault() as
                    LuaFunctionAttribute;
                var f = LuaCallback.Wrap(mi, ca, o);
                var e = new Lua.luaL_Reg();
                e.func = f;
                e.name = ca.FunctionName;
                staticFunctions.Add(e);
                CallbackReferences.Add(f);
            }

            foreach (var mi in methods)
            {
                var ca =
                    mi.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault() as
                    LuaFunctionAttribute;
                var f = LuaCallback.Wrap(mi, ca, o);
                var e = new Lua.luaL_Reg();
                e.func = f;
                e.name = ca.FunctionName;
                objectFunctions.Add(e);
                CallbackReferences.Add(f);
            }

            RegisterObject(objectType, tableName, objectFunctions.ToArray(), staticFunctions.ToArray());
        }

        public void RegisterObject(Type t)
        {
            string objectType = "";
            string tableName = "";
            var staticFunctions = new List<Lua.luaL_Reg>();
            var objectFunctions = new List<Lua.luaL_Reg>();


            // Get the object name
            var objInfo = (LuaObjectAttribute)t.GetCustomAttributes(typeof (LuaObjectAttribute), false).SingleOrDefault();

            if (objInfo == null)
            {
                objectType = "" + t.FullName.GetHashCode();
                tableName = t.Name;
            }else
            {
                objectType = objInfo.TypeName;
                tableName = objInfo.Name;
            }

            var functions =
                t.GetMethods().Where(
                    a =>
                    a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).Length > 0 &&
                    ((LuaFunctionAttribute)
                     a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault()).RequireObject ==
                    false);


            var methods =
                t.GetMethods().Where(
                    a =>
                    a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).Length > 0 && (
                    ((LuaFunctionAttribute)
                     a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault()).RequireObject ==
                    true ||
                    ((LuaFunctionAttribute)
                     a.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault()).ForceMethod ==
                    true));

            foreach (var mi in functions)
            {
                var ca =
                    mi.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault() as
                    LuaFunctionAttribute;
                var f = LuaCallback.Wrap(mi, ca);
                var e = new Lua.luaL_Reg();
                e.func = f;
                e.name = ca.FunctionName;
                staticFunctions.Add(e);
                CallbackReferences.Add(f);
            }


            foreach (var mi in methods)
            {
                var ca =
                    mi.GetCustomAttributes(typeof(LuaFunctionAttribute), false).SingleOrDefault() as
                    LuaFunctionAttribute;
                var f = LuaCallback.Wrap(mi, ca);
                var e = new Lua.luaL_Reg();
                e.func = f;
                e.name = ca.FunctionName;
                objectFunctions.Add(e);
                CallbackReferences.Add(f);
            }

            RegisterObject(objectType, tableName, objectFunctions.ToArray(), staticFunctions.ToArray());
        }
    }
}
