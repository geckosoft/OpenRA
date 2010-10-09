using System;

namespace SharpLua.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
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
