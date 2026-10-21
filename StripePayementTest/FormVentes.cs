using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace StripePayementTest
{
    public partial class FormVentes : Form
    {
        public FormVentes()
        {
            InitializeComponent();
        }

        private void FormVentes_Load(object sender, EventArgs e)
        {
            ChargerVentes();
            ChargerTotalAujourdhui();
            ChargerNombreVentesAujourdhui();
        }
        private void ChargerVentes()
        {
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);

            connection.Open();

            string sql = @"
        SELECT Id, Montant, Statut, NumeroTransaction, DatePaiement
        FROM Paiements
        ORDER BY DatePaiement DESC";

            using SqlDataAdapter adapter = new SqlDataAdapter(sql, connection);

            DataTable table = new DataTable();

            adapter.Fill(table);

            dgvVentes.DataSource = table;
        }
        private void ChargerTotalAujourdhui()
        {
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = @"
        SELECT ISNULL(SUM(Montant), 0)
        FROM Paiements
        WHERE CAST(DatePaiement AS DATE) = CAST(GETDATE() AS DATE)
        AND Statut = 'Réussi'";

            using SqlCommand command = new SqlCommand(sql, connection);

            decimal total = Convert.ToDecimal(command.ExecuteScalar());

            lblVentesAujourdhui.Text = $"VENTES AUJOURD'HUI : {total:C}";
        }


        private void ChargerNombreVentesAujourdhui()
        {
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = @"
        SELECT COUNT(*)
        FROM Paiements
        WHERE CAST(DatePaiement AS DATE) = CAST(GETDATE() AS DATE)
        AND Statut = 'Réussi'";

            using SqlCommand command = new SqlCommand(sql, connection);

            int nombreVentes = Convert.ToInt32(command.ExecuteScalar());

            lblNombreVentes.Text = $"NOMBRE DE VENTES AUJOURD'HUI : {nombreVentes}";
        }

        private void dgvVentes_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            int paiementId = Convert.ToInt32(
                dgvVentes.Rows[e.RowIndex].Cells["Id"].Value
            );

            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = @"
        SELECT NomProduit, Quantite, PrixUnitaire, Total
        FROM DetailsVente
        WHERE PaiementId = @PaiementId
        ORDER BY Id";

            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PaiementId", paiementId);

            using SqlDataAdapter adapter = new SqlDataAdapter(command);

            DataTable table = new DataTable();
            adapter.Fill(table);

            dgvDetailsVente.DataSource = table;
        }

        private void btnRechercherDate_Click(object sender, EventArgs e)
        {
            DateTime dateChoisie = dtpDate.Value.Date;

            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = @"
        SELECT Id, Montant, Statut, NumeroTransaction, DatePaiement
        FROM Paiements
        WHERE CAST(DatePaiement AS DATE) = @Date
        ORDER BY DatePaiement DESC";

            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Date", dateChoisie);

            using SqlDataAdapter adapter = new SqlDataAdapter(command);

            DataTable table = new DataTable();
            adapter.Fill(table);

            dgvVentes.DataSource = table;
            ChargerResumeDate(dateChoisie);
        }
        private void ChargerResumeDate(DateTime date)
        {
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = @"
        SELECT 
            ISNULL(SUM(Montant), 0) AS TotalVentes,
            COUNT(*) AS NombreVentes
        FROM Paiements
        WHERE CAST(DatePaiement AS DATE) = @Date
        AND Statut = 'Réussi'";

            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Date", date.Date);

            using SqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                decimal total = Convert.ToDecimal(reader["TotalVentes"]);
                int nombre = Convert.ToInt32(reader["NombreVentes"]);

                lblVentesAujourdhui.Text = $"VENTES DU {date:dd/MM/yyyy} : {total:C}";
                lblNombreVentes.Text = $"NOMBRE DE VENTES : {nombre}";
            }
        }
    }
}
