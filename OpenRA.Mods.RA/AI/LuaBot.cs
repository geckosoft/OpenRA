using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Traits;
using SharpLua;

namespace OpenRA.Mods.RA.AI
{
    public class LuaBot : ITick, IBot
    {
        protected ActorList Actors = new ActorList();
        protected Dictionary<string, ulong> BuildTicks = new Dictionary<string, ulong>();

        public bool Enabled;
        public Player Player;
        public Random Random = new Random();
        protected PowerManager Power;
        protected ulong Ticks;
        protected ulong TicksPerCheck = 60;
        protected ulong TicksPerUpdate = 30;
        protected LuaVM VM;


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
            Info = (IBotInfo) info.Clone(this); // Get a personal instance :)
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
            Player = p;

            OnActivate();
            Enabled = true;
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

        protected void OnRun(Actor self)
        {
            if (VM.HasFunction("Events.OnTick"))
            {
                VM.CallFunction("Events.OnTick", new object[] {self}, typeof(bool));
            }
        }

        protected void OnCheck(Actor self)
        {

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

        protected void DeployMcv(Actor self)
        {
            /* find our mcv and deploy it */
            Actor mcv = self.World.Queries.OwnedBy[Player]
                .FirstOrDefault(a => a.Info == Rules.Info["mcv"]);

            if (mcv != null)
            {
                // baseCenter = mcv.Location;
                Game.IssueOrder(new Order("DeployTransform", mcv));

                Debug("Deploying MCV.");
            }
            else
                Debug("Can't find the MCV.");
        }

        protected virtual void OnActivate()
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

                //ScriptRunFunction("Bot::onActivate", new object[] { p });
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

            VM.OpenLibs();
            VM.RegisterObject(new Modules.ModEngine(this));

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
                                                 a => !Actors["NewActors"].Contains(a) && !Actors["Actors"].Contains(a))
                                             .ToList());

            // Update known units (actors that can IMove)
            Actors["NewUnits"].AddRange(self.World.Queries.OwnedBy[Player]
                                            .Where(
                                                a =>
                                                a.HasTrait<IMove>() && !Actors["Units"].Contains(a) &&
                                                !Actors["NewUnits"].Contains(a)).ToList());

            Actors["NewFactories"].AddRange(self.World.Queries.OwnedBy[Player]
                                                .Where(a => (a.TraitOrDefault<RallyPoint>() != null
                                                             && !Actors["Factories"].Contains(a) &&
                                                             !Actors["NewFactories"].Contains(a))).ToList());
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
            BuildTicks.Add("Infantry".ToUpper(), 0);
            BuildTicks.Add("Plane".ToUpper(), 0);
            BuildTicks.Add("Vehicle".ToUpper(), 0);
            BuildTicks.Add("Ship".ToUpper(), 0);
        }

        /// <summary>
        /// won't work for shipyards...
        /// </summary>
        protected int2 ChooseRallyLocationNear(int2 startPos)
        {
            foreach (int2 t in Game.world.FindTilesInCircle(startPos, 6))
                if (Game.world.IsCellBuildable(t, false) && t != startPos)
                    return t;

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

            Actors["NewFactories"].Clear();
            Actors["NewUnits"].Clear();
            Actors["NewActors"].Clear();
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
        }

        /// <summary>
        /// Triggered when a new unit is found (create / spawned / ...)
        /// </summary>
        /// <param name="factory"></param>
        protected virtual void OnUnitCreated(Actor unit)
        {
        }

        protected virtual void OnFactoryCreated(Actor factory)
        {
            // Default behaviour - set rally point
            //int2 newRallyPoint = ChooseRallyLocationNear(factory.Location);
            //Game.IssueOrder(new Order("SetRallyPoint", factory, newRallyPoint));
            //
            //if (ScriptHasFunction("Bot::onFactoryCreated"))
            //   ScriptRunFunction("Bot::onFactoryCreated", new object[] { Wrapper.Wrap(factory, "", this) });
        }

        #endregion
    }
}