using System;

namespace SharpLua.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LuaFunctionAttribute : Attribute
    {
        public string FunctionName = "";

        public LuaFunctionAttribute(string functionName)
        {
            FunctionName = functionName;
        }

        public bool RequireObject = false;
        public bool ForceMethod = false;
    }
}
