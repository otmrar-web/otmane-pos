using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace OtmanePOS
{
    public class FormParametres : Form
    {
        private const int LargeurCarte = 700;
        private const int XChamp = 270;
        private const int LargeurChamp = 400;
        private const int HauteurLigne = 46;

        private readonly Panel defilement;
        private int y = 16;

        private TextBox txtNom, txtAdresse, txtVille, txtTelephone, txtTPSNo, txtTVQNo, txtMessage;
        private TextBox txtTauxTPS, txtTauxTVQ, txtCleStripe;
        private ComboBox cboLecteur;
        private Label lblEtatStripe;
        private CheckBox chkArrondi, chkFidelite;
        private ComboBox cboOffert;
        private Label lblResumeCartes;

        public FormParametres()
        {
            Text = "Otmane POS - Paramètres";
            BackColor = Theme.Fond;
            Font = Theme.Police(10.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(LargeurCarte + 70, 820);
            MinimumSize = new Size(LargeurCarte + 90, 500);
            MaximizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            // ---- Barre du bas ----
            Panel bas = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = Color.White, Padding = new Padding(24, 14, 24, 14) };
            BoutonArrondi btnEnregistrer = new BoutonArrondi { Text = "Enregistrer", Font = Theme.Police(12f, FontStyle.Bold), Dock = DockStyle.Right, Width = 190 };
            btnEnregistrer.Click += (s, e) => Enregistrer();
            Panel espace = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = Color.White };
            BoutonArrondi btnAnnuler = new BoutonArrondi { Text = "Annuler", Font = Theme.Police(11f, FontStyle.Bold), BackColor = Theme.Gris, ForeColor = Theme.Texte, Dock = DockStyle.Right, Width = 140 };
            btnAnnuler.Click += (s, e) => Close();
            bas.Controls.Add(btnAnnuler);
            bas.Controls.Add(espace);
            bas.Controls.Add(btnEnregistrer);

            defilement = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Fond };
            Controls.Add(defilement);
            Controls.Add(bas);

            Label titre = Theme.Etiquette("Paramètres", Theme.Police(20f, FontStyle.Bold), Theme.Texte);
            titre.SetBounds(24, y, LargeurCarte, 44);
            defilement.Controls.Add(titre);
            y += 52;

            if (!Parametres.TableTrouvee)
            {
                Label alerte = Theme.Etiquette("La base de données n'a pas pu être ouverte : les valeurs par défaut sont utilisées. " +
                    "Fichier : " + Base.Fichier, Theme.Police(10f, FontStyle.Bold), Theme.Rouge);
                alerte.SetBounds(24, y, LargeurCarte, 44);
                defilement.Controls.Add(alerte);
                y += 52;
            }

            CreerSectionCommerce();
            CreerSectionTaxes();
            CreerSectionFidelite();
            CreerSectionPaiements();
            CreerSectionDonnees();

            // marge en bas de la zone qui défile
            Panel fin = new Panel { Bounds = new Rectangle(24, y, 10, 16), BackColor = Theme.Fond };
            defilement.Controls.Add(fin);
        }

        // =====================================================================
        // SECTIONS
        // =====================================================================
        private void CreerSectionCommerce()
        {
            PanneauArrondi c = Carte("Commerce (affiché sur le ticket)", 7);
            txtNom = Champ(c, 0, "Nom du commerce", Parametres.NomCommerce);
            txtAdresse = Champ(c, 1, "Adresse", Parametres.Adresse, "123, rue Principale");
            txtVille = Champ(c, 2, "Ville et code postal", Parametres.Ville, "Montréal (Québec) H2X 1Y4");
            txtTelephone = Champ(c, 3, "Téléphone", Parametres.Telephone, "514 555-1234");
            txtTPSNo = Champ(c, 4, "N° d'inscription TPS", Parametres.NumeroTPS, "123456789 RT0001");
            txtTVQNo = Champ(c, 5, "N° d'inscription TVQ", Parametres.NumeroTVQ, "1234567890 TQ0001");
            txtMessage = Champ(c, 6, "Message en bas du ticket", Parametres.MessageTicket);
        }

        private void CreerSectionTaxes()
        {
            PanneauArrondi c = Carte("Taxes", 3);
            txtTauxTPS = Champ(c, 0, "TPS (%)", Parametres.TauxTPSPourcent.ToString("0.###", Theme.Culture));
            txtTauxTPS.Width = 120;
            txtTauxTVQ = Champ(c, 1, "TVQ (%)", Parametres.TauxTVQPourcent.ToString("0.###", Theme.Culture));
            txtTauxTVQ.Width = 120;
            chkArrondi = CaseACocher(c, 2, "Comptant", "Arrondir le comptant aux 5 cents", Parametres.ArrondiComptant);
        }

        private void CreerSectionFidelite()
        {
            PanneauArrondi c = Carte("Cartes fidélité (tampons)", 3);
            chkFidelite = CaseACocher(c, 0, "Programme", "Activer les cartes fidélité", Parametres.FideliteActive);

            cboOffert = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Police(11f), Width = 260 };
            cboOffert.Items.AddRange(new object[] { "Le plus cher du panier", "Le moins cher du panier" });
            cboOffert.SelectedIndex = Parametres.OffrirLePlusCher ? 0 : 1;
            PlacerChamp(c, 1, "Produit offert", cboOffert);

            BoutonArrondi btnCartes = new BoutonArrondi
            {
                Text = "Gérer les cartes",
                Icone = Theme.IcClients,
                TailleIcone = 13f,
                Font = Theme.Police(10.5f, FontStyle.Bold),
                Size = new Size(190, 36)
            };
            btnCartes.Click += (s, e) =>
            {
                using (FormCartesFidelite f = new FormCartesFidelite()) f.ShowDialog(this);
                Fidelite.Charger();
                MettreAJourResumeCartes();
            };
            PlacerChamp(c, 2, "Cartes", btnCartes);

            lblResumeCartes = Theme.Etiquette("", Theme.Police(9.5f), Theme.TexteDoux);
            lblResumeCartes.SetBounds(XChamp + 200, btnCartes.Top, LargeurCarte - XChamp - 220, 36);
            c.Controls.Add(lblResumeCartes);
            MettreAJourResumeCartes();
        }

        private void MettreAJourResumeCartes()
        {
            if (!Fidelite.TableTrouvee)
            {
                lblResumeCartes.Text = "Base de données introuvable";
                lblResumeCartes.ForeColor = Theme.Rouge;
                return;
            }
            int actives = Fidelite.CartesActives.Count();
            lblResumeCartes.ForeColor = Theme.TexteDoux;
            lblResumeCartes.Text = actives == 0 ? "Aucune carte active"
                : actives + " active" + (actives > 1 ? "s" : "") + " : " + string.Join(", ", Fidelite.CartesActives.Select(x => x.Nom));
        }

        private void CreerSectionPaiements()
        {
            PanneauArrondi c = Carte("Stripe (paiements par carte)", 4);

            txtCleStripe = Champ(c, 0, "Clé secrète Stripe", Parametres.StripeCle, "sk_live_... ou rk_live_...");
            txtCleStripe.UseSystemPasswordChar = true;
            txtCleStripe.Width = 300;
            CheckBox voir = new CheckBox { Text = "Voir", AutoSize = true, BackColor = Color.White, Location = new Point(XChamp + 310, txtCleStripe.Top + 3) };
            voir.CheckedChanged += (s, e) => txtCleStripe.UseSystemPasswordChar = !voir.Checked;
            c.Controls.Add(voir);

            BoutonArrondi btnTester = new BoutonArrondi { Text = "Tester la connexion", Font = Theme.Police(10.5f, FontStyle.Bold), Size = new Size(200, 36) };
            btnTester.Click += async (s, e) => await TesterStripe(btnTester);
            PlacerChamp(c, 1, "Connexion", btnTester);
            lblEtatStripe = Theme.Etiquette(ServiceStripe.EstConfigure ? "" : "Pas encore connecté", Theme.Police(9.5f, FontStyle.Bold), Theme.TexteDoux);
            lblEtatStripe.AutoEllipsis = true;
            lblEtatStripe.SetBounds(XChamp + 210, btnTester.Top, LargeurCarte - XChamp - 230, 36);
            c.Controls.Add(lblEtatStripe);

            cboLecteur = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Police(11f), Width = LargeurChamp };
            cboLecteur.Items.Add("(aucun lecteur)");
            if (!string.IsNullOrEmpty(Parametres.ReaderId))
                cboLecteur.Items.Add(new LecteurStripe { Id = Parametres.ReaderId, Nom = Parametres.ReaderId, EnLigne = true });
            cboLecteur.SelectedIndex = cboLecteur.Items.Count - 1;
            PlacerChamp(c, 2, "Lecteur (TPE)", cboLecteur);

            Label aide = Theme.Etiquette("Ta clé est dans le tableau de bord Stripe → Développeurs → Clés API. " +
                "Elle est chiffrée sur cet ordinateur. Clique sur « Tester » pour voir tes lecteurs.", Theme.Police(9f), Theme.TexteDoux);
            aide.SetBounds(20, 56 + 3 * HauteurLigne, LargeurCarte - 40, 40);
            c.Controls.Add(aide);
        }

        private async System.Threading.Tasks.Task TesterStripe(BoutonArrondi bouton)
        {
            string cle = txtCleStripe.Text.Trim();
            if (cle.Length == 0) { Erreur(txtCleStripe, "Colle d'abord ta clé secrète Stripe."); return; }

            bouton.Enabled = false;
            lblEtatStripe.ForeColor = Theme.TexteDoux;
            lblEtatStripe.Text = "Connexion à Stripe...";
            try
            {
                InfoCompteStripe info = await ServiceStripe.TesterConnexionAsync(cle);
                lblEtatStripe.ForeColor = info.ModeTest ? Color.FromArgb(202, 138, 4) : Theme.Vert;
                lblEtatStripe.Text = "✓ " + info.Nom + (info.ModeTest ? "  (MODE TEST)" : "  (paiements réels)");

                // Liste des lecteurs du compte
                string garde = cboLecteur.SelectedItem is LecteurStripe ? ((LecteurStripe)cboLecteur.SelectedItem).Id : null;
                cboLecteur.Items.Clear();
                cboLecteur.Items.Add("(aucun lecteur)");
                foreach (LecteurStripe l in info.Lecteurs) cboLecteur.Items.Add(l);
                int index = 0;
                for (int i = 1; i < cboLecteur.Items.Count; i++)
                    if (((LecteurStripe)cboLecteur.Items[i]).Id == garde || (garde == null && info.Lecteurs.Count == 1)) index = i;
                cboLecteur.SelectedIndex = index;

                if (info.ErreurLecteurs != null)
                    MessageBox.Show(this, "Connexion réussie, mais impossible de lister les lecteurs :\n\n" + info.ErreurLecteurs +
                        "\n\nAvec une clé restreinte, donne-lui le droit « Terminal » en écriture.", "Stripe",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (info.Lecteurs.Count == 0)
                    MessageBox.Show(this, "Connexion réussie. Aucun lecteur Stripe Terminal n'est enregistré sur ce compte.\n\n" +
                        "Enregistre ton lecteur dans le tableau de bord Stripe (Terminal → Lecteurs), puis teste de nouveau. " +
                        "Sans lecteur, tu peux quand même encaisser au comptant et avec « Payer avec Stripe ».", "Stripe",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblEtatStripe.ForeColor = Theme.Rouge;
                lblEtatStripe.Text = "✗ Échec de la connexion";
                MessageBox.Show(this, "Stripe refuse cette clé :\n\n" + ex.Message, "Stripe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                bouton.Enabled = true;
            }
        }

        private void CreerSectionDonnees()
        {
            PanneauArrondi c = Carte("Données (sauvegarde)", 2);

            Label chemin = Theme.Etiquette(Base.Fichier, Theme.Police(9.5f), Theme.TexteDoux);
            chemin.AutoEllipsis = true;
            chemin.SetBounds(XChamp, 56, LargeurChamp, 32);
            Label lChemin = Theme.Etiquette("Fichier de la base", Theme.Police(10.5f), Theme.TexteDoux);
            lChemin.SetBounds(20, 56, XChamp - 30, 32);
            c.Controls.Add(lChemin);
            c.Controls.Add(chemin);

            BoutonArrondi btnSauver = new BoutonArrondi { Text = "Sauvegarder la base...", Font = Theme.Police(10.5f, FontStyle.Bold), Size = new Size(210, 36) };
            btnSauver.Click += (s, e) => SauvegarderBase();
            PlacerChamp(c, 1, "Copie de sécurité", btnSauver);

            BoutonArrondi btnDossier = new BoutonArrondi { Text = "Ouvrir le dossier", BackColor = Theme.Gris, ForeColor = Theme.Texte, Font = Theme.Police(10.5f, FontStyle.Bold), Size = new Size(170, 36) };
            btnDossier.Location = new Point(XChamp + 220, btnSauver.Top);
            btnDossier.Click += (s, e) => System.Diagnostics.Process.Start("explorer.exe", Base.Dossier);
            c.Controls.Add(btnDossier);
        }

        private void SauvegarderBase()
        {
            using (SaveFileDialog choix = new SaveFileDialog
            {
                Title = "Sauvegarder la base",
                Filter = "Base Otmane POS (*.db)|*.db",
                FileName = "otmanepos-sauvegarde-" + DateTime.Now.ToString("yyyy-MM-dd") + ".db"
            })
            {
                if (choix.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    Base.Sauvegarder(choix.FileName);
                    MessageBox.Show(this, "Sauvegarde réussie :\n" + choix.FileName +
                        "\n\nGarde une copie sur une clé USB ou dans le nuage.", "Sauvegarde",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "La sauvegarde a échoué :\n\n" + ex.Message, "Sauvegarde",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // =====================================================================
        // OUTILS DE MISE EN PAGE
        // =====================================================================
        private PanneauArrondi Carte(string titre, int lignes)
        {
            int hauteur = 56 + lignes * HauteurLigne + 12;
            PanneauArrondi c = new PanneauArrondi { Bounds = new Rectangle(24, y, LargeurCarte, hauteur), Bordure = Theme.Gris };
            Label t = Theme.Etiquette(titre, Theme.Police(13f, FontStyle.Bold), Theme.Texte);
            t.SetBounds(20, 12, LargeurCarte - 40, 32);
            c.Controls.Add(t);
            defilement.Controls.Add(c);
            y += hauteur + 16;
            return c;
        }

        private void PlacerChamp(PanneauArrondi c, int ligne, string libelle, Control champ)
        {
            int yLigne = 56 + ligne * HauteurLigne;
            Label l = Theme.Etiquette(libelle, Theme.Police(10.5f), Theme.TexteDoux);
            l.SetBounds(20, yLigne, XChamp - 30, 32);
            champ.Location = new Point(XChamp, yLigne + (32 - champ.Height) / 2);
            c.Controls.Add(l);
            c.Controls.Add(champ);
        }

        private TextBox Champ(PanneauArrondi c, int ligne, string libelle, string valeur, string indicatif = null)
        {
            TextBox tb = new TextBox { Text = valeur ?? "", Font = Theme.Police(11f), Width = LargeurChamp, BorderStyle = BorderStyle.FixedSingle };
            if (indicatif != null) Theme.TexteIndicatif(tb, indicatif);
            PlacerChamp(c, ligne, libelle, tb);
            return tb;
        }

        private CheckBox CaseACocher(PanneauArrondi c, int ligne, string libelle, string texte, bool coche)
        {
            CheckBox cb = new CheckBox { Text = texte, Checked = coche, AutoSize = true, Font = Theme.Police(11f), ForeColor = Theme.Texte, BackColor = Color.White };
            PlacerChamp(c, ligne, libelle, cb);
            return cb;
        }

        // =====================================================================
        // ENREGISTRER
        // =====================================================================
        private static bool LirePourcent(TextBox tb, out decimal valeur)
        {
            string s = tb.Text.Trim().Replace('.', ',').Replace("%", "").Trim();
            return decimal.TryParse(s, NumberStyles.Number, Theme.Culture, out valeur) && valeur >= 0 && valeur <= 30;
        }

        private void Erreur(Control champ, string message)
        {
            MessageBox.Show(this, message, "Paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            defilement.ScrollControlIntoView(champ);
            champ.Focus();
        }

        private void Enregistrer()
        {
            if (txtNom.Text.Trim().Length == 0) { Erreur(txtNom, "Le nom du commerce est obligatoire."); return; }

            decimal tps, tvq;
            if (!LirePourcent(txtTauxTPS, out tps)) { Erreur(txtTauxTPS, "Taux de TPS invalide (exemple : 5)."); return; }
            if (!LirePourcent(txtTauxTVQ, out tvq)) { Erreur(txtTauxTVQ, "Taux de TVQ invalide (exemple : 9,975)."); return; }

            Parametres.NomCommerce = txtNom.Text.Trim();
            Parametres.Adresse = txtAdresse.Text.Trim();
            Parametres.Ville = txtVille.Text.Trim();
            Parametres.Telephone = txtTelephone.Text.Trim();
            Parametres.NumeroTPS = txtTPSNo.Text.Trim();
            Parametres.NumeroTVQ = txtTVQNo.Text.Trim();
            Parametres.MessageTicket = txtMessage.Text.Trim();
            Parametres.TauxTPSPourcent = tps;
            Parametres.TauxTVQPourcent = tvq;
            Parametres.ArrondiComptant = chkArrondi.Checked;
            Parametres.FideliteActive = chkFidelite.Checked;
            Parametres.OffrirLePlusCher = cboOffert.SelectedIndex == 0;
            Parametres.StripeCle = txtCleStripe.Text.Trim();
            Parametres.ReaderId = cboLecteur.SelectedItem is LecteurStripe ? ((LecteurStripe)cboLecteur.SelectedItem).Id : "";

            try
            {
                Parametres.Enregistrer();
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Les paramètres sont appliqués pour cette session, mais n'ont pas pu être enregistrés :\n\n" +
                    ex.Message + "\n\nFichier : " + Base.Fichier, "Paramètres",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.OK;   // appliqués quand même en mémoire
            }
        }
    }
}
