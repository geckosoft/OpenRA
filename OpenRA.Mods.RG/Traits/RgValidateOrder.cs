using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Rg.Traits
{
    public class RgValidateOrderInfo : ITraitInfo
    {
        public RgValidateOrderInfo() { }		/* only because we have other ctors */

        public object Create(ActorInitializer init) { return new RgValidateOrder(init.self, this); }
    }

    public class RgValidateOrder : IValidateOrder
    {
        Actor self;
        public RgValidateOrderInfo Info { get; protected set; }

        public RgValidateOrder(Actor self, RgValidateOrderInfo info)
		{
			this.Info = info;
			this.self = self;
		}

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
    }
}
