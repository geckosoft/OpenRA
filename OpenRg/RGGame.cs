using System.Drawing;
using OpenRA;
using OpenRg.Traits;

namespace OpenRg
{
	public static class RGGame
	{
		/// <summary>
		/// Shows an EVA message
		/// </summary>
		/// <param name="message">The message to show</param>
		public static void EVA(string message)
		{
			Game.AddChatLine(Color.OrangeRed, "EVA", message);
		}

		public static RGPlayer LocalPlayer { get; set; }
	}
}
