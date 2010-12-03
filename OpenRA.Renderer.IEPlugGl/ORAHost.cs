using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Win32;
using OpenRA.Graphics;
using OpenRA.Renderer.IEPlugGl;
using OpenRA.Widgets;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.Sdl;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

//[assembly: System.Runtime.InteropServices.ComVisible(true)]
namespace OpenRA.IEPlug
{

	[Guid("EDA0DE07-5693-45b0-8269-7D8ED564BEAB"), ComVisible(true)]
	[ProgId("OpenRA.IEPlug")]
	[ClassInterface(ClassInterfaceType.None) ,ComDefaultInterface(typeof(_ORAHost))]

	public partial class ORAHost : SimpleOpenGlControl, IObjectWithSite
	{
		public ORAHost()
		{
			LoadDependencies();
			InitializeComponent();
			InitializeContexts();
			Instance = this;
			Graphics.Renderer.Plug = Assembly.GetExecutingAssembly();
		}

		List<object> Libraries = new List<object>();
		private void LoadDependencies()
		{
			// Badd m'kay
			Game.Initialize(new Arguments(new string []{}), true);

			// Load list of dlls
			Libraries.Add(new Native.Library(Path.Combine(Game.SupportDir, "freetype6.dll")));
			Libraries.Add(new Native.Library(Path.Combine(Game.SupportDir, "SDL.dll")));
			Libraries.Add(new Native.Library(Path.Combine(Game.SupportDir, "zlib1.dll")));
			Libraries.Add(new Native.Library(Path.Combine(Game.SupportDir, "cgGL.dll")));
		}

		public static ORAHost Instance { get; private set; }

		public void Start()
		{
			Focus();
			Game.Initialize(new Arguments(new string[]{}));
			GC.Collect();
			Ticker.Enabled = true;
		}

		public void StartRendering()
		{
			Visible = true;
		}

		private bool haveViewport = false;

		private void OnPaint(object sender, PaintEventArgs e)
		{
			if (!haveViewport && Game.viewport != null)
			{
				ResizeMe();
				haveViewport = true;
			}
			else if (Game.viewport == null)
				haveViewport = false;
		}

		protected Queue<Sdl.SDL_Event> QueuedEvents = new Queue<Sdl.SDL_Event>();

		public bool PollEvents(out Sdl.SDL_Event e)
		{
			if (IsDisposed )
			{
				if (Game.Quitting)
				{
					e = new Sdl.SDL_Event();
					return false;
				}

				e = new Sdl.SDL_Event {type = Sdl.SDL_QUIT};
				return true;
			}

			if (QueuedEvents.Count == 0)
			{
				e = new Sdl.SDL_Event();
				return false;
			}
			e = QueuedEvents.Dequeue();

			return true;
		}


		private void OnResize(object sender, EventArgs e)
		{
			if (GraphicsDevice.Instance != null)
			{
				ResizeMe();
			}
		}

		private void ResizeMe()
		{
			GraphicsDevice.Instance.WindowSize = Size;
			Gl.glViewport(0, 0, Size.Width, Size.Height);

			if (Game.viewport != null)
			{
				Game.viewport = new Viewport(new int2(Size.Width, Size.Height), Game.viewport.MapStart, Game.viewport.MapEnd, Game.Renderer);

				Widget.ReinitAll();
			}
		}

		#region com
		[ComRegisterFunction()]
		public static void RegisterClass(string key)
		{
			// Strip off HKEY_CLASSES_ROOT\ from the passed key as I don't need it

			StringBuilder sb = new StringBuilder(key);
			sb.Replace(@"HKEY_CLASSES_ROOT\", "");

			// Open the CLSID\{guid} key for write access

			RegistryKey k = Registry.ClassesRoot.OpenSubKey(sb.ToString(), true);

			// And create the 'Control' key - this allows it to show up in 

			// the ActiveX control container 

			RegistryKey ctrl = k.CreateSubKey("Control");
			ctrl.Close();

			// Next create the CodeBase entry - needed if not string named and GACced.

			RegistryKey inprocServer32 = k.OpenSubKey("InprocServer32", true);
			inprocServer32.SetValue("CodeBase", Assembly.GetExecutingAssembly().CodeBase);
			inprocServer32.Close();

			// Finally close the main key

			k.Close();
		}

		[ComUnregisterFunction()]
		public static void UnregisterClass(string key)
		{
			StringBuilder sb = new StringBuilder(key);
			sb.Replace(@"HKEY_CLASSES_ROOT\", "");

			// Open HKCR\CLSID\{guid} for write access

			RegistryKey k = Registry.ClassesRoot.OpenSubKey(sb.ToString(), true);

			// Delete the 'Control' key, but don't throw an exception if it does not exist

			k.DeleteSubKey("Control", false);

			// Next open up InprocServer32

			RegistryKey inprocServer32 = k.OpenSubKey("InprocServer32", true);

			// And delete the CodeBase key, again not throwing if missing 

			k.DeleteSubKey("CodeBase", false);

			// Finally close the main key 

			k.Close();
		}
		#endregion

		private void Renderer_MouseEnter(object sender, EventArgs e)
		{
			System.Windows.Forms.Cursor.Hide();
		}

		private void Renderer_MouseLeave(object sender, EventArgs e)
		{
			System.Windows.Forms.Cursor.Show();
		}

		private void ORAHost_Load(object sender, EventArgs e)
		{

		}

		private void OnClickStart(object sender, EventArgs e)
		{
			timer1.Enabled = true;
		}

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			var ev = new Sdl.SDL_Event();
			ev.type = Sdl.SDL_MOUSEBUTTONDOWN;

			if (e.Button == MouseButtons.Left)
				ev.button.button = Sdl.SDL_BUTTON_LEFT;
			else if (e.Button == MouseButtons.Right)
				ev.button.button = Sdl.SDL_BUTTON_RIGHT;
			else if (e.Button == MouseButtons.Middle)
				ev.button.button = Sdl.SDL_BUTTON_MIDDLE;
			else
				return;

			ev.button.x = (short) e.X;
			ev.button.y = (short) e.Y;
			QueuedEvents.Enqueue(ev);
		}
		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			var ev = new Sdl.SDL_Event();
			ev.type = Sdl.SDL_MOUSEBUTTONUP;

			if (e.Button == MouseButtons.Left)
				ev.button.button = Sdl.SDL_BUTTON_LEFT;
			else if (e.Button == MouseButtons.Right)
				ev.button.button = Sdl.SDL_BUTTON_RIGHT;
			else if (e.Button == MouseButtons.Middle)
				ev.button.button = Sdl.SDL_BUTTON_MIDDLE;


			ev.button.x = (short)e.X;
			ev.button.y = (short)e.Y;
			QueuedEvents.Enqueue(ev);
		}

		private void OnMouseMoved(object sender, MouseEventArgs e)
		{
			var ev = new Sdl.SDL_Event();
			ev.type = Sdl.SDL_MOUSEMOTION;
			ev.motion.x = (short)e.X;
			ev.motion.y = (short)e.Y;
			QueuedEvents.Enqueue(ev);
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			var ev = new Sdl.SDL_Event {type = Sdl.SDL_KEYDOWN};
			if (GetKeySym(e) != 0 && (!((char)e.KeyValue >= '0' && (char)e.KeyValue <= '9') || e.Control))
			{
				ev.key.keysym.unicode = (short)e.KeyValue;
				ev.key.keysym.sym = GetKeySym(e);

				QueuedEvents.Enqueue(ev);
			}
			else if (!IsInputChar((char)e.KeyValue))
				return;

			ev.key.keysym.unicode = (short) e.KeyValue;
			ev.key.keysym.sym = GetKeySym(e);

			QueuedEvents.Enqueue(ev);
		}

		protected override bool IsInputKey(System.Windows.Forms.Keys keyData)
		{
			return true;
		}

		[DllImport("user32.dll")]
		static extern short VkKeyScan(char ch);
		static public Key ResolveKey(char charToResolve)
		{
			return KeyInterop.KeyFromVirtualKey(VkKeyScan(charToResolve));
		}

		public override bool PreProcessMessage(ref Message msg)
		{
			if (msg.Msg == (int)0x102)  // WM_CHAR
			{
				var c = (char)msg.WParam;
				var ev = new Sdl.SDL_Event {type = Sdl.SDL_KEYDOWN};

				ev.key.keysym.unicode = (short)c;
				ev.key.keysym.sym = GetKeySym(ResolveKey(c));

				QueuedEvents.Enqueue(ev);
				return true;
			}

			return base.PreProcessMessage(ref msg);
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			var ev = new Sdl.SDL_Event {type = Sdl.SDL_KEYUP};

			if ((e.KeyValue >= (int)Keys.A && e.KeyValue <= (int)Keys.Z))
			{
				var k = ((char)e.KeyValue).ToString();
				k = e.Shift ? k.ToUpper() : k.ToLower();
				ev.key.keysym.unicode = (short)k[0];
			}
			else
			{
				ev.key.keysym.unicode = (short)e.KeyValue;
			}

			ev.key.keysym.sym = GetKeySym(e);

			QueuedEvents.Enqueue(ev);
		}
		private static int GetKeySym(KeyEventArgs ke)
		{
			return GetKeySym(ke.KeyCode);
		}

		private static int GetKeySym(Key keyCode)
		{
			switch (keyCode)
			{
				case Key.LeftCtrl:
				case Key.RightCtrl:
					return Sdl.SDLK_LCTRL;
				case Key.LeftShift:
				case Key.RightShift:
					return Sdl.SDLK_LSHIFT;
				case Key.LeftAlt:
				case Key.RightAlt:
					return Sdl.SDLK_LALT;
				case Key.Down:
					return Sdl.SDLK_DOWN;
				case Key.Up:
					return Sdl.SDLK_UP;
				case Key.Left:
					return Sdl.SDLK_LEFT;
				case Key.Right:
					return Sdl.SDLK_RIGHT;
				case Key.NumPad0:
				case Key.D0:
					return Sdl.SDLK_0;
				case Key.NumPad1:
				case Key.D1:
					return Sdl.SDLK_1;
				case Key.NumPad2:
				case Key.D2:
					return Sdl.SDLK_2;
				case Key.NumPad3:
				case Key.D3:
					return Sdl.SDLK_3;
				case Key.NumPad4:
				case Key.D4:
					return Sdl.SDLK_4;
				case Key.NumPad5:
				case Key.D5:
					return Sdl.SDLK_5;
				case Key.NumPad6:
				case Key.D6:
					return Sdl.SDLK_6;
				case Key.NumPad7:
				case Key.D7:
					return Sdl.SDLK_7;
				case Key.NumPad8:
				case Key.D8:
					return Sdl.SDLK_8;
				case Key.NumPad9:
				case Key.D9:
					return Sdl.SDLK_9;
				case Key.F1:
					return Sdl.SDLK_F1;
				case Key.F2:
					return Sdl.SDLK_F2;
				case Key.F3:
					return Sdl.SDLK_F3;
				case Key.F4:
					return Sdl.SDLK_F4;
				case Key.F5:
					return Sdl.SDLK_F5;
				case Key.F6:
					return Sdl.SDLK_F6;
				case Key.F7:
					return Sdl.SDLK_F7;
				case Key.F8:
					return Sdl.SDLK_F8;
				case Key.F9:
					return Sdl.SDLK_F9;
				case Key.F10:
					return Sdl.SDLK_F10;
				case Key.F11:
					return Sdl.SDLK_F11;
				case Key.F12:
					return Sdl.SDLK_F12;

				default:
					return 0;
			}
		}
		private static int GetKeySym(Keys keyCode)
		{
			switch (keyCode)
			{
				case Keys.ControlKey:
				case Keys.Control:
					return Sdl.SDLK_LCTRL;
				case Keys.ShiftKey:
				case Keys.Shift:
					return Sdl.SDLK_LSHIFT;
				case Keys.Alt:
					return Sdl.SDLK_LALT;
				case Keys.Down:
					return Sdl.SDLK_DOWN;
				case Keys.Up:
					return Sdl.SDLK_UP;
				case Keys.Left:
					return Sdl.SDLK_LEFT;
				case Keys.Right:
					return Sdl.SDLK_RIGHT;
				case Keys.NumPad0:
				case Keys.D0:
					return Sdl.SDLK_0;
				case Keys.NumPad1:
				case Keys.D1:
					return Sdl.SDLK_1;
				case Keys.NumPad2:
				case Keys.D2:
					return Sdl.SDLK_2;
				case Keys.NumPad3:
				case Keys.D3:
					return Sdl.SDLK_3;
				case Keys.NumPad4:
				case Keys.D4:
					return Sdl.SDLK_4;
				case Keys.NumPad5:
				case Keys.D5:
					return Sdl.SDLK_5;
				case Keys.NumPad6:
				case Keys.D6:
					return Sdl.SDLK_6;
				case Keys.NumPad7:
				case Keys.D7:
					return Sdl.SDLK_7;
				case Keys.NumPad8:
				case Keys.D8:
					return Sdl.SDLK_8;
				case Keys.NumPad9:
				case Keys.D9:
					return Sdl.SDLK_9;
				case Keys.F1:
					return Sdl.SDLK_F1;
				case Keys.F2:
					return Sdl.SDLK_F2;
				case Keys.F3:
					return Sdl.SDLK_F3;
				case Keys.F4:
					return Sdl.SDLK_F4;
				case Keys.F5:
					return Sdl.SDLK_F5;
				case Keys.F6:
					return Sdl.SDLK_F6;
				case Keys.F7:
					return Sdl.SDLK_F7;
				case Keys.F8:
					return Sdl.SDLK_F8;
				case Keys.F9:
					return Sdl.SDLK_F9;
				case Keys.F10:
					return Sdl.SDLK_F10;
				case Keys.F11:
					return Sdl.SDLK_F11;
				case Keys.F12:
					return Sdl.SDLK_F12;

				default:
					return 0;
			}
		}

		private void OnClick(object sender, EventArgs e)
		{
			Game.HasInputFocus = true;

			Focus();
		}

		private void ORAHost_MouseLeave(object sender, EventArgs e)
		{

		}

		private void ORAHost_KeyPress(object sender, KeyPressEventArgs e)
		{

		}

		private void ORAHost_VisibleChanged(object sender, EventArgs e)
		{

		}

		private void ORAHost_ParentChanged(object sender, EventArgs e)
		{

		}

		#region Implementation of IObjectWithSite

		public void SetSite(object pUnkSite)
		{
			throw new NotImplementedException();
		}

		public void GetSite(ref Guid riid, out object ppvSite)
		{
			throw new NotImplementedException();
		}

		#endregion

		private void timer1_Tick(object sender, EventArgs e)
		{

			timer1.Enabled = false;
			button1.Visible = false;
			Application.DoEvents();
			Start();
		}

		private void Ticker_Tick(object sender, EventArgs e)
		{
			if (!Game.Quitting)
				Game.Tick();
		}
	}
}
