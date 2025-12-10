namespace Amur_store.Models
{
    public partial class Order
    {
        // Свойство для определения, можно ли отменить заказ
        public bool CanCancel { get; set; }
    }
}