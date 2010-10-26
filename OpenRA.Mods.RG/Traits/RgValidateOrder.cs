using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
	public class RgValidateOrderInfo : ITraitInfo
	{
		/* only because we have other ctors */

		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RgValidateOrder(init.self, this);
		}

		#endregion
	}

	public class RgValidateOrder : IValidateOrder
	{
		private Actor self;

		public RgValidateOrder(Actor self, RgValidateOrderInfo info)
		{
			Info = info;
			this.self = self;
		}

		public RgValidateOrderInfo Info { get; protected set; }

		#region IValidateOrder Members

		public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
		{
			// @todo Drop exploiting orders
			/*
            if (order.Subject != null && order.Subject.Owner.ClientIndex != clientId)
            {
                Game.Debug("Detected exploit order from {0}: {1}".F(clientId, order.OrderString));
                return false;
            }
            */
			return true;
		}

		#endregion
	}
}