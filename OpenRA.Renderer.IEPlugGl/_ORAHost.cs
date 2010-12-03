using System.Runtime.InteropServices;
using Tao.Sdl;

namespace OpenRA.IEPlug
{
	[Guid("3E05ADDA-673D-46d4-98ED-0EDA7F659BEB"), ComVisible(true)]
	[InterfaceType( ComInterfaceType.InterfaceIsDual)]

	public interface _ORAHost
	{
		void Start();
		void StartRendering();
		bool PollEvents(out Sdl.SDL_Event e);
	}
}