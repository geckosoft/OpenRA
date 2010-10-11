using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.AI.Proxies;
using OpenRA.Mods.RA.AI.Traits;
using OpenRA.Traits;
using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Module
{
    [LuaObject("Engine")]
    public class ModEngine
    {
        public LuaBot Bot = null;
        public ModEngine(LuaBot bot)
        {
            Bot = bot;
        }

        [LuaFunction("exception")]
        public void Exception(string text)
        {
            text = text;
            //throw new Exception(text);

            return;
        }
        [LuaFunction("debug")]
        public void Print(string text)
        {
            Bot.Debug(text);
        }

        [LuaFunction("isBuilding")]
        public bool IsBuilding(string actorName)
        {
            return Bot.Queue.IsBuilding(actorName);
        }

        [LuaFunction("canProduce")]
        public bool CanProduce(string actorName)
        {
            var qs = Bot.GetQueues();

            foreach (var queue in qs)
            {
                if (queue.BuildableItems().Where(a => a.Name.ToLower() == actorName.ToLower()).Count() > 0)
                    return true;
            }

            return false;
        }

        [LuaFunction("getMoney")]
        public int GetMoney()
        {
            return Bot.GetMoney();
        }


        [LuaFunction("getPowerAvailable")]
        public int GetPowerAvailable()
        {
            return Bot.GetPowerUnused();
        }

        [LuaFunction("getPowerUsed")]
        public int GetPowerUsed()
        {
            return Bot.GetPowerUsed();
        }

        [LuaFunction("getPower")]
        public int GetTotalPower()
        {
            return Bot.GetPowerProvided();
        }

        [LuaFunction("getProductionType")]
        public string GetProductionType(string actorName)
        {
            if (!Rules.Info.ContainsKey(actorName))
                return ""; /* invalid */

            return Bot.GetBuildableType(Rules.Info[actorName]);
        }

        [LuaFunction("getBuildings")]
        public ActorProxy[] GetBuildings(string optionalType)
        {
            IEnumerable<Actor> myBuildings;

            if (optionalType == null)
                myBuildings = Bot.Player.World.Actors.Where(a => a != Bot.Player.PlayerActor && a.Owner == Bot.Player && (a.HasTrait<BaseBuilding>() || a.HasTrait<Buildable>()));
            else
                myBuildings = Bot.Player.World.Actors.Where(a => a != Bot.Player.PlayerActor && Bot.GetBuildableType(a.Info) == optionalType && a.Owner == Bot.Player && (a.HasTrait<BaseBuilding>() || a.HasTrait<Buildable>()));

            var actors = myBuildings.ToArray();

            var res = new List<ActorProxy>();

            foreach (var a in actors)
            {
                res.Add(a);
            }

            return res.ToArray();
        }

        /*
        [LuaFunction("countBuildings")]
        public int CountAc(string optionalType)
        {
            IEnumerable<Actor> myBuildings;

            if (optionalType == null)
                myBuildings = Bot.Player.World.Actors.Where(a => a != Bot.Player.PlayerActor && a.Owner == Bot.Player && (a.HasTrait<BaseBuilding>() || a.HasTrait<Buildable>()));
            else
                myBuildings = Bot.Player.World.Actors.Where(a => a != Bot.Player.PlayerActor && Bot.GetBuildableType(a.Info) == optionalType && a.Owner == Bot.Player && (a.HasTrait<BaseBuilding>() || a.HasTrait<Buildable>()));

            var actors = myBuildings.ToArray();

            return actors.Length;
        }
        */


        [LuaFunction("getActors", RequireObject = false)]
        public ActorProxy[] GetActors(string actorName)
        {
            if (!Rules.Info.ContainsKey(actorName))
                return new ActorProxy[0];

            var res = new List<ActorProxy>();

            var acs = Bot.GetOwnedActors().Where(a => a.Info.Name == actorName);

            foreach (var actor in acs)
            {
                res.Add(actor);
            }

            return res.ToArray();
        }
               

        [LuaFunction("countActors", RequireObject = false)]
        public int CountActors(string actorName)
        {
            if (!Rules.Info.ContainsKey(actorName))
                return 0;

            return Bot.GetOwnedActors().Where(a => a.Info.Name == actorName).Count();
        }

        [LuaFunction("countBuildings")]
        public int CountBuildings(string optionalType)
        {
            IEnumerable<Actor> myBuildings;

            if (optionalType == null)
                myBuildings = Bot.Player.World.Actors.Where(a => a != Bot.Player.PlayerActor && a.Owner == Bot.Player && (a.HasTrait<BaseBuilding>() || a.HasTrait<Buildable>()));
            else
                myBuildings = Bot.Player.World.Actors.Where(a => a != Bot.Player.PlayerActor && Bot.GetBuildableType(a.Info) == optionalType && a.Owner == Bot.Player && (a.HasTrait<BaseBuilding>() || a.HasTrait<Buildable>()));

            var actors = myBuildings.ToArray();

            return actors.Length;
        }

        [LuaFunction("getPlayer")]
        public PlayerProxy GetPlayer()
        {
            return Bot.Player;
        }

        [LuaFunction("deployMcv")]
        public void UnpackMcv()
        {
            Bot.DeployMcv();
        }

        [LuaFunction("findBuildLocation")]
        public Int2Proxy ChooseBuildLocation(ProductionItemProxy item, int maxBaseDistance, Int2Proxy baseCenter)
        {
            return (Int2Proxy)Bot.Proxy.Get(Bot.ChooseBuildLocation(item, maxBaseDistance, baseCenter));
        }


        [LuaFunction("placeBuilding")]
        public bool PlaceBuilding(ProductionItemProxy currentBuilding, Int2Proxy location)
        {
            if (location == null)
                return false;

            Bot.Debug("Placing {0}".F(((ProductionItem)currentBuilding).Item));

            Game.IssueOrder(new Order("PlaceBuilding", Bot.Player.PlayerActor, (int2)location,
                                      ((ProductionItem)currentBuilding).Item));
            return true;
        }

        [LuaFunction("getCYLocation")]
        public Int2Proxy GetCYLocation()
        {
            var r = Bot.GetConstructionYardLocation();
            if (r == null)
                return null;

            return new Int2Proxy(r);
        }

        [LuaFunction("getRallyLocation")]
        public Int2Proxy ChooseRallyLocationNear(Int2Proxy location)
        {
            var r = Bot.ChooseRallyLocationNear(location);

            return new Int2Proxy(r);
        }

        [LuaFunction("getEnemies")]
        public PlayerProxy[] GetEnemies()
        {
            var res = new List<PlayerProxy>();

            foreach (var q in Bot.Player.World.players.Values)
            {
                if (q.Stances.ContainsKey(Bot.Player) && q.Stances[Bot.Player] == Stance.Enemy)
                {
                    res.Add((PlayerProxy)Bot.Proxy.Get(q));
                }
            }

            return res.ToArray();
        }

        [LuaFunction("clearProductionQueue")]
        public void ClearQueueProduction(string optionalBuildType)
        {
            if (optionalBuildType == null)
                Bot.Queue.Clear();
            else
                Bot.Queue.Clear(optionalBuildType);
        }

        [LuaFunction("queueProduction")]
        public AutoProductionQueue.Entry QueueProduction(string actor)
        {
            return Bot.QueueProduction(actor);
        }


        [LuaFunction("countPendingQueue")]
        public int GetPendingQueue(string buildType)
        {
            return Bot.Queue.Count(buildType);
        }

        [LuaFunction("assignBrain")]
        public bool AssignBrain(ActorProxy actor, string brain)
        {
            var b = actor.Get().TraitOrDefault<LuaBrain>();

            if (b == null)
                return false;
            b.Assign(Bot, "Brains." + brain);
            return true;
        }
    }
}
