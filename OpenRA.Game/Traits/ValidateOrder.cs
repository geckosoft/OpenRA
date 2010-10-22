using System;
using System.Collections.Generic;
using OpenRA.Network;

namespace OpenRA.Traits
{
    public class ValidateOrderInfo : ITraitInfo
    {
        public ValidateOrderInfo() { }		/* only because we have other ctors */

        public object Create(ActorInitializer init) { return new ValidateOrder(init.self, this); }
    }

    public class ValidateOrder : IValidateOrder
    {
        Actor self;
        public ValidateOrderInfo Info { get; protected set; }

        public ValidateOrder(Actor self, ValidateOrderInfo info)
		{
			this.Info = info;
			this.self = self;
		}
        public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
        {            
            // Drop exploiting orders
            if (order.Subject != null && order.Subject.Owner.ClientIndex != clientId)
            {
                Game.Debug("Detected exploit order from {0}: {1}".F(clientId, order.OrderString));
                return false;
            }

            return true;
        }
    }
}
