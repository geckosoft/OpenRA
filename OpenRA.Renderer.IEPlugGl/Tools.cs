using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenRA.Renderer.IEPlugGl
{
	public static class Native
	{
		[DllImport("kernel32")]
		private unsafe static extern void* LoadLibrary(string dllname);

		[DllImport("kernel32")]
		private unsafe static extern void FreeLibrary(void* handle);

		public sealed unsafe class Library
		{
			internal Library(string path)
			{
				handle = LoadLibrary(path);
			}

			~Library()
			{
				if (handle != null)
					FreeLibrary(handle);
			}

			private void* handle;

		} // Library
	}
}
