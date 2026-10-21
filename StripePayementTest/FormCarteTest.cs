using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace StripePayementTest
{
    public partial class FormCarteTest : Form
    {
        public string NumeroCarte { get; private set; }
        public bool Confirme { get; private set; } = false;

        public FormCarteTest()
        {
            InitializeComponent();
            txtNumeroCarte.Text = "4242424242424242"; // carte test qui réussit toujours
        }

        private void FormCarteTest_Load(object sender, EventArgs e)
        {
            // rien à faire ici pour l'instant
        }

        private void btnConfirmer_Click(object sender, EventArgs e)
        {
            string numero = txtNumeroCarte.Text.Trim().Replace(" ", "");

            if (string.IsNullOrEmpty(numero))
            {
                MessageBox.Show("Entre un numéro de carte test.");
                return;
            }

            NumeroCarte = numero;
            Confirme = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnAnnuler_Click(object sender, EventArgs e)
        {
            Confirme = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
