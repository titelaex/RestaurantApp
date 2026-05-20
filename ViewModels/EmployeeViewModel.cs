using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using RestaurantApp.Core;
using RestaurantApp.Models;

namespace RestaurantApp.ViewModels
{
    public class EmployeeViewModel : ObservableObject
    {
        private string _connString = System.Configuration.ConfigurationManager.ConnectionStrings["RestaurantDb"].ConnectionString;

        public ObservableCollection<DataRowView> ToateComenzile { get; set; } = new ObservableCollection<DataRowView>();
        public ObservableCollection<DataRowView> ProduseEpuizate { get; set; } = new ObservableCollection<DataRowView>();

        public string[] OptiuniStare { get; } = { "inregistrata", "se pregateste", "a plecat la client", "livrata", "anulata" };

        public RelayCommand ActualizeazaStareCommand { get; }

        public EmployeeViewModel()
        {
            IncarcaDate();
            ActualizeazaStareCommand = new RelayCommand(ExecuteActualizeazaStare);
        }

        private void IncarcaDate()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();

                    // Incarca Comenzi
                    using (SqlCommand cmd = new SqlCommand("sp_GetComenziAngajat", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        DataTable dtComenzi = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtComenzi); }
                        ToateComenzile.Clear();
                        foreach (DataRow row in dtComenzi.Rows) ToateComenzile.Add(dtComenzi.DefaultView[dtComenzi.Rows.IndexOf(row)]);
                    }

                    // Incarca Produse Epuizate
                    int limita = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["CantitateLimitaEpuizareC"] ?? "10");
                    using (SqlCommand cmd = new SqlCommand("sp_GetPreparateEpuizate", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Limita", limita);
                        DataTable dtEpuizate = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtEpuizate); }
                        ProduseEpuizate.Clear();
                        foreach (DataRow row in dtEpuizate.Rows) ProduseEpuizate.Add(dtEpuizate.DefaultView[dtEpuizate.Rows.IndexOf(row)]);
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void ExecuteActualizeazaStare(object parameter)
        {
            if (parameter is DataRowView row)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("sp_UpdateStareComanda", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ComandaId", (int)row["Id"]);
                            cmd.Parameters.AddWithValue("@NouaStare", row["Stare"].ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    System.Windows.MessageBox.Show("Stare actualizată!");
                    IncarcaDate(); // Refresh
                }
                catch (Exception ex) { System.Windows.MessageBox.Show("Eroare: " + ex.Message); }
            }
        }
    }
}
