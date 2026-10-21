using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OtmanePOS
{
    // Créer, modifier, activer ou désactiver les cartes fidélité
    public class FormCartesFidelite : Form
    {
        private readonly ListView lvCartes;
        private readonly TextBox txtNom;
        private readonly ComboBox cboCategorie;
        private readonly NumericUpDown numTampons;
        private readonly CheckBox chkActive;
        private readonly Label lblEdition, lblExemple;
        private CarteFidelite carteEnCours;   // null = nouvelle carte

        public FormCartesFidelite()
        {
            Text = "Cartes fidélité";
            BackColor = Color.White;
            Font = Theme.Police(10.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(700, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            Label titre = Theme.Etiquette("Cartes fidélité", Theme.Police(18f, FontStyle.Bold), Theme.Texte);
            titre.SetBounds(24, 16, 400, 40);
            Label aide = Theme.Etiquette("Chaque carte donne 1 tampon par produit acheté dans sa catégorie. " +
                "Quand le client a assez de tampons, un produit de cette catégorie lui est offert.",
                Theme.Police(9.5f), Theme.TexteDoux);
            aide.SetBounds(24, 56, 652, 40);

            // ---- Liste des cartes ----
            lvCartes = new ListView
            {
                Bounds = new Rectangle(24, 102, 652, 190),
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.Police(10.5f),
                SmallImageList = new ImageList { ImageSize = new Size(1, 28) }
            };
            lvCartes.Columns.Add("Carte", 190);
            lvCartes.Columns.Add("Catégorie", 170);
            lvCartes.Columns.Add("Règle", 180);
            lvCartes.Columns.Add("État", 90);
            lvCartes.SelectedIndexChanged += (s, e) =>
            {
                if (lvCartes.SelectedItems.Count > 0) Editer((CarteFidelite)lvCartes.SelectedItems[0].Tag);
            };

            BoutonArrondi btnNouvelle = new BoutonArrondi
            {
                Text = "Nouvelle carte",
                Icone = "\uE710",   // +
                TailleIcone = 12f,
                BackColor = Theme.Gris,
                ForeColor = Theme.Texte,
                CouleurIcone = Theme.Texte,
                Font = Theme.Police(10.5f, FontStyle.Bold),
                Bounds = new Rectangle(24, 302, 170, 38)
            };
            btnNouvelle.Click += (s, e) => { lvCartes.SelectedItems.Clear(); Editer(null); txtNom.Focus(); };

            // ---- Édition ----
            PanneauArrondi edition = new PanneauArrondi { Bounds = new Rectangle(24, 354, 652, 176), BackColor = Theme.GrisClair, Rayon = 12 };
            lblEdition = Theme.Etiquette("Nouvelle carte", Theme.Police(12f, FontStyle.Bold), Theme.Texte);
            lblEdition.SetBounds(18, 10, 400, 28);

            Label lNom = Theme.Etiquette("Nom", Theme.Police(10f), Theme.TexteDoux);
            lNom.SetBounds(18, 44, 200, 20);
            txtNom = new TextBox { Bounds = new Rectangle(18, 66, 200, 30), Font = Theme.Police(11f) };
            Theme.TexteIndicatif(txtNom, "ex. Sandwich");

            Label lCat = Theme.Etiquette("Catégorie de produits", Theme.Police(10f), Theme.TexteDoux);
            lCat.SetBounds(232, 44, 200, 20);
            cboCategorie = new ComboBox { Bounds = new Rectangle(232, 66, 200, 30), Font = Theme.Police(11f), DropDownStyle = ComboBoxStyle.DropDown };
            foreach (string nom in Catalogue.Noms) cboCategorie.Items.Add(nom);   // catégories du commerce

            Label lTamp = Theme.Etiquette("Tampons requis", Theme.Police(10f), Theme.TexteDoux);
            lTamp.SetBounds(446, 44, 180, 20);
            numTampons = new NumericUpDown { Bounds = new Rectangle(446, 66, 90, 30), Font = Theme.Police(11f), Minimum = 1, Maximum = 50, Value = 9 };

            chkActive = new CheckBox { Text = "Active", Checked = true, AutoSize = true, Font = Theme.Police(10.5f, FontStyle.Bold), BackColor = Theme.GrisClair, Location = new Point(552, 70) };

            lblExemple = Theme.Etiquette("", Theme.Police(9.5f), Theme.Bleu);
            lblExemple.SetBounds(18, 104, 420, 22);

            BoutonArrondi btnEnregistrer = new BoutonArrondi
            {
                Text = "Enregistrer la carte",
                Font = Theme.Police(11f, FontStyle.Bold),
                BackColor = Theme.Vert,
                Bounds = new Rectangle(446, 124, 188, 40)
            };
            btnEnregistrer.Click += (s, e) => EnregistrerCarte();

            numTampons.ValueChanged += (s, e) => MettreAJourExemple();
            cboCategorie.TextChanged += (s, e) => MettreAJourExemple();

            edition.Controls.AddRange(new Control[] { lblEdition, lNom, txtNom, lCat, cboCategorie, lTamp, numTampons, chkActive, lblExemple, btnEnregistrer });

            BoutonArrondi btnFermer = new BoutonArrondi
            {
                Text = "Fermer",
                BackColor = Theme.Gris,
                ForeColor = Theme.Texte,
                Font = Theme.Police(11f, FontStyle.Bold),
                Bounds = new Rectangle(556, 546, 120, 40)
            };
            btnFermer.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { titre, aide, lvCartes, btnNouvelle, edition, btnFermer });

            Shown += (s, e) => Charger();
        }

        private void Charger(int selectionner = 0)
        {
            List<CarteFidelite> cartes;
            try
            {
                cartes = Donnees.ChargerCartes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de lire les cartes :\n\n" + ex.Message +
                    "", "Cartes fidélité",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lvCartes.BeginUpdate();
            lvCartes.Items.Clear();
            foreach (CarteFidelite c in cartes)
            {
                ListViewItem item = new ListViewItem(new[]
                {
                    c.Nom,
                    c.Categorie,
                    c.TamponsRequis + " achetés → 1 gratuit",
                    c.Active ? "Active" : "Désactivée"
                }) { Tag = c };
                if (!c.Active) item.ForeColor = Theme.TexteDoux;
                lvCartes.Items.Add(item);
            }
            lvCartes.EndUpdate();

            ListViewItem choisi = lvCartes.Items.Cast<ListViewItem>().FirstOrDefault(i => ((CarteFidelite)i.Tag).Id == selectionner);
            if (choisi != null) choisi.Selected = true;
            else if (lvCartes.Items.Count > 0 && selectionner == 0) lvCartes.Items[0].Selected = true;
            else Editer(null);
        }

        private void Editer(CarteFidelite c)
        {
            carteEnCours = c;
            lblEdition.Text = c == null ? "Nouvelle carte" : "Modifier : " + c.Nom;
            txtNom.Text = c == null ? "" : c.Nom;
            cboCategorie.Text = c != null ? c.Categorie : (cboCategorie.Items.Count > 0 ? (string)cboCategorie.Items[0] : "");
            numTampons.Value = c == null ? 5 : Math.Max(1, Math.Min(50, c.TamponsRequis));
            chkActive.Checked = c == null || c.Active;
            MettreAJourExemple();
        }

        private void MettreAJourExemple()
        {
            int n = (int)numTampons.Value;
            lblExemple.Text = n + " produits « " + cboCategorie.Text + " » achetés → le " + (n + 1) + "e est gratuit";
        }

        private void EnregistrerCarte()
        {
            string nom = txtNom.Text.Trim();
            string categorie = cboCategorie.Text.Trim();
            if (nom.Length == 0) { MessageBox.Show(this, "Donne un nom à la carte (ex. Sandwich).", "Cartes fidélité"); txtNom.Focus(); return; }
            if (categorie.Length == 0) { MessageBox.Show(this, "Choisis la catégorie de produits.", "Cartes fidélité"); cboCategorie.Focus(); return; }

            CarteFidelite c = carteEnCours ?? new CarteFidelite();
            c.Nom = nom;
            c.Categorie = categorie;
            c.TamponsRequis = (int)numTampons.Value;
            c.Active = chkActive.Checked;

            try
            {
                Donnees.EnregistrerCarte(c);
                Charger(c.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible d'enregistrer la carte :\n\n" + ex.Message, "Cartes fidélité",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
