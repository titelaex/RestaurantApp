using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Data.SqlClient;
using RestaurantApp.Core;

namespace RestaurantApp.ViewModels
{
    public class RegisterViewModel : ObservableObject
    {
        private string _connString = ConfigurationManager.ConnectionStrings["RestaurantDb"].ConnectionString;

        private string _nume;
        public string Nume { get => _nume; set { _nume = value; OnPropertyChanged(); } }

        private string _prenume;
        public string Prenume { get => _prenume; set { _prenume = value; OnPropertyChanged(); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        private string _telefon;
        public string Telefon { get => _telefon; set { _telefon = value; OnPropertyChanged(); } }

        private string _adresa;
        public string Adresa { get => _adresa; set { _adresa = value; OnPropertyChanged(); } }

        private string _parola;
        public string Parola { get => _parola; set { _parola = value; OnPropertyChanged(); } }

        private string _mesajEroare;
        public string MesajEroare { get => _mesajEroare; set { _mesajEroare = value; OnPropertyChanged(); } }

        public RelayCommand RegisterCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(ExecuteRegister);
        }

        private void ExecuteRegister(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Parola) ||
                string.IsNullOrWhiteSpace(Nume) || string.IsNullOrWhiteSpace(Prenume))
            {
                MesajEroare = "Completează Nume, Prenume, Email și Parolă!";
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_CreareContClient", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Nume", Nume);
                        cmd.Parameters.AddWithValue("@Prenume", Prenume);
                        cmd.Parameters.AddWithValue("@Email", Email);

                        cmd.Parameters.AddWithValue("@Telefon", string.IsNullOrWhiteSpace(Telefon) ? "" : Telefon);
                        cmd.Parameters.AddWithValue("@Adresa", string.IsNullOrWhiteSpace(Adresa) ? "" : Adresa);
                        cmd.Parameters.AddWithValue("@Parola", Parola);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Cont creat cu succes! Acum te poți autentifica.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                if (parameter is Window currentWindow)
                {
                    currentWindow.Close();
                }
            }
            catch (Exception ex)
            {
                MesajEroare = "Eroare: Verificați datele introduse. " + ex.Message;
            }
        }
    }
}