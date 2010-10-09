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


    public interface ILuaBrainInterface : ITick, INotifySold, INotifyAttack, INotifyBuildComplete, INotifyCapture, INotifyDamage, INotifyIdle, INotifyProduction, INotifySelection
    {

    }
    /// <summary>
    /// The all knowing trait
    /// </summary>
    public class LuaBrain : ITick, INotifySold, INotifyAttack, INotifyBuildComplete, INotifyCapture, INotifyDamage, INotifyIdle, INotifyProduction, INotifySelection
    {
        public Actor Self = null;
        public LuaBrainInfo Info = null;
        public Object Tag = null;
        public string State = ""; // A string representation, can be used to store a state, ...
        public ILuaBrain Core = null;

        public void Assign(ILuaBrain brain)
        {
            Core = brain;
        }

        public ILuaBrain Get()
        {
            return Core;
        }

        public LuaBrain(Actor self, ITraitInfo info)
        {
            Self = self;
            Info = (LuaBrainInfo)info;
        }

        public virtual void Tick(Actor self)
        {
            if (Core != null)
                Core.Tick(self);
        }

        public virtual void Selling(Actor self)
        {
            if (Core != null)
                Core.Selling(self);
        }

        public virtual void Sold(Actor self)
        {
            if (Core != null)
                Core.Sold(self);

        }

        public virtual void Attacking(Actor self)
        {
            if (Core != null)
                Core.Attacking(self);

        }

        public virtual void BuildingComplete(Actor self)
        {
            if (Core != null)
                Core.BuildingComplete(self);

        }

        public virtual void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
        {
            if (Core != null)
                Core.OnCapture(self, captor, oldOwner, newOwner);

        }

        public virtual void Damaged(Actor self, AttackInfo e)
        {
            if (Core != null)
                Core.Damaged(self, e);

        }

        public virtual void Idle(Actor self)
        {
            if (Core != null)
                Core.Idle(self);

        }

        public virtual void UnitProduced(Actor self, Actor other, int2 exit)
        {
            if (Core != null)
                Core.UnitProduced(self, other, exit);
        }

        public virtual void SelectionChanged()
        {
            if (Core != null)
                Core.SelectionChanged();
        }
    }
}
