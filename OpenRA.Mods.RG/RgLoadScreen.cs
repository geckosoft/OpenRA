using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Rg
{
	public class RgLoadScreen : ILoadScreen
	{
		private static readonly string[] Comments = new[]
		                                            	{
		                                            		"Contacting EVA ..."
		                                            	};

		private readonly Stopwatch lastLoadScreen = new Stopwatch();
		private SpriteFont Font;
		private Sprite Logo;
		private float2 LogoPos;
		private Sprite Stripe;
		private Rectangle StripeRect;

		private Renderer r;

		#region ILoadScreen Members

		public void Init()
		{
			// Avoid standard loading mechanisms so we
			// can display loadscreen as early as possible
			r = Game.Renderer;
			if (r == null) return;
			Font = r.BoldFont;

			var s = new Sheet("mods/rg/uibits/loadscreen.png");
			Logo = new Sprite(s, new Rectangle(0, 0, 256, 256), TextureChannel.Alpha);
			Stripe = new Sprite(s, new Rectangle(256, 0, 256, 256), TextureChannel.Alpha);
			StripeRect = new Rectangle(0, Renderer.Resolution.Height/2 - 128, Renderer.Resolution.Width, 256);
			LogoPos = new float2(Renderer.Resolution.Width/2 - 128, Renderer.Resolution.Height/2 - 128);
		}


		public void Display()
		{
			if (r == null)
				return;

			// Update text at most every 0.5 seconds
			if (lastLoadScreen.ElapsedTime() < 0.5)
				return;

			lastLoadScreen.Reset();
			string text = Comments.Random(Game.CosmeticRandom);
			int2 textSize = Font.Measure(text);

			r.BeginFrame(float2.Zero);
			WidgetUtils.FillRectWithSprite(StripeRect, Stripe);
			r.RgbaSpriteRenderer.DrawSprite(Logo, LogoPos);
			Font.DrawText(text,
			              new float2(Renderer.Resolution.Width - textSize.X - 20, Renderer.Resolution.Height - textSize.Y - 20),
			              Color.White);
			r.EndFrame();
		}

		#endregion
	}
}