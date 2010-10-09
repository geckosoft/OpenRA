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

        public virtual void Tick(Actor self)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnTick"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnTick", new[] {Bot.Proxy.Get(self)}, typeof (bool));
                }
            }
        }

        public virtual void Selling(Actor self)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnSelling"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnSelling", new[] { Bot.Proxy.Get(self) }, typeof(bool));
                }
            }
        }

        public virtual void Sold(Actor self)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnSold"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnSold", new[] { Bot.Proxy.Get(self) }, typeof(bool));
                }
            }
        }

        public virtual void Attacking(Actor self)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnAttacking"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnAttacking", new[] { Bot.Proxy.Get(self) }, typeof(bool));
                }
            }

        }

        public virtual void BuildingComplete(Actor self)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnBuildingComplete"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnBuildingComplete", new[] { Bot.Proxy.Get(self) }, typeof(bool));
                }
            }
        }

        public virtual void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnCapture"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnCapture", new[] { Bot.Proxy.Get(self), Bot.Proxy.Get(captor), Bot.Proxy.Get(oldOwner), Bot.Proxy.Get(newOwner) }, typeof(bool));
                }
            }
        }

        public virtual void Damaged(Actor self, AttackInfo e)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnDamaged"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnDamaged", new[] { Bot.Proxy.Get(self), Bot.Proxy.Get(e) }, typeof(bool));
                }
            }
        }

        public virtual void Idle(Actor self)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnIdle"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnIdle", new[] { Bot.Proxy.Get(self) }, typeof(bool));
                }
            }
        }

        public virtual void UnitProduced(Actor self, Actor other, int2 exit)
        {
            if (BrainObject != null && Bot != null)
            {
                if (Bot.VM.HasFunction(BrainObject + ".OnUnitProduced"))
                {
                    Bot.VM.CallFunction(BrainObject + ".OnUnitProduced", new[] { Bot.Proxy.Get(self), Bot.Proxy.Get(other), Bot.Proxy.Get(exit) }, typeof(bool));
                }
            }
        }
    }
}
