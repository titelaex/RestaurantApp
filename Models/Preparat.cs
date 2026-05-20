using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class Preparat
    {
        public int Id { get; set; }
        public string Denumire { get; set; }
        public decimal Pret { get; set; }
        public int CantitatePortie { get; set; } 
        public int CantitateTotala { get; set; } 
        public int CategorieId { get; set; }

        public string CategorieDenumire { get; set; }

        public string ListaAlergeni { get; set; }

        public string TextAfisareAlergeni => string.IsNullOrEmpty(ListaAlergeni) ? "" : $"Alergeni: {ListaAlergeni}";
        public bool EsteEpuizat => CantitateTotala <= 0; 

        public override string ToString()
        {
            return $"{Denumire} - {CantitatePortie}g - {Pret} RON";
        }
    }
}
