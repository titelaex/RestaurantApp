using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestaurantApp.Models;

namespace RestaurantApp.Core
{
    public static class SesiuneCurenta
    {
        public static Utilizator UtilizatorLogat { get; set; } = null;

        public static bool EsteLogat => UtilizatorLogat != null;

        public static void Delogare()
        {
            UtilizatorLogat = null;
        }
    }
}
