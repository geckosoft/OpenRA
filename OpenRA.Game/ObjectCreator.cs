﻿#region Copyright & License Information
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
using System.Reflection;
using OpenRA.FileFormats;

namespace OpenRA
{
	public class ObjectCreator
	{
		Pair<Assembly, string>[] ModAssemblies;

		public ObjectCreator( Manifest manifest )
		{
			// All the core namespaces
			var asms = typeof(Game).Assembly.GetNamespaces()
				.Select(c => Pair.New(typeof(Game).Assembly, c))
				.ToList();

			// Namespaces from each mod assembly
			foreach (var a in manifest.Assemblies)
			{
				var asm = Assembly.LoadFile(Path.GetFullPath(Path.Combine(Game.SupportDir, a)));
				asms.AddRange(asm.GetNamespaces().Select(ns => Pair.New(asm, ns)));
			}

			ModAssemblies = asms.ToArray();
		}

		public static Action<string> MissingTypeAction = 
			s => { throw new InvalidOperationException("Cannot locate type: {0}".F(s)); };

		public T CreateObject<T>(string className)
		{
			return CreateObject<T>( className, new Dictionary<string, object>() );
		}

		public T CreateObject<T>( string className, Dictionary<string, object> args )
		{
			foreach( var mod in ModAssemblies )
			{
				var type = mod.First.GetType( mod.Second + "." + className, false );
				if( type == null ) continue;
				var ctors = type.GetConstructors( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance ).Where( x => x.HasAttribute<UseCtorAttribute>() ).ToList();
				if( ctors.Count == 0 )
					return (T)CreateBasic( type );
				else if( ctors.Count == 1 )
					return (T)CreateUsingArgs( ctors[ 0 ], args );
				else
					throw new InvalidOperationException( "ObjectCreator: UseCtor on multiple constructors; invalid." );
			}
			MissingTypeAction(className);
			return default(T);
		}

		public object CreateBasic( Type type )
		{
			return type.GetConstructor( new Type[ 0 ] ).Invoke( new object[ 0 ] );
		}

		public object CreateUsingArgs( ConstructorInfo ctor, Dictionary<string, object> args )
		{
			var p = ctor.GetParameters();
			var a = new object[ p.Length ];
			for( int i = 0 ; i < p.Length ; i++ )
			{
				var attrs = p[ i ].GetCustomAttributes<ParamAttribute>();
				if( attrs.Length != 1 ) throw new InvalidOperationException( "ObjectCreator: argument in [UseCtor] doesn't have [Param]" );
				a[ i ] = args[ attrs[ 0 ].ParamName ?? p[i].Name ];
			}
			return ctor.Invoke( a );
		}

		[AttributeUsage( AttributeTargets.Parameter )]
		public class ParamAttribute : Attribute
		{
			public string ParamName { get; private set; }

			public ParamAttribute() { }

			public ParamAttribute( string paramName )
			{
				ParamName = paramName;
			}
		}

		[AttributeUsage( AttributeTargets.Constructor )]
		public class UseCtorAttribute : Attribute
		{
		}
	}
}
