﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRg.Traits.System
{
	public class RGCrateSpawnerInfo : TraitInfo<RGCrateSpawner>
	{
		public readonly int Minimum = 1; // Minumum number of crates
		public readonly int Maximum = 255; // Maximum number of crates
		public readonly int SpawnInterval = 180; // Average time (seconds) between crate spawn
	}

	public class RGCrateSpawner : ITick
	{
		List<Actor> crates = new List<Actor>();
		int ticks = 0;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<RGCrateSpawnerInfo>();
				ticks = info.SpawnInterval * 25;		// todo: randomize

				crates.RemoveAll(x => !x.IsInWorld);

				var toSpawn = Math.Max(0, info.Minimum - crates.Count)
					+ (crates.Count < info.Maximum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self, info);
			}
		}

		void SpawnCrate(Actor self, RGCrateSpawnerInfo info)
		{
			int2 p = int2.Zero;

			var spawns = self.World.Actors.Where(a => !a.Destroyed && a.IsInWorld && a.Info.Name == "crate_spawn").Where(s => !crates.Any(c => c.Location == s.Location));
			if (spawns.Count() == 0)
				return; /* no spawn poins for crates found! */

			p = spawns.Random(self.World.SharedRandom).Location;

			self.World.AddFrameEndTask(
					w => crates.Add(w.CreateActor("crate", new TypeDictionary
			        {
						new LocationInit( p ),
						new OwnerInit( self.World.WorldActor.Owner ),
					})));
			return;
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			yield break;
		}
	}
}
