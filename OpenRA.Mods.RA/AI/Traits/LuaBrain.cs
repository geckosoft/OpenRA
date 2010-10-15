using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI.Traits
{
    public class LuaBrainInfo : ITraitInfo
    {
        /// <summary>
        /// The Yaml used to load this info instance
        /// </summary>
        [FieldLoader.LoadUsing("StoreYaml")]
        public MiniYaml Yaml = null;

        public virtual object Create(ActorInitializer init)
        {
            return new LuaBrain(init.self, this);
        }

        static object StoreYaml(MiniYaml y)
        {
            return y;
        }
    }

    /// <summary>
    ///  Just to make it easier to assign a custom brain
    /// </summary>
    public interface ILuaBrain : ICloneable, ILuaBrainInterface
    {

    }


    public interface ILuaBrainInterface : ITick, INotifySold, INotifyAttack, INotifyBuildComplete, INotifyCapture, INotifyDamage, INotifyIdle, INotifyProduction
    {

    }
    /// <summary>
    /// The all knowing trait
    /// </summary>
    public class LuaBrain : ITick, INotifySold, INotifyAttack, INotifyBuildComplete, INotifyCapture, INotifyDamage, INotifyIdle, INotifyProduction
    {
        public Actor Self = null;
        public LuaBrainInfo Info = null;
        public Object Tag = null;
        public LuaBot Bot = null;

        public string BrainObject = null;
        public string BrainState = ""; // Actually a 'tag' field :P
        public bool IsUsed = false; // For the lua bot. False means its available for 'use' by other task forces!

        public void Assign(LuaBot bot,  string brainObject)
        {
            BrainObject = brainObject;
            Bot = bot;
        }

        public LuaBrain(Actor self, ITraitInfo info)
        {
            Self = self;
            Info = (LuaBrainInfo)info;
        }

        public void TriggerEvent(string func, object[] args)
        {
            if (BrainObject != null && Bot != null)
            {
                string fullPath = BrainObject + "." + func;

                if (Bot.VM.HasFunction(fullPath))
                {
                    var newArgs = new List<Object>();
                    newArgs.Add(BrainObject);
                    newArgs.Add(func);
                    newArgs.AddRange(args);

                    Bot.VM.CallFunction("botlib.callObject", newArgs.ToArray(), typeof (bool));
                }
            }
        }

        public virtual void Tick(Actor self)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnTick", new[] {Bot.Proxy.GetVar(self)});
            }
        }

        public virtual void Selling(Actor self)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnSelling", new[] { Bot.Proxy.GetVar(self) });
            }
        }

        public virtual void Sold(Actor self)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnSold", new[] { Bot.Proxy.GetVar(self) });
            }
        }

        public virtual void Attacking(Actor self)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnAttacking", new[] { Bot.Proxy.GetVar(self) });
            }

        }

        public virtual void BuildingComplete(Actor self)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnBuildingComplete", new[] { Bot.Proxy.GetVar(self) });
            }
        }

        public virtual void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnCapture", new[] { Bot.Proxy.GetVar(self), Bot.Proxy.GetVar(captor), Bot.Proxy.GetVar(oldOwner), Bot.Proxy.GetVar(newOwner) });
            }
        }

        public virtual void Damaged(Actor self, AttackInfo e)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnDamaged", new[] { Bot.Proxy.GetVar(self), Bot.Proxy.GetVar(e) });
            }
        }

        public virtual void Idle(Actor self)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnIdle", new[] { Bot.Proxy.GetVar(self) });
            }
        }

        public virtual void UnitProduced(Actor self, Actor other, int2 exit)
        {
            if (BrainObject != null && Bot != null && self == Self)
            {
                TriggerEvent("OnUnitProduced", new[] { Bot.Proxy.GetVar(self), Bot.Proxy.GetVar(other), Bot.Proxy.GetVar(exit) });
            }
        }
    }
}
