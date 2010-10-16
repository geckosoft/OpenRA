using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using SharpLua;
using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Proxies
{
    [LuaObject("MiniYaml")]
    public class MiniYamlProxy : IProxy
    {
        protected MiniYaml Field = null;
        public string ObjectType { get { return "MiniYaml"; } }
        protected string FilePath = "";
        protected string FieldName = "";


        public MiniYamlProxy(MiniYaml y)
        {
            Field = y;
        }


        public MiniYamlProxy(MiniYaml y, string fieldName)
        {
            Field = y;
            FieldName = fieldName;
        }

        //

        public MiniYamlProxy(string filePath)
        {
            FilePath = filePath;

            if (!File.Exists(filePath))
                return;

            Field = new MiniYaml(null, MiniYaml.FromFile(Path.GetFullPath(filePath)));
        }

        [LuaFunction("isLoaded", RequireObject = true)]
        public static bool IsValid(MiniYamlProxy self)
        {
            return self.Field != null;
        }

        [LuaFunction("clear", RequireObject = true)]
        public static void Clear(MiniYamlProxy self)
        {
            self.Field = null;
        }


        [LuaFunction("getFields", RequireObject = true)]
        public static MiniYamlProxy[] GetFields(MiniYamlProxy self)
        {
            var y = self.Field;
            if (y == null)
                return null;

            var res = y.NodesDict.ToDictionary(x => x.Key, x => x.Value);

            var result = new List<MiniYamlProxy>();

            foreach (var re in res)
            {
                result.Add(new MiniYamlProxy(re.Value, re.Key));   
            }

            return result.ToArray();
        }

        [LuaFunction("getKey", RequireObject = true)]
        public static string GetKey(MiniYamlProxy self)
        {
            return self.FieldName.Trim();
        }

        [LuaFunction("getValue", RequireObject = true)]
        public static string GetValue(MiniYamlProxy self)
        {
            if (self.Field == null)
                return null;

            if (self.Field.Value == null)
                return "";

            return self.Field.Value.Trim();
        }

        [LuaFunction("open", RequireObject = false)]
        public static LuaUserData CreateObject(LuaVM vm, string path)
        {
            return vm.UserData.Create(new MiniYamlProxy(path), "MiniYaml");
        }
    }
}
