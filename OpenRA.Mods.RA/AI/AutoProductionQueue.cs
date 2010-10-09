using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.RA.AI
{
    public class AutoProductionQueue
    {
        public LuaBot Bot;
        public List<Entry> Pending;

        public class Entry
        {
            public string QueueType = "";
            public string ActorName = "";
            public ProductionQueue Queue = null;

            public Entry(string type, string actor)
            {
                QueueType = type;
                ActorName = actor;
            }
            /// <summary>
            /// Use this?
            /// </summary>
            public bool BeingProduced
            {
                get
                {
                    if (Queue == null || Queue.CurrentItem() == null || Queue.CurrentItem().Item != ActorName)
                        return false;

                    return true;
                }
            }

            public bool CanBuild(ProductionQueue queue)
            {
                if (queue.BuildableItems().Where(a => a.Name == ActorName).FirstOrDefault() == null)
                    return false;

                return true;
            }
        }

        public AutoProductionQueue(LuaBot luaBot)
        {
            Pending = new List<Entry>();
            Bot = luaBot;
        }


        public void Clear(string queueType)
        {
            lock (this)
            {
                var t = Pending.ToList();

                for (int i = 0; i < t.Count; i++)
                {
                    var e = t[i];

                    if (e.QueueType == queueType)
                    {
                        Pending.Remove(e);
                    }
                }
            }
        }

        public void Clear()
        {
            Pending.Clear();
        }
        public Entry Enqueue(string queueType, string actor)
        {
            lock (this)
            {
                var e = new Entry(queueType, actor);
                Pending.Add(e);

                return e;
            }
        }
        /*
         * 
                if (engine.GetBuildableType(item.Info) != buildableType)
                    continue;

                if (queue.BuildableItems().Where(a => a.Name == item.Info.Name).FirstOrDefault() == null)
                    continue;
         * */
        /// <summary>
        /// Gets the next entry that should be build!
        /// </summary>
        public Entry Next(string buildType, ProductionQueue queue)
        {
            lock (this)
            {
                for (int i = 0; i < Pending.Count;  i++)
                {
                    var e = Pending[i];
                    if (e.QueueType == buildType)
                    {
                        return e;
                    }
                }
            }

            return null;
        }

        public Entry Dequeue(string buildType, ProductionQueue queue)
        {
            lock (this)
            {
                var e = Next(buildType, queue);

                if (e == null)
                    return null;

                if (!e.CanBuild(queue))
                {
                    // TODO trigger an event?
                    return null;
                }
                Pending.Remove(e);

                return e;
            }
        }
    }
}

