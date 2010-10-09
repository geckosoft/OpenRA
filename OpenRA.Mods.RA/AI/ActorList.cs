using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.RA.AI
{
    public class ActorList
    {
        private readonly Dictionary<string, List<Actor>> _actorLists;

        public Func<Actor, bool> OnActorDestroyed = _ => false;

        public ActorList()
        {
            _actorLists = new Dictionary<string, List<Actor>>();
        }

        public List<Actor> this[string type]
        {
            get
            {
                lock (this)
                {
                    if (!_actorLists.ContainsKey(type))
                        _actorLists.Add(type, new List<Actor>());

                    return _actorLists[type];
                }
            }
            set
            {
                lock (this)
                {
                    if (_actorLists.ContainsKey(type))
                        _actorLists.Remove(type);

                    _actorLists.Add(type, value);
                }
            }
        }

        /// <summary>
        /// Cleans up destroyed actors
        /// </summary>
        public void Cleanup()
        {
            lock (this)
            {
                var lst = _actorLists.Values.ToArray();
                var destroyed = new List<Actor>();

                for (int i = 0; i < lst.Count(); i++)
                {
                    var list = lst[i];

                    foreach (var actor in list.Where(a => a.Destroyed))
                    {
                        destroyed.Add(actor);
                    }

                    list.RemoveAll(a => a.Destroyed);
                }


                if (OnActorDestroyed != null)
                {
                    foreach (var b in destroyed.ToArray())
                    {
                        OnActorDestroyed(b);
                    }
                }
            }
        }
    }

}
