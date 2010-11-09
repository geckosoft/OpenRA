using OpenRA;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRg.Traits
{
	public class RGValidateOrderInfo : ITraitInfo
	{
		#region ITraitInfo Members

		public object Create(ActorInitializer init)
		{
			return new RGValidateOrder(init.self, this);
		}

		#endregion
	}

	public class RGValidateOrder : IValidateOrder
	{
		public readonly Actor Self;

		public RGValidateOrder(Actor self, RGValidateOrderInfo info)
		{
			Info = info;
			Self = self;
		}

		public RGValidateOrderInfo Info { get; protected set; }

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