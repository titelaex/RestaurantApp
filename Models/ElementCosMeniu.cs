using RestaurantApp.Core;

namespace RestaurantApp.Models
{
    public class ElementCosMeniu : ObservableObject
    {
        public Meniu Meniu { get; set; }

        private int _cantitate;
        public int Cantitate
        {
            get => _cantitate;
            set
            {
                _cantitate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal GetSubtotal(decimal discountX) => Meniu.PretFinal(discountX) * Cantitate;
        
        // Pentru afisare simplificata fara a trimite discountX constant in binding
        public decimal PretUnitarAfisat { get; set; }
        public decimal Subtotal => PretUnitarAfisat * Cantitate;
    }
}
