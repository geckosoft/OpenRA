using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI.Traits
{
    public class LuaTaskForceInfo : ITraitInfo
    {
        /// <summary>
        /// The Yaml used to load this info instance
        /// </summary>
        [FieldLoader.LoadUsing("StoreYaml")]
        public MiniYaml Yaml = null;

        public virtual object Create(ActorInitializer init)
        {
            return new LuaTaskForce(init.self, this);
        }

        static object StoreYaml(MiniYaml y)
        {
            return y;
        }
    }

    /// <summary>
    /// The all knowing trait
    /// </summary>
    public class LuaTaskForce
    {
        public Actor Self = null;
        public LuaTaskForceInfo Info = null;
        public Dictionary<string, int> Options = new Dictionary<string, int>();
        public Dictionary<string, string> States = new Dictionary<string, string>();
        public LuaTaskForce(Actor self, ITraitInfo info)
        {
            Self = self;
            Info = (LuaTaskForceInfo)info;
        }

        public void SetOption(string option, int val)
        {
            if (Options.ContainsKey(option))
                Options.Remove(option);

            Options.Add(option, val);
        }

        public void SetState(string option, string val)
        {
            if (States.ContainsKey(option))
                Options.Remove(option);

            States.Add(option, val);
        }

        public int GetOption(string option)
        {
            if (!Options.ContainsKey(option))
                return 0;

            return Options[option];
        }
        public string GetState(string option)
        {
            if (!States.ContainsKey(option))
                return "";

            return States[option];
        }
    }
}
