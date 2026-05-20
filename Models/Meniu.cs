using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class Meniu
    {
        public int Id { get; set; }
        public string Denumire { get; set; }
        public string CategorieDenumire { get; set; }
        public string DetaliiPreparate { get; set; }
        public decimal PretFaraReducere { get; set; }
        public int CategorieId { get; set; }

        public decimal PretFinal(decimal discountX) => PretFaraReducere * (1 - discountX / 100);

        public bool EsteDisponibil { get; set; } = true; 
    }
}
