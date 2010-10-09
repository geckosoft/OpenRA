using SharpLua.Attributes;

namespace OpenRA.Mods.RA.AI.Proxies
{
    [LuaObject("ProductionItem")]
    public class ProductionItemProxy : IProxy
    {
        protected ProductionItem Field = null;
        public string ObjectType { get { return "ProductionItem"; } }

        public ProductionItemProxy(ProductionItem field)
        {
            Field = field;
        }

        [LuaFunction("getItem", RequireObject = true)]
        public static string GetName(ProductionItemProxy self)
        {
            return self.Field.Item;
        }

        public static implicit operator ProductionItem(ProductionItemProxy d)
        {
            return d.Field;
        }

        public static implicit operator ProductionItemProxy(ProductionItem d)
        {
            return new ProductionItemProxy(d);
        }
    }
}
