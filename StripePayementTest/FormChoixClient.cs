using System;
using System.Drawing;
using System.Windows.Forms;

namespace OtmanePOS
{
    // Trouve un client par son téléphone, ou crée sa carte s'il est nouveau
    public class FormChoixClient : Form
    {
        public Client ClientChoisi { get; private set; }

        private readonly TextBox txtTelephone, txtPrenom, txtNom;
        private readonly Label lblMessage;
        private readonly Panel panNouveau;

        public FormChoixClient()
        {
            Text = "Carte fidélité";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = Theme.Police(10.5f);
            ClientSize = new Size(440, 190);

            Label titre = Theme.Etiquette("Téléphone du client", Theme.Police(13f, FontStyle.Bold), Theme.Texte);
            titre.Bounds = new Rectangle(24, 18, 390, 30);

            txtTelephone = new TextBox { Font = Theme.Police(15f), Bounds = new Rectangle(24, 56, 250, 36), BorderStyle = BorderStyle.FixedSingle };
            Theme.TexteIndicatif(txtTelephone, "514 555-1234");
            txtTelephone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Chercher(); } };

            BoutonArrondi btnChercher = new BoutonArrondi
            {
                Text = "Chercher",
                Icone = Theme.IcRecherche,
                TailleIcone = 13f,
                Font = Theme.Police(11f, FontStyle.Bold),
                Bounds = new Rectangle(286, 55, 130, 40)
            };
            btnChercher.Click += (s, e) => Chercher();

            lblMessage = Theme.Etiquette("", Theme.Police(10f), Theme.TexteDoux);
            lblMessage.Bounds = new Rectangle(24, 104, 392, 44);

            // Zone « nouveau client », cachée au départ
            panNouveau = new Panel { Bounds = new Rectangle(0, 150, 440, 150), Visible = false, BackColor = Color.White };
            Label lPrenom = Theme.Etiquette("Prénom *", Theme.Police(10f, FontStyle.Bold), Theme.Texte);
            lPrenom.Bounds = new Rectangle(24, 0, 190, 22);
            Label lNom = Theme.Etiquette("Nom", Theme.Police(10f, FontStyle.Bold), Theme.Texte);
            lNom.Bounds = new Rectangle(226, 0, 190, 22);
            txtPrenom = new TextBox { Font = Theme.Police(12f), Bounds = new Rectangle(24, 24, 190, 30), BorderStyle = BorderStyle.FixedSingle };
            txtNom = new TextBox { Font = Theme.Police(12f), Bounds = new Rectangle(226, 24, 190, 30), BorderStyle = BorderStyle.FixedSingle };
            txtNom.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Creer(); } };

            BoutonArrondi btnCreer = new BoutonArrondi
            {
                Text = "Créer la carte",
                Icone = Theme.IcPersonne,
                TailleIcone = 13f,
                BackColor = Theme.Vert,
                Font = Theme.Police(11f, FontStyle.Bold),
                Bounds = new Rectangle(24, 72, 392, 44)
            };
            btnCreer.Click += (s, e) => Creer();
            panNouveau.Controls.AddRange(new Control[] { lPrenom, lNom, txtPrenom, txtNom, btnCreer });

            Controls.AddRange(new Control[] { titre, txtTelephone, btnChercher, lblMessage, panNouveau });
            Shown += (s, e) => txtTelephone.Focus();
        }

        private string TelephoneSaisi()
        {
            string tel = Fidelite.NormaliserTelephone(txtTelephone.Text);
            if (tel.Length == 10) return tel;

            lblMessage.ForeColor = Theme.Rouge;
            lblMessage.Text = "Entre un numéro à 10 chiffres (ex. 514 555-1234).";
            txtTelephone.Focus();
            return null;
        }

        private void Chercher()
        {
            string tel = TelephoneSaisi();
            if (tel == null) return;

            try
            {
                Client c = Donnees.ChercherClient(tel);
                if (c != null)
                {
                    ClientChoisi = c;
                    DialogResult = DialogResult.OK;
                    return;
                }

                // Nouveau client : on affiche le formulaire de création
                lblMessage.ForeColor = Theme.TexteDoux;
                lblMessage.Text = "Aucune carte pour le " + Fidelite.FormaterTelephone(tel) + ".\nCrée la carte du client :";
                panNouveau.Visible = true;
                ClientSize = new Size(440, 290);
                txtPrenom.Focus();
            }
            catch (Exception ex)
            {
                ErreurBase(ex);
            }
        }

        private void Creer()
        {
            string tel = TelephoneSaisi();
            if (tel == null) return;

            string prenom = txtPrenom.Text.Trim();
            if (prenom.Length == 0)
            {
                lblMessage.ForeColor = Theme.Rouge;
                lblMessage.Text = "Le prénom est obligatoire.";
                txtPrenom.Focus();
                return;
            }

            try
            {
                ClientChoisi = Donnees.CreerClient(prenom, txtNom.Text.Trim(), tel);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                ErreurBase(ex);
            }
        }

        private void ErreurBase(Exception ex)
        {
            MessageBox.Show(this, "Erreur avec la base de données :\n\n" + ex.Message +
                "", "Carte fidélité",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
