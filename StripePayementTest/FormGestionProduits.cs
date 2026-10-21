using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OtmanePOS
{
    // Ajouter, modifier, supprimer les produits (remplace l'ancien FormProduits)
    public class FormGestionProduits : Form
    {
        private readonly TextBox txtRecherche;
        private readonly ListView lvProduits;
        private readonly Label lblEdition, lblImage;
        private readonly TextBox txtNom, txtCode, txtPrix, txtPrixPromo;
        private readonly ComboBox cboCategorie;
        private readonly NumericUpDown numStock, numQtePromo;
        private readonly CheckBox chkIllimite, chkTaxable;
        private readonly BoutonArrondi btnSupprimer;

        private List<Produit> produits = new List<Produit>();
        private Produit produitEnCours;   // null = nouveau produit
        private string imageChoisie;      // nom du fichier dans Base.DossierImages

        public FormGestionProduits()
        {
            Text = "Otmane POS - Gestion des produits";
            BackColor = Theme.Fond;
            Font = Theme.Police(10.5f);
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1180, 800);
            MinimumSize = new Size(1000, 700);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            TableLayoutPanel racine = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(24, 16, 24, 20), BackColor = Theme.Fond };
            racine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            racine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            racine.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label titre = Theme.Etiquette("Gestion des produits", Theme.Police(20f, FontStyle.Bold), Theme.Texte);
            titre.Dock = DockStyle.Fill;
            racine.Controls.Add(titre, 0, 0);
            racine.SetColumnSpan(titre, 2);

            // ================= LISTE =================
            PanneauArrondi carteListe = new PanneauArrondi { Dock = DockStyle.Fill, Bordure = Theme.Gris, Padding = new Padding(16, 12, 16, 16), Margin = new Padding(0, 0, 8, 0) };

            Panel barre = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White };
            txtRecherche = new TextBox { Font = Theme.Police(12f), Location = new Point(0, 8), Width = 300 };
            Theme.TexteIndicatif(txtRecherche, "Rechercher un produit...");
            txtRecherche.TextChanged += (s, e) => Remplir();
            BoutonArrondi btnNouveau = new BoutonArrondi
            {
                Text = "Nouveau produit",
                Icone = "\uE710",
                TailleIcone = 12f,
                Font = Theme.Police(10.5f, FontStyle.Bold),
                Size = new Size(180, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNouveau.Click += (s, e) => { lvProduits.SelectedItems.Clear(); Editer(null); txtNom.Focus(); };
            barre.Controls.Add(txtRecherche);
            barre.Controls.Add(btnNouveau);
            barre.Resize += (s, e) =>
            {
                btnNouveau.Location = new Point(barre.Width - btnNouveau.Width, 6);
                txtRecherche.Width = Math.Max(150, barre.Width - btnNouveau.Width - 16);
            };

            lvProduits = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                Font = Theme.Police(10.5f),
                SmallImageList = new ImageList { ImageSize = new Size(1, 30) }
            };
            lvProduits.Columns.Add("Produit", 220);
            lvProduits.Columns.Add("Catégorie", 120);
            lvProduits.Columns.Add("Prix", 90, HorizontalAlignment.Right);
            lvProduits.Columns.Add("Stock", 80, HorizontalAlignment.Right);
            lvProduits.Columns.Add("Code-barres", 130);
            lvProduits.Resize += (s, e) => lvProduits.Columns[0].Width = Math.Max(150, lvProduits.ClientSize.Width - 120 - 90 - 80 - 130 - 4);
            lvProduits.SelectedIndexChanged += (s, e) =>
            {
                if (lvProduits.SelectedItems.Count > 0) Editer((Produit)lvProduits.SelectedItems[0].Tag);
            };

            carteListe.Controls.Add(lvProduits);
            carteListe.Controls.Add(barre);
            racine.Controls.Add(carteListe, 0, 1);

            // ================= FICHE =================
            PanneauArrondi fiche = new PanneauArrondi { Dock = DockStyle.Fill, Bordure = Theme.Gris, Margin = new Padding(8, 0, 0, 0) };
            Panel defile = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            fiche.Padding = new Padding(8);
            fiche.Controls.Add(defile);

            int y = 10;
            lblEdition = Theme.Etiquette("Nouveau produit", Theme.Police(14f, FontStyle.Bold), Theme.Texte);
            lblEdition.SetBounds(14, y, 420, 32); defile.Controls.Add(lblEdition); y += 44;

            txtNom = Champ(defile, "Nom", ref y, 400);
            Label aide = Theme.Etiquette("Tailles : nomme-les « Latte - Petit », « Latte - Moyen », « Latte - Grand » : la caisse les regroupe sur une seule carte.",
                Theme.Police(9f), Theme.TexteDoux);
            aide.SetBounds(14, y - 6, 420, 36); defile.Controls.Add(aide); y += 34;

            txtPrix = Champ(defile, "Prix ($)", ref y, 140);
            Theme.TexteIndicatif(txtPrix, "0,00");

            Label lCat = Theme.Etiquette("Catégorie", Theme.Police(10f), Theme.TexteDoux);
            lCat.SetBounds(14, y, 300, 20); defile.Controls.Add(lCat); y += 22;
            cboCategorie = new ComboBox { Font = Theme.Police(11f), Width = 240, Location = new Point(14, y), DropDownStyle = ComboBoxStyle.DropDownList };
            BoutonArrondi btnCategories = new BoutonArrondi
            {
                Text = "Gérer...",
                BackColor = Theme.Gris,
                ForeColor = Theme.Texte,
                Font = Theme.Police(10f, FontStyle.Bold),
                Bounds = new Rectangle(262, y - 2, 100, 34)
            };
            btnCategories.Click += (s, e) => GererCategories();
            defile.Controls.Add(cboCategorie); defile.Controls.Add(btnCategories); y += 42;

            txtCode = Champ(defile, "Code-barres (facultatif)", ref y, 240);

            Label lStock = Theme.Etiquette("Stock", Theme.Police(10f), Theme.TexteDoux);
            lStock.SetBounds(14, y, 300, 20); defile.Controls.Add(lStock); y += 22;
            numStock = new NumericUpDown { Font = Theme.Police(11f), Width = 110, Location = new Point(14, y), Minimum = 0, Maximum = 9998 };
            chkIllimite = new CheckBox { Text = "Illimité (café, boissons faites sur place...)", AutoSize = true, Location = new Point(136, y + 3), BackColor = Color.White };
            chkIllimite.CheckedChanged += (s, e) => numStock.Enabled = !chkIllimite.Checked;
            defile.Controls.Add(numStock); defile.Controls.Add(chkIllimite); y += 44;

            chkTaxable = new CheckBox { Text = "Taxable (TPS et TVQ)", AutoSize = true, Checked = true, Location = new Point(14, y), BackColor = Color.White, Font = Theme.Police(10.5f, FontStyle.Bold) };
            defile.Controls.Add(chkTaxable); y += 36;

            Label lPromo = Theme.Etiquette("Promo (facultatif) : X pour Y $", Theme.Police(10f), Theme.TexteDoux);
            lPromo.SetBounds(14, y, 300, 20); defile.Controls.Add(lPromo); y += 22;
            numQtePromo = new NumericUpDown { Font = Theme.Police(11f), Width = 80, Location = new Point(14, y), Minimum = 0, Maximum = 99 };
            Label lPour = Theme.Etiquette("pour", Theme.Police(10.5f), Theme.Texte, ContentAlignment.MiddleCenter);
            lPour.SetBounds(98, y, 44, 30);
            txtPrixPromo = new TextBox { Font = Theme.Police(11f), Width = 100, Location = new Point(146, y) };
            Label lDollar = Theme.Etiquette("$   (0 = pas de promo)", Theme.Police(10f), Theme.TexteDoux);
            lDollar.SetBounds(252, y, 180, 30);
            defile.Controls.AddRange(new Control[] { numQtePromo, lPour, txtPrixPromo, lDollar }); y += 44;

            Label lImg = Theme.Etiquette("Photo (facultatif)", Theme.Police(10f), Theme.TexteDoux);
            lImg.SetBounds(14, y, 300, 20); defile.Controls.Add(lImg); y += 22;
            BoutonArrondi btnImage = new BoutonArrondi { Text = "Choisir une photo", BackColor = Theme.Gris, ForeColor = Theme.Texte, Font = Theme.Police(10f, FontStyle.Bold), Bounds = new Rectangle(14, y, 160, 34) };
            btnImage.Click += (s, e) => ChoisirImage();
            BoutonArrondi btnSansImage = new BoutonArrondi { Text = "Retirer", BackColor = Theme.GrisClair, ForeColor = Theme.TexteDoux, Font = Theme.Police(10f), Bounds = new Rectangle(182, y, 80, 34) };
            btnSansImage.Click += (s, e) => { imageChoisie = null; MajImage(); };
            lblImage = Theme.Etiquette("", Theme.Police(9.5f), Theme.TexteDoux);
            lblImage.SetBounds(270, y, 170, 34);
            defile.Controls.AddRange(new Control[] { btnImage, btnSansImage, lblImage }); y += 56;

            BoutonArrondi btnEnregistrer = new BoutonArrondi { Text = "Enregistrer", BackColor = Theme.Vert, Font = Theme.Police(12f, FontStyle.Bold), Bounds = new Rectangle(14, y, 200, 44) };
            btnEnregistrer.Click += (s, e) => Enregistrer();
            btnSupprimer = new BoutonArrondi { Text = "Supprimer", Icone = Theme.IcSuppr, TailleIcone = 12f, BackColor = Theme.RougeClair, ForeColor = Theme.Rouge, CouleurIcone = Theme.Rouge, Font = Theme.Police(11f, FontStyle.Bold), Bounds = new Rectangle(226, y, 150, 44) };
            btnSupprimer.Click += (s, e) => Supprimer();
            defile.Controls.Add(btnEnregistrer); defile.Controls.Add(btnSupprimer); y += 60;

            Panel fin = new Panel { Bounds = new Rectangle(14, y, 10, 10), BackColor = Color.White };
            defile.Controls.Add(fin);

            racine.Controls.Add(fiche, 1, 1);
            Controls.Add(racine);

            Shown += (s, e) => { RemplirCategories(); Charger(); Editer(null); };
        }

        private TextBox Champ(Panel parent, string libelle, ref int y, int largeur)
        {
            Label l = Theme.Etiquette(libelle, Theme.Police(10f), Theme.TexteDoux);
            l.SetBounds(14, y, 400, 20);
            TextBox tb = new TextBox { Font = Theme.Police(11f), Width = largeur, Location = new Point(14, y + 22) };
            parent.Controls.Add(l);
            parent.Controls.Add(tb);
            y += 64;
            return tb;
        }

        // =====================================================================
        private void RemplirCategories()
        {
            string choisie = cboCategorie.Text;
            cboCategorie.Items.Clear();
            foreach (string nom in Catalogue.Noms) cboCategorie.Items.Add(nom);
            if (cboCategorie.Items.Count == 0) cboCategorie.Items.Add("Autres");
            ChoisirCategorie(choisie);
        }

        private void ChoisirCategorie(string nom)
        {
            int i = -1;
            for (int k = 0; k < cboCategorie.Items.Count; k++)
                if (string.Equals((string)cboCategorie.Items[k], nom, StringComparison.OrdinalIgnoreCase)) { i = k; break; }
            if (i < 0 && !string.IsNullOrEmpty(nom))
            {
                cboCategorie.Items.Add(nom);          // catégorie d'un ancien produit, pas encore dans la liste
                i = cboCategorie.Items.Count - 1;
            }
            cboCategorie.SelectedIndex = i >= 0 ? i : 0;
        }

        private void GererCategories()
        {
            int garder = produitEnCours != null ? produitEnCours.Id : 0;
            using (FormCategories f = new FormCategories()) f.ShowDialog(this);
            Catalogue.Charger();
            RemplirCategories();
            Charger(garder);       // un renommage ou une suppression a pu changer les produits
            if (garder == 0) Editer(null);
        }

        private void Charger(int selectionner = 0)
        {
            try
            {
                produits = Donnees.ChargerProduits().OrderBy(p => p.Nom).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de lire les produits :\n\n" + ex.Message, "Produits", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Remplir(selectionner);
        }

        private void Remplir(int selectionner = 0)
        {
            string filtre = txtRecherche.Text.Trim();
            lvProduits.BeginUpdate();
            lvProduits.Items.Clear();
            foreach (Produit p in produits)
            {
                if (filtre.Length > 0 && p.Nom.IndexOf(filtre, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (p.CodeBarre ?? "").IndexOf(filtre, StringComparison.OrdinalIgnoreCase) < 0) continue;

                ListViewItem item = new ListViewItem(new[]
                {
                    p.Nom,
                    p.Categorie ?? "",
                    Theme.Argent(p.Prix),
                    p.StockIllimite ? "illimité" : p.Stock.ToString(),
                    p.CodeBarre ?? ""
                }) { Tag = p };
                if (!p.StockIllimite && p.Stock <= 5) item.ForeColor = Theme.Rouge;   // bientôt en rupture
                lvProduits.Items.Add(item);
                if (p.Id == selectionner) item.Selected = true;
            }
            lvProduits.EndUpdate();
            if (lvProduits.SelectedItems.Count > 0) lvProduits.SelectedItems[0].EnsureVisible();
        }

        private void Editer(Produit p)
        {
            produitEnCours = p;
            lblEdition.Text = p == null ? "Nouveau produit" : "Modifier : " + p.Nom;
            txtNom.Text = p == null ? "" : p.Nom;
            txtPrix.Text = p == null ? "" : p.Prix.ToString("0.00", Theme.Culture);
            ChoisirCategorie(p == null ? (cboCategorie.Items.Count > 0 ? (string)cboCategorie.Items[0] : "Autres") : (p.Categorie ?? "Autres"));
            txtCode.Text = p == null ? "" : (p.CodeBarre ?? "");
            chkIllimite.Checked = p == null || p.StockIllimite;
            numStock.Value = p == null || p.StockIllimite ? 0 : Math.Max(0, Math.Min(9998, p.Stock));
            chkTaxable.Checked = p == null || p.Taxable;
            numQtePromo.Value = p != null && p.QuantitePromo.HasValue ? Math.Min(99, Math.Max(0, p.QuantitePromo.Value)) : 0;
            txtPrixPromo.Text = p != null && p.PrixPromo.HasValue ? p.PrixPromo.Value.ToString("0.00", Theme.Culture) : "";
            imageChoisie = p == null ? null : p.FichierImage;
            btnSupprimer.Visible = p != null;
            MajImage();
        }

        private void MajImage()
        {
            lblImage.Text = string.IsNullOrEmpty(imageChoisie) ? "Aucune (emoji affiché)" : imageChoisie;
        }

        private void ChoisirImage()
        {
            using (OpenFileDialog choix = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif", Title = "Photo du produit" })
            {
                if (choix.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    // Copiée dans le dossier de données, pour ne pas dépendre de l'emplacement d'origine
                    Directory.CreateDirectory(Base.DossierImages);
                    string nom = Path.GetFileName(choix.FileName);
                    string destination = Path.Combine(Base.DossierImages, nom);
                    if (!string.Equals(Path.GetFullPath(choix.FileName), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                        File.Copy(choix.FileName, destination, true);
                    imageChoisie = nom;
                    MajImage();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Impossible de copier la photo :\n\n" + ex.Message, "Produits", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static bool LireMontant(string texte, out decimal valeur)
        {
            string s = (texte ?? "").Trim().Replace('.', ',').Replace("$", "").Trim();
            return decimal.TryParse(s, NumberStyles.Number, Theme.Culture, out valeur) && valeur >= 0;
        }

        private void Enregistrer()
        {
            string nom = txtNom.Text.Trim();
            if (nom.Length == 0) { MessageBox.Show(this, "Le nom du produit est obligatoire.", "Produits"); txtNom.Focus(); return; }

            decimal prix;
            if (!LireMontant(txtPrix.Text, out prix)) { MessageBox.Show(this, "Prix invalide (exemple : 3,50).", "Produits"); txtPrix.Focus(); return; }

            string code = txtCode.Text.Trim();
            if (code.Length > 0 && produits.Any(x => x != produitEnCours && string.Equals(x.CodeBarre, code, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, "Ce code-barres est déjà utilisé par un autre produit.", "Produits");
                txtCode.Focus();
                return;
            }

            int? qtePromo = null;
            decimal? prixPromo = null;
            if (numQtePromo.Value > 1)
            {
                decimal pp;
                if (!LireMontant(txtPrixPromo.Text, out pp) || pp <= 0)
                {
                    MessageBox.Show(this, "Indique le prix de la promo (exemple : 3 pour 5,00 $).", "Produits");
                    txtPrixPromo.Focus();
                    return;
                }
                qtePromo = (int)numQtePromo.Value;
                prixPromo = pp;
            }

            Produit p = produitEnCours ?? new Produit();
            p.Nom = nom;
            p.Prix = prix;
            p.Categorie = cboCategorie.Text.Trim();
            p.CodeBarre = code;
            p.Stock = chkIllimite.Checked ? 9999 : (int)numStock.Value;
            p.Taxable = chkTaxable.Checked;
            p.QuantitePromo = qtePromo;
            p.PrixPromo = prixPromo;
            p.FichierImage = imageChoisie;

            try
            {
                Donnees.EnregistrerProduit(p);
                Charger(p.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible d'enregistrer le produit :\n\n" + ex.Message, "Produits", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Supprimer()
        {
            if (produitEnCours == null) return;
            if (MessageBox.Show(this, "Supprimer « " + produitEnCours.Nom + " » ?\n\nLes ventes passées restent dans l'historique.",
                    "Produits", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                Donnees.SupprimerProduit(produitEnCours.Id);
                Charger();
                Editer(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de supprimer :\n\n" + ex.Message, "Produits", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
