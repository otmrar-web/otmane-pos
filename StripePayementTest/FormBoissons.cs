using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace StripePayementTest
{
    public partial class FormBoissons : Form
    {
        public string CodeSelectionne { get; private set; }
        public bool Confirme { get; private set; } = false;

        private string prefixeCategorie = "";

        public FormBoissons()
        {
            InitializeComponent();
        }

        private void ChoisirCategorie(string prefixe, string nomAffiche)
        {
            prefixeCategorie = prefixe;
            lblCategorieChoisie.Text = $"Boisson : {nomAffiche}   —   Choisis la taille :";

            btnPetit.Visible = true;
            btnMoyen.Visible = true;
            btnGrand.Visible = true;
        }

        private void btnCafeRegulier_Click(object sender, EventArgs e) => ChoisirCategorie("CAFEREG", "Café régulier");
        private void btnLatte_Click(object sender, EventArgs e) => ChoisirCategorie("LATTE", "Latte");
        private void btnCappuccino_Click(object sender, EventArgs e) => ChoisirCategorie("CAPPU", "Cappuccino");
        private void btnMoccachino_Click(object sender, EventArgs e) => ChoisirCategorie("MOCCA", "Moccachino");
        private void btnChocoChaud_Click(object sender, EventArgs e) => ChoisirCategorie("CHOCO", "Chocolat chaud");
        private void btnPolarPop_Click(object sender, EventArgs e) => ChoisirCategorie("POLAR", "Polar Pop");

        private void ChoisirTaille(string suffixeTaille)
        {
            if (string.IsNullOrEmpty(prefixeCategorie))
            {
                MessageBox.Show("Choisis d'abord une boisson.");
                return;
            }

            CodeSelectionne = $"{prefixeCategorie}-{suffixeTaille}";
            Confirme = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnPetit_Click(object sender, EventArgs e) => ChoisirTaille("P");
        private void btnMoyen_Click(object sender, EventArgs e) => ChoisirTaille("M");
        private void btnGrand_Click(object sender, EventArgs e) => ChoisirTaille("G");
    }
}