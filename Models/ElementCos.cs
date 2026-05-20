using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestaurantApp.Core;

namespace RestaurantApp.Models
{
    public class ElementCos : ObservableObject
    {
        public Preparat Preparat { get; set; }

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

        public decimal Subtotal => Preparat.Pret * Cantitate;
    }
}
