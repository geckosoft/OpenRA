using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.RA.AI.Module;
using OpenRA.Mods.RA.AI.Proxies;
using OpenRA.Traits;
using SharpLua;

namespace OpenRA.Mods.RA.AI
{
    public class LuaBot : ITick, IBot
    {
        protected ActorList Actors;
        protected Dictionary<string, ulong> BuildTicks = new Dictionary<string, ulong>();

        public bool Enabled;
        public Player Player;
        public Random Random = new Random();
        protected PowerManager Power;
        protected ulong Ticks;
        protected ulong TicksPerCheck = 60;
        protected ulong TicksPerUpdate = 5;
        protected ulong TicksPerProductionUpdate = 30;
        public LuaVM VM;
        public Proxy Proxy = null;
        public AutoProductionQueue Queue;

        public LuaBot()
        {
            // Should not be called!
            throw new ApplicationException("Should not be constructed with default contructor!");
        }
        /// <summary>
        /// Ultimate cleanup
        /// </summary>
        ~LuaBot()
        {
            try
            {
                if (VM != null)
                {
                    VM.Close();
                    VM = null;
                }
            }
            catch (Exception)
            {

            }
        }

        public LuaBot(ActorInitializer init, IBotInfo info)
        {
            Info = (IBotInfo) info; //.Clone(this); // Get a personal instance :)
        }

        protected virtual ulong TicksPerAIRun
        {
            get { return 5; }
        }

        public PlayerResources Resources
        {
            get { return Player.PlayerActor.Trait<PlayerResources>(); }
        }

        public List<Actor> Factories
        {
            get { return Actors["Factories"]; }
        }

        #region IBot Members

        public virtual IBotInfo Info { get; protected set; }

        public virtual void Activate(Player p)
        {
            Actors = new ActorList();
            Actors.OnActorDestroyed = new Func<Actor, bool>(OnActorDestroyed);
            Player = p;
            Queue = new AutoProductionQueue(this);
            OnActivate(p);
            Enabled = true;
        }

        protected virtual bool OnActorDestroyed(Actor actor)
        {
            if (VM.HasFunction("Events.OnActorDestroyed"))
            {
                CallFunction("Events.OnActorDestroyed", new[] { Proxy.GetVar(actor) }, typeof(bool));
            }

            return true; // Needed?
        }

        public LuaBotInfo MyInfo
        {
            get { return (LuaBotInfo) Info; }
        }

        #endregion

        #region ITick Members

        public void Tick(Actor self)
        {
            if (!Enabled) /* not active - return */
                return;

            Ticks++;

            if (Ticks > 9) /* skip first 10 ticks */
            {
                if (Ticks == 10)
                {
                    OnFirstRun(self);
                }

                OnPreRun(self);
                // See if we have to trigger an update
                if (Ticks%TicksPerUpdate == 0)
                {
                    OnCleanup(self);
                    OnUpdate(self);
                }

                // See if we have to perform some checks
                if (Ticks%TicksPerCheck == 0)
                {
                    OnCheck(self);
                }

                OnRun(self);
            }
        }

        private void OnFirstRun(Actor self)
        {
            if (VM.HasFunction("Events.OnFirstRun"))
            {
                CallFunction("Events.OnFirstRun", new[] { Proxy.GetVar(self) }, typeof(bool));
            }
        }

        protected void OnRun(Actor self)
        {
            if (VM.HasFunction("Events.OnRun"))
            {
                CallFunction("Events.OnRun", new[] { Proxy.GetVar(self) }, typeof(bool));
            }
        }

        protected void OnCheck(Actor self)
        {
            if (VM.HasFunction("Events.OnCheck"))
            {
                CallFunction("Events.OnCheck", new[] { Proxy.GetVar(self) }, typeof(bool));
            }
        }

        protected object CallFunction(string func, object[] args, Type returnType)
        {
            try
            {
                if (VM.HasFunction(func))
                {
                    return VM.CallFunction(func, args, returnType);
                }
            }
            catch (Exception ex)
            {
                Debug("Lua error: " + ex.Message);
            }

            return null;
        }

        #endregion

        public int GetPowerProvided()
        {
            return Power.PowerProvided;
        }

        public int GetPowerUsed()
        {
            return Power.PowerDrained;
        }

        public int GetPowerUnused()
        {
            return GetPowerProvided() - GetPowerUsed();
        }

        public virtual void Debug(string msg)
        {
            Game.Debug("[" + Info.Identifier + "] " + msg);
        }

        public ProductionQueue GetAvailableQueue(string type)
        {
            return Game.world.Queries.WithTraitMultiple<ProductionQueue>()
                .Where(a => a.Actor.Owner == Player && a.Trait.Info.Type == type && a.Trait.CurrentItem() == null)
                .Select(a => a.Trait).FirstOrDefault();
        }

        public ProductionQueue[] GetAvailableQueues(string type)
        {
            return Game.world.Queries.WithTraitMultiple<ProductionQueue>()
                .Where(a => a.Actor.Owner == Player && a.Trait.Info.Type == type && a.Trait.CurrentItem() == null)
                .Select(a => a.Trait).ToArray();
        }

        public ProductionQueue[] GetQueues()
        {
            return Game.world.Queries.WithTraitMultiple<ProductionQueue>()
                .Where(a => a.Actor.Owner == Player)
                .Select(a => a.Trait)
                .ToArray();
        }

        public ProductionQueue[] GetQueues(string type)
        {
            return Game.world.Queries.WithTraitMultiple<ProductionQueue>()
                .Where(a => a.Actor.Owner == Player && a.Trait.Info.Type == type)
                .Select(a => a.Trait)
                .ToArray();
        }

        public ProductionQueue GetQueue(string type)
        {
            return Game.world.Queries.WithTraitMultiple<ProductionQueue>()
                .Where(a => a.Actor.Owner == Player && a.Trait.Info.Type == type)
                .Select(a => a.Trait)
                .FirstOrDefault();
        }

        public int2? ChooseBuildLocation(ProductionItem item, int maxBaseDistance, int2? baseCenter)
        {
            if (baseCenter == null)
                return null;

            var bi = Rules.Info[item.Item].Traits.Get<BuildingInfo>();

            for (int k = 0; k < maxBaseDistance; k++)
                foreach (int2 t in Game.world.FindTilesInCircle((int2)baseCenter, k))
                    if (Game.world.CanPlaceBuilding(item.Item, bi, t, null))
                        if (Game.world.IsCloseEnoughToBase(Player, item.Item, bi, t))
                            return t;

            return null; // i don't know where to put it.
        }

        public Actor[] GetOwnedActors()
        {
            return Player.World.Queries.OwnedBy[Player].ToArray();
        }

        public virtual int2? GetConstructionYardLocation()
        {
            Actor fact = GetOwnedActors().FirstOrDefault(a => a.Info == Rules.Info["fact"]);
            if (fact == null)
                return null;

            return fact.Location;
        }

        protected virtual bool OnPlaceBuilding(ProductionQueue queue, ProductionItem currentBuilding)
        {


            if (VM.HasFunction("Events.OnBuildingFinished"))
            {
                if (!(bool)CallFunction("Events.OnBuildingFinished", new[] { Proxy.GetVar(currentBuilding) }, typeof(bool)))
                    return false;
            }else
            {
                // Fallback - auto building!
                if (!PlaceBuilding(currentBuilding, ChooseBuildLocation(currentBuilding, 15, GetConstructionYardLocation())))
                {
                    Debug("Nowhere to place {0}.".F(currentBuilding.Item));
                    return false;
                }
            }

            return true;
        }


        public bool PlaceBuilding(ProductionItem currentBuilding, int2? location)
        {
            if (location == null)
                return false;

            Debug("Placing {0}".F(currentBuilding.Item));

            Game.IssueOrder(new Order("PlaceBuilding", Player.PlayerActor, location.Value,
                                      currentBuilding.Item));
            return true;
        }

        public string GetBuildableType(ActorInfo actor)
        {
            if (actor.Traits.GetOrDefault<BuildableInfo>() == null)
                return null;

            return actor.Traits.GetOrDefault<BuildableInfo>().Queue;
        }

        public AutoProductionQueue.Entry QueueProduction(string actor)
        {
            if (!Rules.Info.ContainsKey(actor))
                return null;

            var i = Rules.Info[actor];
            var b = GetBuildableType(i);
            if ( b == null)
                return null;

            return Queue.Enqueue(b, actor);
        }

        protected bool BuildNext(string buildType)
        {
            if ((BuildTicks[buildType.ToUpper()] != 0 && Ticks - BuildTicks[buildType.ToUpper()] < TicksPerProductionUpdate))
                return false;

            bool buildSomething = false;

            ProductionQueue queue = GetAvailableQueue(buildType);
            if (queue != null)
            {
                // Building Q available
                ProductionItem currentUnitBeingBuild = queue.CurrentItem();

                // if nothing is being build
                if (currentUnitBeingBuild == null)
                {
                    // See what we want to build
                    //AIGroupItem item = GetNextItem(buildType, queue);
                    // GET ITEM TO BUILD
                    var item = Queue.Dequeue(buildType, queue);
                    BuildTicks[buildType.ToUpper()] = Ticks;

                    if (item == null) // item == null
                    {

                    }
                    else
                    {
                        Debug("Building unit {0}".F(item.ActorName));

                        // Issue the build order
                        Game.IssueOrder(Order.StartProduction(queue.self, item.ActorName, 1));
                        buildSomething = true;
                    }
                }
            }
            if (buildSomething)
                return true;

            queue = GetQueue(buildType);
            if (queue != null)
            {
                // Building Q available
                ProductionItem currentUnitBeingBuild = queue.CurrentItem();

                // if nothing is being build
                if (currentUnitBeingBuild == null)
                {
                    // See what we want to build
                    var item = Queue.Dequeue(buildType, queue);

                    BuildTicks[buildType.ToUpper()] = Ticks;

                    if (item == null)
                    {
                        return false;
                    }

                    Debug("Building unit {0}".F(item.ActorName));

                    // Issue the build order
                    Game.IssueOrder(Order.StartProduction(queue.self, item.ActorName, 1));

                    return true;
                }
                else if (currentUnitBeingBuild.Paused)
                    Game.IssueOrder(Order.PauseProduction(queue.self, currentUnitBeingBuild.Item, false)); // Resume
                else if (currentUnitBeingBuild.Done) /* Done! */
                {
                    // Place it, since building is done
                    BuildTicks[buildType.ToUpper()] = Ticks;

                    if (buildType == "Building")
                    {
                        if (!OnPlaceBuilding(queue, currentUnitBeingBuild))
                            Game.IssueOrder(Order.CancelProduction(queue.self, currentUnitBeingBuild.Item, 1));
                    }
                    else if (buildType == "Defense")
                    {
                        if (!OnPlaceBuilding(queue, currentUnitBeingBuild))
                            Game.IssueOrder(Order.CancelProduction(queue.self, currentUnitBeingBuild.Item, 1));
                    }
                }
            }

            return false;
        }


        public void DeployMcv()
        {
            /* find our mcv and deploy it */
            Actor mcv = Player.World.Queries.OwnedBy[Player]
                .FirstOrDefault(a => a.Info == Rules.Info["mcv"]);

            if (mcv != null)
            {
                // baseCenter = mcv.Location;
                Game.IssueOrder(new Order("DeployTransform", mcv));

                //Debug("Deploying MCV.");
            }
            else
                Debug("Can't find the MCV.");
        }

        protected virtual void OnActivate(Player player)
        {
            Power = Player.PlayerActor.Trait<PowerManager>();
            GenerateBuildTicks();

            if (((LuaBotInfo)Info).Script != "")
            {
                var source = ((LuaBotInfo) Info).Script;
                Debug("Loading AI script: " + source);

                if (!LoadAI(source))
                {
                    Debug("Could not load the AI");
                    return;
                }

                if (VM.HasFunction("Events.OnActivate"))
                {
                    CallFunction("Events.OnActivate", new[] { Proxy.GetVar(player) }, typeof(bool));
                }
            }else
            {
                Debug("Could not load Lua' Bot AI: " + ((LuaBotInfo)Info).Script);
                Debug("Bot disabled!");
                Enabled = false;
            }

        }

        protected virtual bool LoadAI(object source)
        {
            VM = new LuaVM();
            VM.Open();
            Proxy = new Proxy(VM);

            VM.OpenLibs();

            VM.RegisterObject(new ModEngine(this));

            /* Register lua object proxies */
            VM.RegisterObject(typeof(ActorProxy)); /* Actor */
            VM.RegisterObject(typeof(PlayerProxy)); /* Player */
            VM.RegisterObject(typeof(Int2Proxy)); /* int2 */
            VM.RegisterObject(typeof(ProductionItemProxy)); /* ProductionItem */
            VM.RegisterObject(typeof(AttackInfoProxy)); /* AttackInfo */
            VM.RegisterObject(typeof(MiniYamlProxy)); /* MiniYaml */

            int s = VM.Run(Path.GetFullPath(MyInfo.Script));

            try
            {
                VM.CheckStatus(s);
            }catch(Exception ex)
            {
                Debug("Lua Error: " + ex.Message);
                return false;
            }

            return true;
        }

        protected virtual void OnPreRun(Actor self)
        {
            Actors["NewActors"].AddRange(self.World.Queries.OwnedBy[Player]
                                             .Where(
                                                 a => a != Player.PlayerActor &&  !Actors["NewActors"].Contains(a) && !Actors["Actors"].Contains(a))
                                             .ToList());

            // Update known units (actors that can IMove)
            Actors["NewUnits"].AddRange(self.World.Queries.OwnedBy[Player]
                                            .Where(
                                                a => a != Player.PlayerActor &&  
                                                a.HasTrait<IMove>() && !Actors["Units"].Contains(a) &&
                                                !Actors["NewUnits"].Contains(a)).ToList());


            Actors["NewBuildings"].AddRange(self.World.Queries.OwnedBy[Player]
                                            .Where(
                                                a => a != Player.PlayerActor &&   !a.HasTrait<IMove>() && 
                                                a.HasTrait<Buildable>() && !Actors["Buildings"].Contains(a) &&
                                                !Actors["NewBuildings"].Contains(a)).ToList());


            Actors["NewFactories"].AddRange(self.World.Queries.OwnedBy[Player]
                                                .Where(a => (a != Player.PlayerActor &&  a.TraitOrDefault<Production>() != null
                                                             && !Actors["Factories"].Contains(a) &&
                                                             !Actors["NewFactories"].Contains(a))).ToList());

            if (VM.HasFunction("Events.OnPreRun"))
            {
                CallFunction("Events.OnPreRun", new[] { Proxy.GetVar(self) }, typeof(bool));
            }
        }

        public bool MoveActor(Actor a, int2 desiredMoveTarget)
        {
            if (!a.HasTrait<IMove>())
                return false;

            int2 xy;
            int loopCount = 0; //avoid infinite loops.
            int range = 2;
            do
            {
                //loop until we find a valid move location
                xy = new int2(desiredMoveTarget.X + Random.Next(-range, range),
                              desiredMoveTarget.Y + Random.Next(-range, range));
                loopCount++;
                range = Math.Max(range, loopCount/2);
                if (loopCount > 10) return false;
            } while (!a.Trait<IMove>().CanEnterCell(xy) && xy != a.Location);
            Game.IssueOrder(new Order("Move", a, xy));

            return true;
        }

        private void GenerateBuildTicks()
        {
            BuildTicks.Add("Building".ToUpper(), 0);
            BuildTicks.Add("Defense".ToUpper(), 0);
            BuildTicks.Add("Infantry".ToUpper(), 0);
            BuildTicks.Add("Plane".ToUpper(), 0);
            BuildTicks.Add("Vehicle".ToUpper(), 0);
            BuildTicks.Add("Ship".ToUpper(), 0);
        }

        /// <summary>
        /// won't work for shipyards...
        /// </summary>
        public int2 ChooseRallyLocationNear(int2 startPos)
        {
            foreach (int2 t in Game.world.FindTilesInCircle(startPos, 6))
            {
                if (Game.world.IsCellBuildable(t, false) && t != startPos)
                {
                    return t;
                }
            }
            return startPos; // i don't know where to put it.
        }


        protected virtual void OnUpdate(Actor self)
        {
            foreach (Actor a in Actors["NewActors"])
            {
                OnActorCreated(a);
                Actors["Actors"].Add(a);
            }

            // Trigger OnFactoryCreated events
            foreach (Actor a in Actors["NewFactories"])
            {
                OnFactoryCreated(a);
                Actors["Factories"].Add(a);
            }

            // Trigger OnUnitCreated events
            foreach (Actor a in Actors["NewUnits"])
            {
                OnUnitCreated(a);
                Actors["Units"].Add(a);
            }


            // Trigger OnUnitCreated events
            foreach (Actor a in Actors["NewBuildings"])
            {
                OnStructureCreated(a);
                Actors["Buildings"].Add(a);
            }

            Actors["NewFactories"].Clear();
            Actors["NewUnits"].Clear();
            Actors["NewActors"].Clear();
            Actors["NewBuildings"].Clear();

            // Run the building Queue
            while (BuildNext("Building")) { }
            while (BuildNext("Infantry")) { }
            while (BuildNext("Vehicle")) { }
            while (BuildNext("Plane")) { }
            while (BuildNext("Ship")) { }
            while (BuildNext("Defense")) { }

            if (VM.HasFunction("Events.OnUpdate"))
            {
                CallFunction("Events.OnUpdate", new[] { Proxy.GetVar(self) }, typeof(bool));
            }
        }

        private void OnStructureCreated(Actor actor)
        {
            if (VM.HasFunction("Events.OnStructureCreated"))
            {
                CallFunction("Events.OnStructureCreated", new[] { Proxy.GetVar(actor) }, typeof(bool));
            }
        }


        protected virtual void OnCleanup(Actor self)
        {
            //if (ScriptHasFunction("Bot::onCleanUp"))
            //    ScriptRunFunction("Bot::onCleanUp", new object[] { Wrapper.Wrap(self, "", this) });

            // Clean up dead (destroyed) actors
            Actors.Cleanup();
        }

        #region Semi-core Events

        protected virtual void OnActorCreated(Actor actor)
        {
            if (VM.HasFunction("Events.OnActorCreated"))
            {
                CallFunction("Events.OnActorCreated", new[] { Proxy.GetVar(actor) }, typeof(bool));
            }
        }

        /// <summary>
        /// Triggered when a new unit is found (create / spawned / ...)
        /// </summary>
        /// <param name="factory"></param>
        protected virtual void OnUnitCreated(Actor unit)
        {
            if (VM.HasFunction("Events.OnUnitCreated"))
            {
                CallFunction("Events.OnUnitCreated", new[] { Proxy.GetVar(unit) }, typeof(bool));
            }
        }

        protected virtual void OnFactoryCreated(Actor factory)
        {
            if (VM.HasFunction("Events.OnFactoryCreated"))
            {
                CallFunction("Events.OnFactoryCreated", new[] { Proxy.GetVar(factory) }, typeof(bool));
            }
        }

        #endregion

        public int GetMoney()
        {
            return Resources.Cash;
        }
    }
}