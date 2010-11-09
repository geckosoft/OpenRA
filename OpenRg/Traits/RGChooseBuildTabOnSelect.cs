using System.Linq;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRg.Widgets;

namespace OpenRg.Traits
{
	public class RGChooseBuildTabOnSelectInfo : ITraitInfo
	{
		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGChooseBuildTabOnSelect(init);
		}

		#endregion
	}

	public class RGChooseBuildTabOnSelect : INotifySelection
	{
		private readonly World world;

		public RGChooseBuildTabOnSelect(ActorInitializer init)
		{
			world = init.world;
		}

		#region INotifySelection Members

		public void SelectionChanged()
		{
			if (world.LocalPlayer == null)
				return; // spectator

			/* selected a producing fact */
			Actor producer =
				world.Selection.Actors.FirstOrDefault(
					a =>
					a.IsInWorld && a.Owner.Stances[a.World.LocalPlayer] == Stance.Ally &&
					a.TraitOrDefault<RGProductionQueue>() != null);

			Actor perqueue =
				world.Selection.Actors.FirstOrDefault(
					a =>
					a.IsInWorld && a.IsInWorld && a.Owner.Stances[a.World.LocalPlayer] == Stance.Ally &&
					a.HasTrait<RGProductionQueue>());

			if (world.Selection.Actors.Count() > 0)
			{
				if (producer != null && perqueue != null)
				{
					/* Widget.RootWidget.GetWidget<RgBuildPaletteWidget>("INGAME_BUILD_PALETTE").paletteOpen = true;*/
					Widget.RootWidget.GetWidget<RGBuildPaletteWidget>("INGAME_BUILD_PALETTE")
						.SetCurrentTab(
							perqueue.TraitsImplementing<RGProductionQueue>().Where(
								a => a.Info.Type == producer.TraitOrDefault<RGProductionQueue>().Info.Type).First());
				}
				else
				{
					/*    Widget.RootWidget.GetWidget<RgBuildPaletteWidget>("INGAME_BUILD_PALETTE").paletteOpen = false;*/
				}
			}
			if (perqueue != null)
			{
				Widget.RootWidget.GetWidget<RGBuildPaletteWidget>("INGAME_BUILD_PALETTE")
					.SetCurrentTab(perqueue.TraitsImplementing<RGProductionQueue>().First());
				return;
			}
			else
			{
				/*
                if (Widget.RootWidget.GetWidget<RgBuildPaletteWidget>("INGAME_BUILD_PALETTE").paletteOpen)
                {
                    Widget.RootWidget.GetWidget<RgBuildPaletteWidget>("INGAME_BUILD_PALETTE").paletteAnimating = true;
                    Widget.RootWidget.GetWidget<RgBuildPaletteWidget>("INGAME_BUILD_PALETTE").paletteOpen = false;
                }*/
			}
			// Queue-per-structure
			//var perqueue = world.Selection.Actors.FirstOrDefault(
			//     a => a.IsInWorld && a.World.LocalPlayer == a.Owner && a.HasTrait<ProductionQueue>());

			/*
            if (perqueue != null)
            {
                Widget.RootWidget.GetWidget<RgBuildPaletteWidget>("INGAME_BUILD_PALETTE")
                    .SetCurrentTab(perqueue.TraitsImplementing<RgProductionQueue>().First());
                return;
            }

            // Queue-per-player
            var types = world.Selection.Actors.Where(a => a.IsInWorld && (a.World.LocalPlayer == a.Owner))
                                              .SelectMany(a => a.TraitsImplementing<Production>())
                                              .SelectMany(t => t.Info.Produces)
                                              .Distinct();

            if (types.Count() == 0)
                return;


            Widget.RootWidget.GetWidget<RgBuildPaletteWidget>("INGAME_BUILD_PALETTE")
                .SetCurrentTab(world.LocalPlayer.PlayerActor.TraitsImplementing<RgProductionQueue>().FirstOrDefault(t => types.Contains(t.Info.Type)));
        
             */
		}

		#endregion
	}
}