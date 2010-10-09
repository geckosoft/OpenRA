using System;
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

        [LuaFunction("getName", RequireObject = true)]
        public static string GetName(ActorProxy self)
        {
            return self.Field.Info.Name;
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
