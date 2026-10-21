using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StripePayementTest
{
    public partial class FormProduits : Form
    {
        public FormProduits()
        {
            InitializeComponent();
        }

        private void FormProduits_Load(object sender, EventArgs e)
        {
            ChargerProduits();
            ChargerStockFaible();
            MettreEnEvidenceStockFaible();
        }
        private void ChargerProduits()
        {
            string connectionString =
    @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string sql = "SELECT Id, CodeBarre, Nom, Prix, Stock FROM Produits";
            using SqlDataAdapter adapter = new SqlDataAdapter(sql, connection);
            DataTable table = new DataTable();
            adapter.Fill(table);
            dgvProduits.DataSource = table;
        }

        private void ChargerStockFaible()
        {
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = @"
        SELECT COUNT(*)
        FROM Produits
        WHERE Stock <= 5";

            using SqlCommand command = new SqlCommand(sql, connection);

            int nombre = Convert.ToInt32(command.ExecuteScalar());

            lblStockFaible.Text = $"PRODUITS EN STOCK FAIBLE : {nombre}";
        }


        private void btnAjouter_Click(object sender, EventArgs e)
        {
            string codeBarre = txtCodeBarre.Text.Trim();
            string nom = txtNom.Text.Trim();
            decimal prix = decimal.Parse(txtPrix.Text);
            int stock = int.Parse(txtStock.Text);
            string connectionString =
    @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string sql = @"
    INSERT INTO Produits (CodeBarre, Nom, Prix, Stock)
    VALUES (@CodeBarre, @Nom, @Prix, @Stock)";
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CodeBarre", codeBarre);
            command.Parameters.AddWithValue("@Nom", nom);
            command.Parameters.AddWithValue("@Prix", prix);
            command.Parameters.AddWithValue("@Stock", stock);
            command.ExecuteNonQuery();
            MessageBox.Show("Produit ajouté avec succès !");
            ChargerProduits();
            txtCodeBarre.Clear();
            txtNom.Clear();
            txtPrix.Clear();
            txtStock.Clear();
        }

        private void btnModifier_Click(object sender, EventArgs e)
        {
            if (dgvProduits.CurrentRow == null)
            {
                MessageBox.Show("Sélectionnez un produit à modifier.");
                return;
            }
            int id = Convert.ToInt32(dgvProduits.CurrentRow.Cells["Id"].Value);
            string codeBarre = txtCodeBarre.Text.Trim();
            string nom = txtNom.Text.Trim();
            decimal prix = decimal.Parse(txtPrix.Text);
            int stock = int.Parse(txtStock.Text);
            string connectionString =
    @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string sql = @"
    UPDATE Produits
    SET CodeBarre = @CodeBarre,
        Nom = @Nom,
        Prix = @Prix,
        Stock = @Stock
    WHERE Id = @Id";
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CodeBarre", codeBarre);
            command.Parameters.AddWithValue("@Nom", nom);
            command.Parameters.AddWithValue("@Prix", prix);
            command.Parameters.AddWithValue("@Stock", stock);
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
            MessageBox.Show("Produit modifié avec succès !");
            ChargerProduits();
        }

        private void dgvProduits_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            DataGridViewRow row = dgvProduits.Rows[e.RowIndex];
            txtCodeBarre.Text = row.Cells["CodeBarre"].Value?.ToString();
            txtNom.Text = row.Cells["Nom"].Value?.ToString();
            txtPrix.Text = row.Cells["Prix"].Value?.ToString();
            txtStock.Text = row.Cells["Stock"].Value?.ToString();
        }

        private void btnSupprimer_Click(object sender, EventArgs e)
        {
            if (dgvProduits.CurrentRow == null)
            {
                MessageBox.Show("Sélectionnez un produit à supprimer.");
                return;
            }
            int id = Convert.ToInt32(dgvProduits.CurrentRow.Cells["Id"].Value);
            string nom = dgvProduits.CurrentRow.Cells["Nom"].Value?.ToString();
            DialogResult resultat = MessageBox.Show(
    $"Voulez-vous vraiment supprimer {nom} ?",
    "Confirmation",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Warning);
            if (resultat != DialogResult.Yes)
            {
                return;
            }
            string connectionString =
    @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string sql = "DELETE FROM Produits WHERE Id = @Id";
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
            MessageBox.Show("Produit supprimé avec succès !");
            ChargerProduits();
        }
        private void MettreEnEvidenceStockFaible()
        {
            foreach (DataGridViewRow row in dgvProduits.Rows)
            {
                if (row.Cells["Stock"].Value != null)
                {
                    int stock = Convert.ToInt32(row.Cells["Stock"].Value);

                    if (stock <= 5)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.White;
                    }
                }
            }
        }
    }
}
