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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA
{
	public class Actor
	{
		public readonly ActorInfo Info;

		public readonly World World;
		public readonly uint ActorID;

		public int2 Location { get { return Trait<IOccupySpace>().TopLeft; } }
		public float2 CenterLocation { get { return Trait<IHasLocation>().PxPosition; } }
		[Sync]
		public Player Owner;

		IActivity currentActivity;
		public Group Group;

		internal Actor(World world, string name, TypeDictionary initDict )
		{
			var init = new ActorInitializer( this, initDict );

			World = world;
			ActorID = world.NextAID();
			if( initDict.Contains<OwnerInit>() )
				Owner = init.Get<OwnerInit,Player>();

			if (name != null)
			{
				if (!Rules.Info.ContainsKey(name.ToLowerInvariant()))
					throw new NotImplementedException("No rules definition for unit {0}".F(name.ToLowerInvariant()));

				Info = Rules.Info[name.ToLowerInvariant()];
				foreach (var trait in Info.TraitsInConstructOrder())
					AddTrait(trait.Create(init));
			}

			Size = Lazy.New(() =>
			{
				var si = Info.Traits.GetOrDefault<SelectableInfo>();
				if (si != null && si.Bounds != null)
					return new float2(si.Bounds[0], si.Bounds[1]);

				// auto size from render
				var firstSprite = TraitsImplementing<IRender>().SelectMany(x => x.Render(this)).FirstOrDefault();
				if (firstSprite.Sprite == null) return float2.Zero;
                return firstSprite.Sprite.size * firstSprite.Scale;
			});
		}

		public void Tick()
		{
			var wasIdle = currentActivity is Idle;
			while (currentActivity != null)
			{
				var a = currentActivity;
				
				var sw = new Stopwatch();
				currentActivity = a.Tick(this) ?? new Idle();
				var dt = sw.ElapsedTime();
				if(dt > Game.Settings.Debug.LongTickThreshold)
					Log.Write("perf", "[{2}] Activity: {0} ({1:0.000} ms)", a, dt * 1000, Game.LocalTick);
				
				if (a == currentActivity) break;

				if (currentActivity is Idle)
				{
					if (!wasIdle)
						foreach (var ni in TraitsImplementing<INotifyIdle>())
							ni.Idle(this);

					break;
				}
			}
		}

		public bool IsIdle
		{
			get { return currentActivity == null || currentActivity is Idle; }
		}

        OpenRA.FileFormats.Lazy<float2> Size;

		public IEnumerable<Renderable> Render()
		{
			var mods = TraitsImplementing<IRenderModifier>();
			var sprites = TraitsImplementing<IRender>().SelectMany(x => x.Render(this));
			return mods.Aggregate(sprites, (m, p) => p.ModifyRender(this, m));
		}

		public RectangleF GetBounds(bool useAltitude)
		{
			var si = Info.Traits.GetOrDefault<SelectableInfo>();

			var size = Size.Value;

            /* apply scaling */
		    var scale = this.TraitOrDefault<Scale>();
            if (scale != null && scale.Info.Value != 1)
                size = size*scale.Info.Value;

			var loc = CenterLocation - 0.5f * size;
			
			if (si != null && si.Bounds != null && si.Bounds.Length > 2)
				loc += new float2(si.Bounds[2], si.Bounds[3]);

			if (useAltitude)
			{
				var move = TraitOrDefault<IMove>();
				if (move != null) loc -= new float2(0, move.Altitude);
			}

			return new RectangleF(loc.X, loc.Y, size.X, size.Y);
		}

		public bool IsInWorld { get; internal set; }

		public void QueueActivity( IActivity nextActivity )
		{
			if( currentActivity == null )
				currentActivity = nextActivity;
			else
				currentActivity.Queue( nextActivity );
		}

		public void CancelActivity()
		{
			if( currentActivity != null )
				currentActivity.Cancel( this );
		}

		// For pathdebug, et al
		public IActivity GetCurrentActivity()
		{
			return currentActivity;
		}

		public override int GetHashCode()
		{
			return (int)ActorID;
		}

		public override bool Equals( object obj )
		{
			var o = obj as Actor;
			return ( o != null && o.ActorID == ActorID );
		}

		public override string ToString()
		{
			return "{0} {1}{2}".F( Info.Name, ActorID, IsInWorld ? "" : " (not in world)" );
		}

		public T Trait<T>()
		{
			return World.traitDict.Get<T>( this );
		}

		public T TraitOrDefault<T>()
		{
			return World.traitDict.GetOrDefault<T>( this );
		}

		public IEnumerable<T> TraitsImplementing<T>()
		{
			return World.traitDict.WithInterface<T>( this );
		}

		public bool HasTrait<T>()
		{
			return World.traitDict.Contains<T>( this );
		}

		public void AddTrait( object trait )
		{
			World.traitDict.AddTrait( this, trait );
		}

		public bool Destroyed { get; private set; }

		public void Destroy()
		{
			World.AddFrameEndTask( w =>
			{
				if (Destroyed || !IsInWorld) return;

				World.Remove( this );
				World.traitDict.RemoveActor( this );
				Destroyed = true;
			} );
		}
	}
}
