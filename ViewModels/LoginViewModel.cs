using RestaurantApp.Core;
using RestaurantApp.Models;
using RestaurantApp.Views;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using System.Data;

namespace RestaurantApp.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private string _connString = System.Configuration.ConfigurationManager.ConnectionStrings["RestaurantDb"].ConnectionString;

        public string Email { get; set; }

        private string _parola;
        public string Parola
        {
            get => _parola;
            set { _parola = value; OnPropertyChanged(); }
        }

        private string _mesajEroare;
        public string MesajEroare { get => _mesajEroare; set { _mesajEroare = value; OnPropertyChanged(); } }

        public ICommand LoginCommand { get; }
        public ICommand ContinuaFaraContCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
            ContinuaFaraContCommand = new RelayCommand(ExecuteContinuaFaraCont);
        }

        private void ExecuteLogin(object parameter)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Parola))
            {
                MesajEroare = "Introdu datele!";
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_AutentificareUtilizator", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Email", Email);
                        cmd.Parameters.AddWithValue("@Parola", Parola);

                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dt); }

                        if (dt.Rows.Count == 1)
                        {
                            SesiuneCurenta.UtilizatorLogat = new Utilizator
                            {
                                Id = (int)dt.Rows[0]["Id"],
                                RolId = (int)dt.Rows[0]["RolId"]
                            };
                            DeschideMainWindowInchideLogin(parameter);
                        }
                        else { MesajEroare = "Date incorecte!"; }
                    }
                }
            }
            catch (System.Exception ex) { MesajEroare = "Eroare DB: " + ex.Message; }
        }

        private void ExecuteContinuaFaraCont(object parameter)
        {
            SesiuneCurenta.UtilizatorLogat = null;
            DeschideMainWindowInchideLogin(parameter);
        }

        private void DeschideMainWindowInchideLogin(object windowParam)
        {
            MainWindow main = new MainWindow();
            main.Show();
            if (windowParam is Window currentWindow)
            {
                currentWindow.Close();
            }
        }
    }
}