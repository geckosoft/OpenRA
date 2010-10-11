using System;
using OpenRA.Traits;
using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Proxies
{
    [LuaObject("Actor")]
    public class ActorProxy : IProxy
    {
        protected Actor Field = null;
        public string ObjectType { get { return "Actor";  } }

        public ActorProxy(Actor actor)
        {
            Field = actor;
        }

        [LuaFunction("getCost", RequireObject = false)]
        public static int GetCost(string actorName)
        {
            if (!Rules.Info.ContainsKey(actorName))
                return 0;

            return Rules.Info[actorName].Traits.Contains<ValuedInfo>()
                       ? Rules.Info[actorName].Traits.Get<ValuedInfo>().Cost
                       : 0;
        }

        [LuaFunction("getCost", RequireObject = true)]
        public static int GetCost(ActorProxy self)
        {
            var actorName = self.ToString();
            if (!Rules.Info.ContainsKey(actorName))
                return 0;

            return Rules.Info[actorName].Traits.Contains<ValuedInfo>()
                       ? Rules.Info[actorName].Traits.Get<ValuedInfo>().Cost
                       : 0;

        }

        [LuaFunction("getName", RequireObject = true)]
        public static string GetName(ActorProxy self)
        {
            return self.Field.Info.Name;
        }

        [LuaFunction("isBuilding", RequireObject = true)]
        public static bool IsBuilding(ActorProxy self)
        {
            return self.Field.HasTrait<Buildable>();
        }

        [LuaFunction("getLocation", RequireObject = true)]
        public static Int2Proxy GetLocation(ActorProxy self)
        {
            return self.Field.Location;
        }


        [LuaFunction("order", RequireObject = true)]
        public static void Order(ActorProxy self, string command, object arg1, object arg2)
        {
            Order order = null;

            if (arg1 == null)
            {
                order = new Order(command, ((Actor)self));
            }
            else if (arg1 as Actor != null)
            {
                order = new Order(command, ((Actor)self), (Actor)arg1);
            }
            else if (arg1 as Int2Proxy != null)
            {
                order = new Order(command, ((Actor)self), (Int2Proxy)arg1);
            }
            else
            {
                return;
            }

            Game.IssueOrder(order);
        }
        public static implicit operator Actor(ActorProxy d)
        {
            return d.Field;
        }

        public static implicit operator ActorProxy(Actor d)
        {
            return new ActorProxy(d);
        }

        public Actor Get()
        {
            return Field;
        }
    }
}
