using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class Utilizator
    {
        public int Id { get; set; }
        public string Nume { get; set; }
        public string Prenume { get; set; }
        public string Email { get; set; }
        public string Telefon { get; set; }
        public string AdresaLivrare { get; set; }
        public string Parola { get; set; }
        public int RolId { get; set; }

        public bool EsteAngajat => RolId == 2;
        public bool EsteClient => RolId == 1;

        public string NumeComplet => $"{Nume} {Prenume}";
    }
}
