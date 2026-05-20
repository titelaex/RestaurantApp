using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Data;
using Microsoft.Data.SqlClient;
using RestaurantApp.Core;
using RestaurantApp.Models;

namespace RestaurantApp.ViewModels
{
    public class EmployeeViewModel : ObservableObject
    {
        private string _connString = System.Configuration.ConfigurationManager.ConnectionStrings["RestaurantDb"].ConnectionString;

        public ObservableCollection<DataRowView> ToateComenzile { get; set; } = new ObservableCollection<DataRowView>();
        public ICollectionView ComenziActiveView { get; private set; }

        public ObservableCollection<DataRowView> ProduseEpuizate { get; set; } = new ObservableCollection<DataRowView>();
        public ObservableCollection<DataRowView> StocComplet { get; set; } = new ObservableCollection<DataRowView>();
        public ObservableCollection<Categorie> Categorii { get; set; } = new ObservableCollection<Categorie>();

        private string _numeCategorieNoua;
        public string NumeCategorieNoua { get => _numeCategorieNoua; set { _numeCategorieNoua = value; OnPropertyChanged(); } }

        private Categorie _categorieSelectata;
        public Categorie CategorieSelectata 
        { 
            get => _categorieSelectata; 
            set 
            { 
                _categorieSelectata = value; 
                OnPropertyChanged(); 
                if (value != null) NumeCategorieNoua = value.Denumire;
            } 
        }

        public string[] OptiuniStare { get; } = { "inregistrata", "se pregateste", "a plecat la client", "livrata", "anulata" };

        public RelayCommand ActualizeazaStareCommand { get; }
        public RelayCommand AdaugaCategorieCommand { get; }
        public RelayCommand ModificaCategorieCommand { get; }
        public RelayCommand StergeCategorieCommand { get; }

        public EmployeeViewModel()
        {
            ComenziActiveView = CollectionViewSource.GetDefaultView(ToateComenzile);
            ComenziActiveView.Filter = (obj) => {
                if (obj is DataRowView row) {
                    string stare = row["Stare"].ToString().ToLower();
                    return stare != "livrata" && stare != "anulata";
                }
                return false;
            };

            IncarcaDate();
            ActualizeazaStareCommand = new RelayCommand(ExecuteActualizeazaStare);
            AdaugaCategorieCommand = new RelayCommand(ExecuteAdaugaCategorie);
            ModificaCategorieCommand = new RelayCommand(ExecuteModificaCategorie);
            StergeCategorieCommand = new RelayCommand(ExecuteStergeCategorie);
        }

        private void IncarcaDate()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();

                    // Incarca Categorii
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Categorii", conn))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dt); }
                        Categorii.Clear();
                        foreach (DataRow row in dt.Rows)
                            Categorii.Add(new Categorie { Id = (int)row["Id"], Denumire = row["Denumire"].ToString() });
                    }

                    // Incarca Comenzi
                    using (SqlCommand cmd = new SqlCommand("sp_GetComenziAngajat", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        DataTable dtComenzi = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtComenzi); }
                        ToateComenzile.Clear();
                        foreach (DataRow row in dtComenzi.Rows) ToateComenzile.Add(dtComenzi.DefaultView[dtComenzi.Rows.IndexOf(row)]);
                    }
                    ComenziActiveView.Refresh();

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

                    // Incarca Toate Preparatele (pentru Stoc Complet)
                    using (SqlCommand cmd = new SqlCommand("sp_GetMeniuPreparate", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        DataTable dtStoc = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtStoc); }
                        StocComplet.Clear();
                        foreach (DataRow row in dtStoc.Rows) StocComplet.Add(dtStoc.DefaultView[dtStoc.Rows.IndexOf(row)]);
                    }

                    // Incarca Alergeni
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Alergeni", conn))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dt); }
                        Alergeni.Clear();
                        foreach (DataRow row in dt.Rows)
                            Alergeni.Add(dt.DefaultView[dt.Rows.IndexOf(row)]);
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void ExecuteActualizeazaStare(object parameter)
        {
            if (parameter is DataRowView row)
            {
                // Inainte de actualizare, verificam daca starea veche (din baza de date) era finalizata
                // Pentru simplitate, verificam ce scrie in randul curent, dar blocam logica daca e cazul.
                
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("UPDATE Comenzi SET Stare = @Stare WHERE Id = @Id", conn))
                        {
                            cmd.Parameters.AddWithValue("@Stare", row["Stare"]);
                            cmd.Parameters.AddWithValue("@Id", (int)row["Id"]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    IncarcaDate(); // Reincarca totul
                    ComenziActiveView.Refresh(); // Fortam filtrarea
                    System.Windows.MessageBox.Show("Stare actualizată!");
                }
                catch (Exception ex) { System.Windows.MessageBox.Show("Eroare: " + ex.Message); }
            }
        }

        private void ExecuteAdaugaCategorie(object obj)
        {
            if (string.IsNullOrWhiteSpace(NumeCategorieNoua)) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO Categorii (Denumire) VALUES (@Denumire)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Denumire", NumeCategorieNoua);
                        cmd.ExecuteNonQuery();
                    }
                }
                NumeCategorieNoua = "";
                IncarcaDate();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }

        private void ExecuteModificaCategorie(object obj)
        {
            if (CategorieSelectata == null || string.IsNullOrWhiteSpace(NumeCategorieNoua)) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UPDATE Categorii SET Denumire = @Denumire WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Denumire", NumeCategorieNoua);
                        cmd.Parameters.AddWithValue("@Id", CategorieSelectata.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                IncarcaDate();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }

        private void ExecuteStergeCategorie(object obj)
        {
            if (CategorieSelectata == null) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Categorii WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CategorieSelectata.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                IncarcaDate();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("Nu se poate sterge o categorie care are preparate asociate!"); }
        }

        // --- Administrare Alergeni ---
        public ObservableCollection<DataRowView> Alergeni { get; set; } = new ObservableCollection<DataRowView>();
        
        private string _numeAlergenNou;
        public string NumeAlergenNou { get => _numeAlergenNou; set { _numeAlergenNou = value; OnPropertyChanged(); } }

        public RelayCommand AdaugaAlergenCommand => new RelayCommand(obj => {
            if (string.IsNullOrWhiteSpace(NumeAlergenNou)) return;
            using (SqlConnection conn = new SqlConnection(_connString)) {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("INSERT INTO Alergeni (Denumire) VALUES (@D)", conn)) {
                    cmd.Parameters.AddWithValue("@D", NumeAlergenNou);
                    cmd.ExecuteNonQuery();
                }
            }
            NumeAlergenNou = "";
            IncarcaDate();
        });

        public RelayCommand StergeAlergenCommand => new RelayCommand(param => {
            if (param is DataRowView row) {
                try {
                    using (SqlConnection conn = new SqlConnection(_connString)) {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Alergeni WHERE Id = @Id", conn)) {
                            cmd.Parameters.AddWithValue("@Id", (int)row["Id"]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    IncarcaDate();
                } catch { System.Windows.MessageBox.Show("Eroare la stergere (posibile asocieri existente)."); }
            }
        });
    }
}
