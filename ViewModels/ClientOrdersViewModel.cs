using System;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Windows;
using RestaurantApp.Core;

namespace RestaurantApp.ViewModels
{
    public class ClientOrdersViewModel : ObservableObject
    {
        private string _connString = ConfigurationManager.ConnectionStrings["RestaurantDb"].ConnectionString;

        public ObservableCollection<DataRowView> IndexComenzi { get; set; } = new ObservableCollection<DataRowView>();

        public RelayCommand AnuleazaComandaCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public ClientOrdersViewModel()
        {
            AnuleazaComandaCommand = new RelayCommand(ExecuteAnuleazaComanda);
            RefreshCommand = new RelayCommand(obj => IncarcaComenzi());
            IncarcaComenzi();
        }

        private void IncarcaComenzi()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_GetComenziClient", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClientId", SesiuneCurenta.UtilizatorLogat.Id);

                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dt); }

                        IndexComenzi.Clear();
                        foreach (DataRow row in dt.Rows)
                        {
                            IndexComenzi.Add(dt.DefaultView[dt.Rows.IndexOf(row)]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la încărcarea istoricului: " + ex.Message);
            }
        }

        private void ExecuteAnuleazaComanda(object parameter)
        {
            if (parameter is DataRowView row)
            {
                var confirmare = MessageBox.Show("Sigur dorești să anulezi această comandă?", "Confirmare Anulare", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmare == MessageBoxResult.Yes)
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
                                cmd.Parameters.AddWithValue("@NouaStare", "anulata");
                                cmd.ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Comanda a fost anulată cu succes!");
                        IncarcaComenzi(); 
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Eroare la anulare: " + ex.Message);
                    }
                }
            }
        }
    }
}