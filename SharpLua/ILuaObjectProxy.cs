using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua
{
    public interface ILuaObjectProxy
    {
        string ObjectType { get; }
    }
}
