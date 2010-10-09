using System.Collections.Generic;
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

        [LuaFunction("debug")]
        public void Print(string text)
        {
            Bot.Debug(text);
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
