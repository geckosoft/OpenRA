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
using System.Drawing;
using System.Windows.Forms;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class TextFieldWidget : Widget
	{
		public string Text;
		public int MaxLength = 0;
		public bool Bold = false;
		public int VisualHeight = 1;
		public Func<bool> OnEnterKey = () => false;
		public Func<bool> OnTabKey = () => false;
		public Action OnLoseFocus = () => { };
		public int CursorPosition { get; protected set; }
		public string PasswordChar = "";
		protected bool FirstFocusRender = true;

		public TextFieldWidget()
			: base()
		{
			SetText(Text);
		}

		protected TextFieldWidget(TextFieldWidget widget)
			: base(widget)
		{
			SetText(widget.Text);
			MaxLength = widget.MaxLength;
			Bold = widget.Bold;
			VisualHeight = widget.VisualHeight;
		}

		public override bool LoseFocus(MouseInput mi)
		{
			OnLoseFocus();
			var lose = base.LoseFocus(mi);
			return lose;
		}

		public void SetText(string text)
		{
			Text = text ?? "";
			CursorPosition = Text.Length;
		}

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				return false;

			// Lose focus if they click outside the box; return false so the next widget can grab this event
			if (mi.Event == MouseInputEvent.Down && !RenderBounds.Contains(mi.Location.X, mi.Location.Y) && LoseFocus(mi))
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			blinkCycle = 10;
			showCursor = true;
			return true;
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up) return false;
			
			// Only take input if we are focused
			if (!Focused)
				return false;

			if (e.KeyChar == '\r' && OnEnterKey())
				return true;

			if (e.KeyChar == '\t' && OnTabKey())
				return true;

			// ctrl-v => paste
			if ((e.KeyChar == 22 || e.KeyName == "v") && e.Modifiers == Modifiers.Ctrl)
			{
				if (Clipboard.ContainsText())
				{
					var text = Text;
					var clipText = Clipboard.GetText();

					Text = text.Substring(0, CursorPosition) + clipText;
					
					if (text.Length > CursorPosition + 1)
					{
						Text += text.Substring(CursorPosition + 1);
					}

					CursorPosition += clipText.Length;
				}

				return true;
			}

			if (e.KeyName == "left")
			{
				if (CursorPosition > 0)
					CursorPosition--;

				return true;
			}

			if (e.KeyName == "right")
			{
				if (CursorPosition <= Text.Length-1)
					CursorPosition++;

				return true;
			}

			if (e.KeyName == "delete")
			{
				if (Text.Length > 0 && CursorPosition < Text.Length)
				{
					Text = Text.Remove(CursorPosition, 1);
				}
				return true;
			}

			TypeChar(e.KeyChar);
			return true;
		}

		public void TypeChar(char c)
		{
			// backspace
			if (c == '\b' || c == 0x7f)
			{
				if (Text.Length > 0 && CursorPosition > 0)
				{
					Text = Text.Remove(CursorPosition - 1, 1);

					CursorPosition--;
				}
			}
			else if (!char.IsControl(c))
			{
				if (MaxLength > 0 && Text.Length >= MaxLength)
					return;

				Text = Text.Insert(CursorPosition, c.ToString());

				CursorPosition++;
			}
		}

		protected int blinkCycle = 10;
		protected bool showCursor = true;
		public override void Tick()
		{
			if (--blinkCycle <= 0)
			{
				blinkCycle = 20;
				showCursor ^= true;
			}

			base.Tick();
		}

		public override void DrawInner( WorldRenderer wr )
		{
			int margin = 5;
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var pos = RenderOrigin;
			var text = Text;

			if (FirstFocusRender && Focused) // workaround 
			{
				FirstFocusRender = false;
				SetText(Text);
			}

			if (CursorPosition > Text.Length)
				CursorPosition = Text.Length;

			if (!string.IsNullOrEmpty(PasswordChar)) text = new string(PasswordChar[0], text.Length);
			text = ((showCursor && Focused)? text.Insert(CursorPosition, "|"): (Focused) ? text.Insert(CursorPosition, " ") : text) ?? "";


			var textSize = font.Measure(text + "|");

			WidgetUtils.DrawPanel("dialog3",
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height));

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(margin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Right align when editing and scissor when the text overflows
			if (textSize.X > Bounds.Width - 2 * margin)
			{
				if (Focused)
					textPos += new int2(Bounds.Width - 2 * margin - textSize.X, 0);

				Game.Renderer.EnableScissor(pos.X + margin, pos.Y, Bounds.Width - 2 * margin, Bounds.Bottom);
			}

			font.DrawText(text, textPos, Color.White);

			if (textSize.X > Bounds.Width - 2 * margin)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new TextFieldWidget(this); }
	}
}