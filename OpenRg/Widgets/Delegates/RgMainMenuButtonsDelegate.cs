using System.IO;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Server;
using OpenRA.Widgets;

namespace OpenRg.Widgets.Delegates
{
	public class RGMainMenuButtonsDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public RGMainMenuButtonsDelegate([ObjectCreator.ParamAttribute] Widget widget)
		{
			// Main menu is the default window
			widget.GetWidget("MAINMENU_BUTTON_JOIN").OnMouseUp = mi =>
			                                                     	{
			                                                     		Widget.OpenWindow("JOINSERVER_BG");
			                                                     		return true;
			                                                     	};
			widget.GetWidget("MAINMENU_BUTTON_CREATE").OnMouseUp = mi =>
			                                                       	{
			                                                       		Widget.OpenWindow("CREATESERVER_BG");
			                                                       		return true;
			                                                       	};
			widget.GetWidget("MAINMENU_BUTTON_SETTINGS").OnMouseUp = mi =>
			                                                         	{
			                                                         		Widget.OpenWindow("SETTINGS_MENU");
			                                                         		return true;
			                                                         	};
			// TODO Add music in? // widget.GetWidget( "MAINMENU_BUTTON_MUSIC" ).OnMouseUp = mi => { Widget.OpenWindow( "MUSIC_MENU" ); return true; };
			widget.GetWidget("MAINMENU_BUTTON_QUIT").OnMouseUp = mi =>
			                                                     	{
			                                                     		Game.Exit();
			                                                     		return true;
			                                                     	};

			var version = widget.GetWidget<LabelWidget>("VERSION_STRING");

			if (FileSystem.Exists("VERSION"))
			{
				Stream s = FileSystem.Open("VERSION");
				string versionFileContent = s.ReadAllText();
				version.Text = versionFileContent;
				s.Close();

				MasterServerQuery.OnVersion += v =>
				                               	{
				                               		if (!string.IsNullOrEmpty(v))
				                               			version.Text = versionFileContent + "\nLatest: " + v;
				                               	};
				MasterServerQuery.GetCurrentVersion(Game.Settings.Server.MasterServer);
			}
			else
			{
				version.Text = "OpenRg: Development Build";
			}
			MasterServerQuery.ClientVersion = version.Text;

			MasterServerQuery.GetMOTD(Game.Settings.Server.MasterServer);
		}
	}
}