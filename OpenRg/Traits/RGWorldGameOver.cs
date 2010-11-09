using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	class RGWorldGameOverInfo : TraitInfo<RGWorldGameOver>
	{
		public readonly bool ReturnToLobby = false;
		public readonly int Delay = 30;
		public readonly string EndMessage = "The game will end in {0} seconds.";
	}

	class RGWorldGameOver : ITick
	{
		public RGWorldGameOverInfo Info = null;
		int? _delayLeft = null;

		public void Tick(Actor self)
		{
			if (self.World.LobbyInfo.Clients.Count == 0)
				return;
			if (Info == null)
				Info = self.Info.Traits.GetOrDefault<RGWorldGameOverInfo>();

			if (Info == null || !Info.ReturnToLobby)
				return;

			// See if there is anyone left who hasnt won / lost (if not, means game over)
			if (!self.World.players.Any(p => !p.Value.NonCombatant && p.Value.WinState == WinState.Undefined))
			{
				if (_delayLeft == null)
				{
					_delayLeft = Info.Delay * 25;
					if (Info.EndMessage.Trim() != "" && Info.EndMessage.Contains("{0}"))
						Game.AddChatLine(Color.Green, "",
							Info.EndMessage.Trim().F(((int)(_delayLeft / 25)) + ""));
					else if (Info.EndMessage.Trim() != "")
						Game.AddChatLine(Color.Green, "", Info.EndMessage.Trim());
				}

				_delayLeft--;

				if (_delayLeft <= 0)
					Game.RejoinLobby(self.World);
			}
		}
	}
}
