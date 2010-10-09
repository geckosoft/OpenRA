using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tao.Lua;

namespace SharpLua
{
    /// <summary>
    /// LuaUserDataManager makes it easier to work with 'userdata'
    /// </summary>
    public class LuaUserDataManager
    {
        public LuaVM VM = null;

        public Dictionary<ulong, LuaUserData> Entries= new Dictionary<ulong, LuaUserData>();

        protected ulong CurrentID = 0;

        public LuaUserDataManager(LuaVM vm)
        {
            VM = vm;
        }

        protected ulong GenerateId()
        {
            lock (this)
            {
                CurrentID++;

                return CurrentID;
            }
        }

        public void Remove(ulong entryId)
        {
            lock (this)
            {
                if (Entries.ContainsKey(entryId))
                    Entries.Remove(entryId); /* free up */
            }
        }
        public void Clear()
        {
            lock (this)
            {
                Entries.Clear();
            }
        }

        /// <summary>
        /// Created a LuaUserData instance but does NOT create (insert data into lua) just yet!
        /// </summary>
        public LuaUserData CreateVar(Object obj, string objectType)
        {
            lock (this)
            {
                var ot = objectType;
                var entry = Register(obj);
                entry.ObjectType = ot;

                //entry.Create(VM);
                return entry;
            }
        }

        /// <summary>
        /// Created a LuaUserData instance but does NOT create (insert data into lua) just yet!
        /// </summary>
        public LuaUserData CreateVar(Object obj)
        {
            lock (this)
            {
                var entry = Register(obj);
                //entry.Create(VM);
                return entry;
            }
        }
        public LuaUserData Create(Object obj, string objectType)
        {
            lock (this)
            {
                var ot = "CSharpObject." + objectType;
                var entry = Register(obj);
                entry.ObjectType = ot;

                entry.Create(VM);
                return entry;
            }
        }


        public LuaUserData Create(Object obj)
        {
            lock (this)
            {
                var entry = Register(obj);
                entry.Create(VM);
                return entry;
            }
        }

        public LuaUserData Register(Object obj)
        {
            lock (this)
            {
                var index = GenerateId();
                var de = new LuaUserData(obj, index);
                Entries.Add(index, de);

                return de;
            }
        }

        public object GetObject(int i)
        {
            var obj = GetEntry(i);
            if (obj == null)
                return null;

            return obj.Object;
        }

        public object GetObject(IntPtr entry)
        {
            var obj = GetEntry(entry);
            if (obj  == null)
                return null;

            return obj.Object;
        }

        public LuaUserData GetEntry(IntPtr entry)
        {
            try
            {
                var data = new byte[8];

                Marshal.Copy(entry, data, 0, 8);

                ulong index = BitConverter.ToUInt64(data, 0);

                if (!Entries.ContainsKey(index))
                    return null;
                else
                {
                    return Entries[index];
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public LuaUserData GetEntry(int i, string objectType)
        {
            if (i < 0)
                throw new Exception("Invalid entry id. Must be > 0");
            
            var ot = "CSharpObject." + objectType;

            var entry = Lua.luaL_checkudata(VM, i, ot);
            if (entry == IntPtr.Zero)
                return null; /* not found */

            return GetEntry(entry);
        }

        public LuaUserData GetEntry(int i)
        {
            if (i < 0)
                throw new Exception("Invalid entry id. Must be > 0");

            var entry = Lua.lua_touserdata(VM, i);
            try
            {

                var data = new byte[8];

                Marshal.Copy(entry, data, 0, 8);

                ulong index = BitConverter.ToUInt64(data, 0);

                if (!Entries.ContainsKey(index))
                    return null;
                else
                {
                    return Entries[index];
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}