using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Core;
using RestaurantApp.Models;

namespace RestaurantApp.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private string _connString = ConfigurationManager.ConnectionStrings["RestaurantDb"].ConnectionString;

        public ObservableCollection<Preparat> MeniuRestaurant { get; set; }
        public ObservableCollection<Meniu> MeniuriCombo { get; set; }

        public ICollectionView MeniuView { get; private set; }
        public ICollectionView MeniuriComboView { get; private set; }

        public string MesajSalut => SesiuneCurenta.EsteLogat
            ? $"Salut, {SesiuneCurenta.UtilizatorLogat.Nume}{(SesiuneCurenta.UtilizatorLogat.EsteAngajat ? " (Angajat)" : " (Client)")}!"
            : "Esti in modul Vizitator.";

        public bool EsteClient => SesiuneCurenta.EsteLogat && SesiuneCurenta.UtilizatorLogat.EsteClient;
        public bool EsteAngajat => SesiuneCurenta.EsteLogat && SesiuneCurenta.UtilizatorLogat.EsteAngajat;

        public ObservableCollection<ElementCos> CosCumparaturi { get; set; } = new ObservableCollection<ElementCos>();
        public ObservableCollection<Meniu> CosMeniuri { get; set; } = new ObservableCollection<Meniu>(); // Simplificat pentru demo, in mod normal ar trebui ElementCosMeniu

        private decimal _costMancare;
        public decimal CostMancare { get => _costMancare; set { _costMancare = value; OnPropertyChanged(); } }

        private decimal _costTransport;
        public decimal CostTransport { get => _costTransport; set { _costTransport = value; OnPropertyChanged(); } }

        private decimal _valoareDiscount;
        public decimal ValoareDiscount { get => _valoareDiscount; set { _valoareDiscount = value; OnPropertyChanged(); } }

        public decimal TotalDePlata => CostMancare + CostTransport - ValoareDiscount;

        private string _cuvantCheie = "";
        public string CuvantCheie
        {
            get => _cuvantCheie;
            set { _cuvantCheie = value; OnPropertyChanged(); MeniuView.Refresh(); MeniuriComboView.Refresh(); }
        }

        private string _tipCautareSelectat = "Denumire";
        public string TipCautareSelectat
        {
            get => _tipCautareSelectat;
            set { _tipCautareSelectat = value; OnPropertyChanged(); MeniuView.Refresh(); MeniuriComboView.Refresh(); }
        }

        private string _modCautareSelectat = "Conține";

        public string ModCautareSelectat
        {
            get => _modCautareSelectat;
            set { _modCautareSelectat = value; OnPropertyChanged(); MeniuView.Refresh(); MeniuriComboView.Refresh(); }
        }

        public string[] OptiuniTipCautare { get; } = { "Denumire", "Alergen" };
        public string[] OptiuniModCautare { get; } = { "Conține", "Nu conține" };

        public ICommand IncarcaMeniuCommand { get; }
        public ICommand AdaugaInCosCommand { get; }
        public ICommand AdaugaMeniuInCosCommand { get; }
        public ICommand EliminaDinCosCommand { get; }
        public ICommand EliminaMeniuDinCosCommand { get; }
        public ICommand TrimiteComandaCommand { get; }
        public ICommand DeschidePanouAngajatCommand { get; }
        public ICommand VeziComenziMeleCommand { get; }

        public MainViewModel()
        {
            MeniuRestaurant = new ObservableCollection<Preparat>();
            MeniuriCombo = new ObservableCollection<Meniu>();

            MeniuView = CollectionViewSource.GetDefaultView(MeniuRestaurant);
            MeniuView.Filter = FiltrarePreparat;
            MeniuView.GroupDescriptions.Add(new PropertyGroupDescription("CategorieDenumire"));

            MeniuriComboView = CollectionViewSource.GetDefaultView(MeniuriCombo);
            MeniuriComboView.Filter = FiltrareMeniu;
            MeniuriComboView.GroupDescriptions.Add(new PropertyGroupDescription("CategorieDenumire"));

            IncarcaMeniuCommand = new RelayCommand(ExecuteIncarcaMeniu);
            AdaugaInCosCommand = new RelayCommand(ExecuteAdaugaInCos);
            AdaugaMeniuInCosCommand = new RelayCommand(ExecuteAdaugaMeniuInCos);
            EliminaDinCosCommand = new RelayCommand(ExecuteEliminaDinCos);
            EliminaMeniuDinCosCommand = new RelayCommand(ExecuteEliminaMeniuDinCos);
            TrimiteComandaCommand = new RelayCommand(ExecuteTrimiteComanda);
            DeschidePanouAngajatCommand = new RelayCommand(ExecuteDeschidePanouAngajat);
            VeziComenziMeleCommand = new RelayCommand(ExecuteVeziComenziMele);

            ExecuteIncarcaMeniu(null);
        }

        private bool FiltrarePreparat(object obj)
        {
            if (string.IsNullOrWhiteSpace(CuvantCheie)) return true;

            if (obj is Preparat preparat)
            {
                string textDeVerificat = TipCautareSelectat == "Denumire" ? preparat.Denumire : preparat.ListaAlergeni;
                if (textDeVerificat == null) textDeVerificat = "";

                bool contineCuvantul = textDeVerificat.Contains(CuvantCheie, StringComparison.OrdinalIgnoreCase);

                return ModCautareSelectat == "Conține" ? contineCuvantul : !contineCuvantul;
            }
            return false;
        }

        private bool FiltrareMeniu(object obj)
        {
            if (string.IsNullOrWhiteSpace(CuvantCheie)) return true;
            if (TipCautareSelectat == "Alergen") return ModCautareSelectat != "Conține"; // Meniurile nu au alergeni direct in DB momentan

            if (obj is Meniu meniu)
            {
                bool contineCuvantul = meniu.Denumire.Contains(CuvantCheie, StringComparison.OrdinalIgnoreCase);
                return ModCautareSelectat == "Conține" ? contineCuvantul : !contineCuvantul;
            }
            return false;
        }

        private void ExecuteIncarcaMeniu(object parameter)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    
                    // Incarca Preparate
                    using (SqlCommand cmd = new SqlCommand("sp_GetMeniuPreparate", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        DataTable dtP = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtP); }

                        MeniuRestaurant.Clear();
                        foreach (DataRow row in dtP.Rows)
                        {
                            MeniuRestaurant.Add(new Preparat
                            {
                                Id = (int)row["Id"],
                                Denumire = row["Denumire"].ToString(),
                                Pret = (decimal)row["Pret"],
                                CantitatePortie = (int)row["CantitatePortie"],
                                CantitateTotala = (int)row["CantitateTotala"],
                                CategorieId = (int)row["CategorieId"],
                                CategorieDenumire = row["CategorieDenumire"].ToString(),
                                ListaAlergeni = row["ListaAlergeni"].ToString()
                            });
                        }
                    }

                    // Incarca Meniuri Combo
                    using (SqlCommand cmd = new SqlCommand("sp_GetMeniuri", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        DataTable dtM = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtM); }

                        MeniuriCombo.Clear();
                        foreach (DataRow row in dtM.Rows)
                        {
                            MeniuriCombo.Add(new Meniu
                            {
                                Id = (int)row["Id"],
                                Denumire = row["Denumire"].ToString(),
                                CategorieDenumire = row["CategorieDenumire"].ToString(),
                                DetaliiPreparate = row["DetaliiPreparate"].ToString(),
                                PretFaraReducere = (decimal)row["PretFaraReducere"],
                                CategorieId = (int)row["CategorieId"]
                            });
                        }
                    }
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void ExecuteAdaugaInCos(object parameter)
        {
            if (parameter is Preparat preparat)
            {
                if (preparat.EsteEpuizat) return;
                var elementExistent = CosCumparaturi.FirstOrDefault(e => e.Preparat.Id == preparat.Id);
                if (elementExistent != null) elementExistent.Cantitate++;
                else CosCumparaturi.Add(new ElementCos { Preparat = preparat, Cantitate = 1 });
                RecalculeazaTotaluri();
            }
        }

        private void ExecuteAdaugaMeniuInCos(object parameter)
        {
            if (parameter is Meniu meniu)
            {
                CosMeniuri.Add(meniu);
                RecalculeazaTotaluri();
            }
        }

        private void ExecuteEliminaDinCos(object parameter)
        {
            if (parameter is ElementCos element)
            {
                CosCumparaturi.Remove(element);
                RecalculeazaTotaluri();
            }
        }

        private void ExecuteEliminaMeniuDinCos(object parameter)
        {
            if (parameter is Meniu meniu)
            {
                CosMeniuri.Remove(meniu);
                RecalculeazaTotaluri();
            }
        }

        private void RecalculeazaTotaluri()
        {
            decimal reducereMeniuX = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["ReducereMeniuProcentX"] ?? "10");
            CostMancare = CosCumparaturi.Sum(e => e.Subtotal) + CosMeniuri.Sum(m => m.PretFinal(reducereMeniuX));

            try
            {
                decimal limitaFaraTransport = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["ValoareMinimaFaraTransportA"] ?? "50");
                decimal taxaTransport = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["CostTransportB"] ?? "15");
                decimal sumaMinimaDiscount = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["SumaMinimaDiscountY"] ?? "150");
                decimal procentDiscount = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["ProcentDiscountW"] ?? "15");

                if (CostMancare > 0)
                {
                    CostTransport = (CostMancare < limitaFaraTransport) ? taxaTransport : 0;
                    decimal reducereProcentuala = (CostMancare > sumaMinimaDiscount) ? CostMancare * (procentDiscount / 100) : 0;
                    ValoareDiscount = reducereProcentuala;
                }
                else
                {
                    CostTransport = 0;
                    ValoareDiscount = 0;
                }
            }
            catch (System.Exception ex)
            {
                CostTransport = 0;
                ValoareDiscount = 0;
                System.Diagnostics.Debug.WriteLine("Eroare la calcul: " + ex.Message);
            }
            OnPropertyChanged(nameof(TotalDePlata));
        }

        private void ExecuteTrimiteComanda(object parameter)
        {
            if (CosCumparaturi.Count == 0 && CosMeniuri.Count == 0)
            {
                System.Windows.MessageBox.Show("Coșul este gol!");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    int comandaNouaId = 0;

                    using (SqlCommand cmd = new SqlCommand("sp_CreareComanda", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClientId", SesiuneCurenta.UtilizatorLogat.Id);
                        cmd.Parameters.AddWithValue("@CostMancare", CostMancare);
                        cmd.Parameters.AddWithValue("@CostTransport", CostTransport);
                        cmd.Parameters.AddWithValue("@CostTotal", TotalDePlata);

                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dt); }
                        if (dt.Rows.Count > 0) comandaNouaId = Convert.ToInt32(dt.Rows[0]["ComandaNouaId"]);
                    }

                    if (comandaNouaId > 0)
                    {
                        foreach (var element in CosCumparaturi)
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_AdaugaDetaliuComanda", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@ComandaId", comandaNouaId);
                                cmd.Parameters.AddWithValue("@PreparatId", element.Preparat.Id);
                                cmd.Parameters.AddWithValue("@NumarBucati", element.Cantitate);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        foreach (var m in CosMeniuri)
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_AdaugaDetaliuMeniu", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@ComandaId", comandaNouaId);
                                cmd.Parameters.AddWithValue("@MeniuId", m.Id);
                                cmd.Parameters.AddWithValue("@NumarBucati", 1);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        CosCumparaturi.Clear();
                        CosMeniuri.Clear();
                        RecalculeazaTotaluri();
                        System.Windows.MessageBox.Show("Comanda a fost înregistrată!", "Succes");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show("Eroare salvare comanda: " + ex.Message);
            }
        }

        private void ExecuteDeschidePanouAngajat(object obj)
        {
            var win = new Views.EmployeeWindow();
            win.Show();
        }

        private void ExecuteVeziComenziMele(object obj)
        {
            System.Windows.MessageBox.Show("Funcție în curs de activare. Comenzile se pot vedea în baza de date momentan.");
        }
    }
}