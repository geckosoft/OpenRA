using System.Drawing;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRg
{
	public class RGLoadScreen : ILoadScreen
	{
		private static readonly string[] Comments = new[]
		                                            	{
		                                            		"Contacting EVA ..."
		                                            	};

		private readonly Stopwatch _lastLoadScreen = new Stopwatch();
		private SpriteFont _font;
		private Sprite _logo;
		private float2 _logoPos;
		private Sprite _stripe;
		private Rectangle _stripeRect;
		private Renderer _renderer;

		#region ILoadScreen Members

		public void Init()
		{
			_renderer = Game.Renderer;
			if (_renderer == null) return;

			_font = _renderer.BoldFont;

			var s = new Sheet("mods/rg/uibits/loadscreen.png");
			_logo = new Sprite(s, new Rectangle(0, 0, 256, 256), TextureChannel.Alpha);
			_stripe = new Sprite(s, new Rectangle(256, 0, 256, 256), TextureChannel.Alpha);
			_stripeRect = new Rectangle(0, Renderer.Resolution.Height / 2 - 128, Renderer.Resolution.Width, 256);
			_logoPos = new float2(Renderer.Resolution.Width / 2 - 128, Renderer.Resolution.Height / 2 - 128);
		}

		public void Display()
		{
			if (_renderer == null)
				return;

			// Update text at most every 0.5 seconds
			if (_lastLoadScreen.ElapsedTime() < 0.5)
				return;

			_lastLoadScreen.Reset();
			string text = Comments.Random(Game.CosmeticRandom);
			int2 textSize = _font.Measure(text);

			_renderer.BeginFrame(float2.Zero);
			WidgetUtils.FillRectWithSprite(_stripeRect, _stripe);
			_renderer.RgbaSpriteRenderer.DrawSprite(_logo, _logoPos);
			_font.DrawText(text,
						  new float2(Renderer.Resolution.Width - textSize.X - 20, Renderer.Resolution.Height - textSize.Y - 20),
						  Color.White);
			_renderer.EndFrame(new NullInputHandler());
		}

		#endregion
	}
}