using System.Linq;
using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Proxies
{
    [LuaObject("Player")]
    public class PlayerProxy : IProxy
    {
        protected Player Field = null;
        public string ObjectType { get { return "Player"; } }

        public PlayerProxy(Player field)
        {
            Field = field;
        }

        [LuaFunction("getName", RequireObject = true)]
        public static string GetName(PlayerProxy self)
        {
            return self.Field.PlayerName;
        }

        [LuaFunction("getId", RequireObject = true)]
        public static int GetId(PlayerProxy self)
        {
            return self.Field.ClientIndex;
        }


        [LuaFunction("getActor", RequireObject = true)]
        public static Actor GetActor(PlayerProxy self)
        {
            return self.Field.PlayerActor;
        }

        [LuaFunction("countActors", RequireObject = true)]
        public static int CountActors(PlayerProxy self, string actorName)
        {
            if (actorName == null)
                return self.Field.World.Queries.OwnedBy[self.Field].ToArray().Length;

            return self.Field.World.Queries.OwnedBy[self.Field].Where(a => a.Info.Name == actorName).ToArray().Length;
        }

        [LuaFunction("order", RequireObject = true)]
        public static void Order(PlayerProxy self, string command, object arg1, object arg2)
        {
            Order order = null;

            if (arg1 == null)
            {
                order = new Order(command, ((Player) self).PlayerActor);
            }else if (arg1 is Actor)
            {
                order = new Order(command, ((Player)self).PlayerActor, (Actor)arg1);
            }else
            {
                return;
            }

            Game.IssueOrder(order);
        }

        public static implicit operator Player(PlayerProxy d)
        {
            return d.Field;
        }

        public static implicit operator PlayerProxy(Player d)
        {
            return new PlayerProxy(d);
        }
    }
}
