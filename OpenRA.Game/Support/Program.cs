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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace OpenRA
{
	public static class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			// brutal hack
			Application.CurrentCulture = CultureInfo.InvariantCulture;

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			if (Debugger.IsAttached || args.Contains("--just-die"))
			{
				Run(args);
				return;
			}

			try
			{
				Run( args );
			}
			catch( Exception e )
			{
				Log.AddChannel("exception", "exception.log");
				Log.Write("exception", "{0}", e.ToString());
				throw;
			}
		}


		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{//This handler is called only when the common language runtime tries to bind to the assembly and fails.

			//Retrieve the list of referenced assemblies in an array of AssemblyName.
			Assembly MyAssembly, objExecutingAssemblies;
			string strTempAssmbPath = "";

			objExecutingAssemblies = Assembly.GetExecutingAssembly();
			AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

			//Loop through the array of referenced assembly names.
			foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
			{
				//Check for the assembly names that have raised the "AssemblyResolve" event.
				if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
				{
					//Build the path of the assembly from where it has to be loaded.				
					strTempAssmbPath = Path.GetFullPath(args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll");
					break;
				}

			}
			//Load the assembly from the specified path. 					
			MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

			//Return the loaded assembly.
			return MyAssembly;
		}

		public static void Run( string[] args )
		{
			Game.Initialize( new Arguments(args) );
			GC.Collect();
			Game.Run();
		}
	}
}