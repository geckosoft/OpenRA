#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Support;

namespace OpenRA
{
	public static class Exts
	{
		public static bool HasModifier(this Modifiers k, Modifiers mod)
		{
			return (k & mod) == mod;
		}

		public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> xs, IEnumerable<T> ys)
		{
			// this is probably a shockingly-slow way to do this, but it's concise.
			return xs.Except(ys).Concat(ys.Except(xs));
		}

		public static float Product(this IEnumerable<float> xs)
		{
			return xs.Aggregate(1f, (a, x) => a * x);
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k)
			where V : new()
		{
			return d.GetOrAdd(k, _ => new V());
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, Func<K, V> createFn)
		{
			V ret;
			if (!d.TryGetValue(k, out ret))
				d.Add(k, ret = createFn(k));
			return ret;
		}

		public static T Random<T>(this IEnumerable<T> ts, Thirdparty.Random r)
		{
			var xs = ts.ToArray();
			return xs[r.Next(xs.Length)];
		}

		public static void DoTimed<T>(this IEnumerable<T> e, Action<T> a, string text, double time)
		{
			var sw = new Stopwatch();

			e.Do(x =>
			{
				var t = sw.ElapsedTime();
				a(x);
				var dt = sw.ElapsedTime() - t;
				if (dt > time)
					Log.Write("perf", text, x, dt * 1000, Game.LocalTick);
			});
		}

		public static void Write(this BinaryWriter w, int2 data)
		{
			w.Write(data.X);
			w.Write(data.Y);
		}

		public static void Write(this BinaryWriter w, string data, bool addBoolean)
		{
			if (addBoolean)
				w.Write(data != null);

			if (addBoolean && data != null)
				w.Write(data);
		}

		public static int2 ReadInt2(this BinaryReader r)
		{
			return new int2(r.ReadInt32(), r.ReadInt32());
		}

		public static string ReadString(this BinaryReader r, bool readBoolean)
		{
			return readBoolean ? (r.ReadBoolean() ? r.ReadString() : null) : r.ReadString();
		}
	}
}
