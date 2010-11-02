using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgProductionQueueInfo : ITraitInfo
	{
		public readonly int LowPowerSlowdown = 3;
		public readonly string Type;
		public float BuildSpeed = 0.4f;

		#region ITraitInfo Members

		public virtual object Create(ActorInitializer init)
		{
			return new RgProductionQueue(init.self, init.self.Owner.PlayerActor, this);
		}

		#endregion
	}

	public class RgProductionQueue : IResolveOrder, ITick, ITechTreeElement
	{
		private readonly PowerManager PlayerPower;
		private readonly PlayerResources PlayerResources;
		public readonly Actor self;
		public RgProductionQueueInfo Info;

		// TODO: sync these
		// A list of things we are currently building

		// A list of things we could possibly build, even if our race doesn't normally get it
		public Dictionary<ActorInfo, ProductionState> Produceable = new Dictionary<ActorInfo, ProductionState>();
		public List<RgProductionItem> Queue = new List<RgProductionItem>();

		public RgProductionQueue(Actor self, Actor playerActor, RgProductionQueueInfo info)
		{
			this.self = self;
			Info = info;
			PlayerResources = playerActor.Trait<PlayerResources>();
			PlayerPower = playerActor.Trait<PowerManager>();

			var ttc = playerActor.Trait<TechTree>();

			foreach (ActorInfo a in AllBuildables(Info.Type))
			{
				var bi = a.Traits.Get<BuildableInfo>();
				// Can our race build this by satisfying normal prereqs?
				bool buildable = bi.Owner.Contains(self.Owner.Country.Race);
				Produceable.Add(a, new ProductionState {Visible = buildable && !bi.Hidden});
				if (buildable)
					ttc.Add(a.Name, a.Traits.Get<BuildableInfo>().Prerequisites.ToList(), this);
			}
		}

		#region IResolveOrder Members

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "RgStartProduction":
					{
						ActorInfo unit = Rules.Info[order.TargetString];
						var bi = unit.Traits.Get<BuildableInfo>();
						if (bi.Queue != Info.Type)
							return; /* Not built by this queue */

						int cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
						int time = GetBuildTime(order.TargetString);

						var power = self.Owner.PlayerActor.Trait<PowerManager>();
						bool lowpower = power.PowerState != PowerState.Normal;
						if (lowpower)
							cost *= 2;

						if (!BuildableItems().Any(b => b.Name == order.TargetString))
							return; /* you can't build that!! */

						bool hasPlayedSound = false;

						if (order.TargetActor.TraitOrDefault<PlayerResources>().Ore +
						    order.TargetActor.TraitOrDefault<PlayerResources>().Cash < cost)
							break; /* cant pay for it! */

						/* give money to cpu! */
						order.TargetActor.TraitOrDefault<RgPlayer>().ParentResources.GiveCash(cost);
						/* take it from the player */
						order.TargetActor.TraitOrDefault<PlayerResources>().TakeCash(cost);

						BeginProduction(new RgProductionItem(this, order.TargetString, time, cost,
						                                     () => self.World.AddFrameEndTask(
						                                     	_ =>
						                                     		{
						                                     			bool isBuilding = unit.Traits.Contains<BuildingInfo>();
						                                     			if (!hasPlayedSound)
						                                     			{
						                                     				var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
						                                     				Sound.PlayToPlayer(order.Player,
						                                     				                   isBuilding
						                                     				                   	? eva.BuildingReadyAudio
						                                     				                   	: eva.UnitReadyAudio);
						                                     				hasPlayedSound = true;
						                                     			}
						                                     			if (!isBuilding)
						                                     			{
						                                     				BuildUnit(order.TargetString, order);
						                                     			}
						                                     		})));
						break;
					}
					/*case "PauseProduction":
                    {
                        if (Queue.Count > 0 && Queue[0].Item == order.TargetString)
                            Queue[0].Paused = (order.TargetLocation.X != 0);
                        break;
                    }
                case "CancelProduction":
                    {
                        CancelProduction(order.TargetString, order.TargetLocation.X);
                        break;
                    }*/
			}
		}

		#endregion

		#region ITechTreeElement Members

		public void PrerequisitesAvailable(string key)
		{
			ProductionState ps = Produceable[Rules.Info[key]];
			if (!ps.Sticky)
				ps.Buildable = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			ProductionState ps = Produceable[Rules.Info[key]];
			if (!ps.Sticky)
				ps.Buildable = false;
		}

		#endregion

		#region ITick Members

		public virtual void Tick(Actor self)
		{
			while (Queue.Count > 0 && (!Queue[0].Force && !BuildableItems().Any(b => b.Name == Queue[0].Item)))
			{
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(Queue[0].TotalCost - Queue[0].RemainingCost);
					// refund what's been paid so far.
				FinishProduction();
			}
			if (Queue.Count > 0)
				Queue[0].Tick(PlayerResources, PlayerPower);
		}

		#endregion

		private IEnumerable<ActorInfo> AllBuildables(string category)
		{
			return Rules.Info.Values
				.Where(x => x.Name[0] != '^')
				.Where(x => x.Traits.Contains<BuildableInfo>())
				.Where(x => x.Traits.Get<BuildableInfo>().Queue == category);
		}

		public void OverrideProduction(ActorInfo type, bool buildable)
		{
			Produceable[type].Buildable = buildable;
			Produceable[type].Sticky = true;
		}

		public RgProductionItem CurrentItem()
		{
			return Queue.ElementAtOrDefault(0);
		}

		public IEnumerable<RgProductionItem> AllQueued()
		{
			return Queue;
		}

		public virtual IEnumerable<ActorInfo> AllItems()
		{
			if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().AllTech)
				return Produceable.Select(a => a.Key);

			return Produceable.Where(a => a.Value.Buildable || a.Value.Visible).Select(a => a.Key);
		}

		public virtual IEnumerable<ActorInfo> BuildableItems()
		{
			if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().AllTech)
				return Produceable.Select(a => a.Key);

			return Produceable.Where(a => a.Value.Buildable).Select(a => a.Key);
		}

		public bool CanBuild(ActorInfo actor)
		{
			return Produceable.ContainsKey(actor) && Produceable[actor].Buildable;
		}

		public int GetBuildTime(String unitString)
		{
			ActorInfo unit = Rules.Info[unitString];
			if (unit == null || !unit.Traits.Contains<BuildableInfo>())
				return 0;

			if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().FastBuild)
				return 0;
			int cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
			float time = cost
			             *Info.BuildSpeed
			             *(25*60) /* frames per min */ /* todo: build acceleration, if we do that */
			             /1000;
			return (int) time;
		}

		protected void CancelProduction(string itemName, int numberToCancel)
		{
			for (int i = 0; i < numberToCancel; i++)
				CancelProductionInner(itemName);
		}

		private void CancelProductionInner(string itemName)
		{
			int lastIndex = Queue.FindLastIndex(a => a.Item == itemName);

			if (lastIndex > 0)
				Queue.RemoveAt(lastIndex);
			else if (lastIndex == 0)
			{
				RgProductionItem item = Queue[0];
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(
					item.TotalCost - item.RemainingCost); // refund what has been paid
				FinishProduction();
			}
		}

		public void FinishProduction()
		{
			if (Queue.Count == 0) return;
			Queue.RemoveAt(0);
		}

		public void BeginProduction(RgProductionItem item)
		{
			Queue.Add(item);
		}

		protected static bool IsDisabledBuilding(Actor a)
		{
			return a.TraitsImplementing<IDisable>().Any(d => d.Disabled);
		}

		// Builds a unit from the actor that holds this queue (1 queue per building)
		internal virtual Actor BuildUnit(string name, Order order)
		{
			// Cannot produce if i'm dead
			if (!self.IsInWorld || self.IsDead())
			{
				CancelProduction(name, 1);
				return null;
			}

			RgProduction sp =
				self.TraitsImplementing<RgProduction>().Where(p => p.Info.Produces.Contains(Info.Type)).FirstOrDefault();


			if (sp != null && !IsDisabledBuilding(self))
			{
				Actor a = sp.Produce(self, Rules.Info[name], order);
				if (a != null)
					FinishProduction();

				return a;
			}

			return null;
		}
	}

	public class RgProductionItem
	{
		public readonly bool Force;
		public readonly string Item;
		public readonly RgProductionQueue Queue;
		public readonly int TotalCost;
		public readonly int TotalTime;
		public bool Done;
		public Action OnComplete;
		public bool Paused;
		private int slowdown;

		public RgProductionItem(RgProductionQueue queue, string item, int time, int cost, Action onComplete)
		{
			if (time <= 0) time = 1;
			Item = item;
			RemainingTime = TotalTime = time;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
			Queue = queue;

			//Log.Write("debug", "new ProductionItem: {0} time={1} cost={2}", item, time, cost);
		}

		public RgProductionItem(RgProductionQueue queue, string item, int time, int cost, Action onComplete, bool force)
		{
			if (time <= 0) time = 1;
			Item = item;
			RemainingTime = TotalTime = time;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
			Queue = queue;
			Force = true;
			//Log.Write("debug", "new ProductionItem: {0} time={1} cost={2}", item, time, cost);
		}

		public int RemainingTime { get; private set; }
		public int RemainingCost { get; private set; }

		public void Tick(PlayerResources pr, PowerManager pm)
		{
			if (Done)
			{
				if (OnComplete != null) OnComplete();
				return;
			}

			if (Paused) return;

			if (pm.PowerState != PowerState.Normal)
			{
				if (--slowdown <= 0)
					slowdown = Queue.Info.LowPowerSlowdown;
				else
					return;
			}

			int costThisFrame = RemainingCost/RemainingTime;
			if (costThisFrame != 0 && !pr.TakeCash(costThisFrame)) return;
			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if (RemainingTime > 0) return;

			Done = true;
		}
	}
}