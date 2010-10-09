using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tao.Lua;

namespace SharpLua.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LuaObjectAttribute : Attribute
    {
        public string Name = "";
        public string TypeName = "";

        public LuaObjectAttribute(string objectName, string createFunction)
        {
            Name = objectName;
            TypeName = createFunction; /* unused! */
        }

        public LuaObjectAttribute(string objectName)
        {
            Name = objectName;
            TypeName = Name;
        }
    }
}
