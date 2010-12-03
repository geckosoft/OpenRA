using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace OpenRA.Renderer.IEPlugGl
{
	[ComImport(), Guid("fc4801a3-2ba9-11cf-a229-00aa003d7352")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IObjectWithSite
	{
		void SetSite([In,
		MarshalAs(UnmanagedType.IUnknown)]
object pUnkSite);

		void GetSite(ref Guid riid,
		[MarshalAs(UnmanagedType.IUnknown)]
out object ppvSite);
	}
}
