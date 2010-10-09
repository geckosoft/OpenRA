using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpLua.Attributes;
using Tao.Lua;

namespace SharpLua.Objects
{
    [LuaObject("TestObject", "TestObject")]
    public class TestObject
    {
        [LuaFunction("new", RequireObject = false)]
        public static LuaUserData CreateObject(LuaVM vm)
        {
            return vm.UserData.Create(new TestObject(), "TestObject");
        }

        [LuaFunction("printMe", RequireObject = true)]
        public static void Print(TestObject obj, string text)
        {
            Console.WriteLine("TestObject writes:" + text);
        }

        [LuaFunction("print", RequireObject = true)]
        public static void Print(string text)
        {
            Console.WriteLine(text);
        }
    }
}
