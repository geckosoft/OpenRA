using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.AI.Traits;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Proxies
{
    [LuaObject("Actor")]
    public class ActorProxy : IProxy
    {
        public LuaBot Bot = null;
        public ActorProxy(LuaBot bot)
        {
            Bot = bot;
        }

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

        // Get<BuildingInfo>().Capturable

        [LuaFunction("canCapture", RequireObject = true)]
        public static bool CanCapture(ActorProxy self)
        {
            var b = self.Field.TraitOrDefault<Building>();
            if ( b== null)
                return false;

            return b.Info.Capturable;
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

        [LuaFunction("getOwner", RequireObject = true)]
        public static PlayerProxy GetOwner(ActorProxy self)
        {
            return self.Field.Owner;
        }

        [LuaFunction("getActorsNearby", RequireObject = true)]
        public ActorProxy[] GetActorsNearby(ActorProxy self, int maxDistance)
        {
            var inRange = self.Field.World.FindUnitsInCircle(self.Field.CenterLocation, Game.CellSize * maxDistance);

            var actors = inRange
                .Where(a => a.Owner != null && !a.Destroyed)
                .Where(a => !a.HasTrait<Cloak>() || a.Trait<Cloak>().IsVisible(a, self.Field.Owner))
                .OrderBy(a => (a.Location - self.Field.Location).LengthSquared).ToArray();

            var acs = new List<ActorProxy>();

            foreach (var actor in actors)
            {
                acs.Add(actor);
            }

            return acs.ToArray();
        }

        [LuaFunction("move", RequireObject = true)]
        public bool Move(ActorProxy self, Int2Proxy target)
        {
            if (target == null)
                return false;
            return Bot.TryToMove(self, target);
        }

        [LuaFunction("isBuilding", RequireObject = true)]
        public static bool IsBuilding(ActorProxy self)
        {
            return self.Field.HasTrait<Buildable>();
        }

        [LuaFunction("inWorld", RequireObject = true)]
        public static bool InWorld(ActorProxy self)
        {
            return self.Field.IsInWorld;
        }

        [LuaFunction("isDestroyed", RequireObject = true)]
        public static bool IsDestroyed(ActorProxy self)
        {
            return self.Field.Destroyed;
        }


        [LuaFunction("canAttack", RequireObject = true)]
        public static bool CanAttack(ActorProxy self, ActorProxy target)
        {
            var attack = self.Field.Trait<AttackBase>();
            if (attack == null)
                return false;

            bool res = attack.HasAnyValidWeapons(Target.FromActor(target));
            if (res)
                res = res;
            // TODO add check for cloaked units?
            return res;
        }

        [LuaFunction("isIdle", RequireObject = true)]
        public static bool IsIdle(ActorProxy self)
        {
            return (self.Field.IsIdle || self.Field.GetCurrentActivity() is Activities.IdleAnimation);
        }

        [LuaFunction("isMoving", RequireObject = true)]
        public static bool IsMoving(ActorProxy self)
        {
            return (self.Field.GetCurrentActivity() != null && (self.Field.GetCurrentActivity() is Turn || self.Field.GetCurrentActivity() is Move || self.Field.TraitOrDefault<Mobile>().IsMoving ));
        }

        [LuaFunction("getLocation", RequireObject = true)]
        public static Int2Proxy GetLocation(ActorProxy self)
        {
            return self.Field.Location;
        }


        [LuaFunction("setState", RequireObject = true)]
        public static bool SetTaskState(ActorProxy actor, string option, string val)
        {
            var b = actor.Get().TraitOrDefault<LuaTaskForce>();

            if (b == null)
                return false;

            b.SetState(option, val);
            return true;
        }

        [LuaFunction("getState", RequireObject = true)]
        public static string GetTaskState(ActorProxy actor, string option)
        {
            var b = actor.Get().TraitOrDefault<LuaTaskForce>();

            if (b == null)
                return "";

            return b.GetState(option);
        }

        [LuaFunction("setOption", RequireObject = true)]
        public static bool SetTaskOption(ActorProxy actor, string option, int val)
        {
            var b = actor.Get().TraitOrDefault<LuaTaskForce>();

            if (b == null)
                return false;

            b.SetOption(option, val);
            return true;
        }

        [LuaFunction("getOption", RequireObject = true)]
        public static  int GetTaskOption(ActorProxy actor, string option, int def)
        {
            var b = actor.Get().TraitOrDefault<LuaTaskForce>();

            if (b == null)
                return def;

            return b.GetOption(option);
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
            else if (arg1 as ActorProxy != null)
            {
                order = new Order(command, ((Actor)self), ((ActorProxy)arg1).Field);
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
