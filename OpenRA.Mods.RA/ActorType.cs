using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ActorTypeInfo : ITraitInfo
	{
		public readonly string[] Types = {};
		public object Create(ActorInitializer init) { return new ActorType(this); }
	}

	public class ActorType
	{
		public readonly ActorTypeInfo Info;

		public string[] Types;

		[Sync]
		public int Hash { get { return string.Join(",", Types).GetHashCode(); } }

		public ActorType(ActorTypeInfo info)
		{
			Info = info;

			Types = info.Types.Select(t => t.ToLowerInvariant()).OrderBy(t => t).ToArray();
		}

		public bool HasAny(string[] typesList)
		{
			return typesList.Select(t => t.ToLowerInvariant()).Any(flag => Types.Contains(flag));
		}

		public bool HasAll(string[] typesList)
		{
			return typesList.Select(t => t.ToLowerInvariant()).All(flag => Types.Contains(flag));
		}
	}
}
