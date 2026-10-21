using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OtmanePOS
{
    // Ajouter, renommer, ordonner et supprimer les catégories de produits
    public class FormCategories : Form
    {
        private readonly ListView lvCategories;
        private readonly TextBox txtNom, txtEmoji;
        private readonly Label lblEdition;
        private readonly BoutonArrondi btnSupprimer;

        private List<CategorieProduit> categories = new List<CategorieProduit>();
        private Dictionary<string, int> nbProduits = new Dictionary<string, int>();
        private CategorieProduit enCours;   // null = nouvelle catégorie

        public FormCategories()
        {
            Text = "Catégories";
            BackColor = Color.White;
            Font = Theme.Police(10.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            Label titre = Theme.Etiquette("Catégories", Theme.Police(18f, FontStyle.Bold), Theme.Texte);
            titre.SetBounds(24, 16, 400, 40);
            Label aide = Theme.Etiquette("Chaque catégorie devient un bouton dans la caisse (seulement si elle contient des produits). " +
                "Utilise ↑ ↓ pour choisir l'ordre des boutons.", Theme.Police(9.5f), Theme.TexteDoux);
            aide.SetBounds(24, 56, 572, 40);

            lvCategories = new ListView
            {
                Bounds = new Rectangle(24, 102, 520, 250),
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.Police(11f),
                SmallImageList = new ImageList { ImageSize = new Size(1, 30) }
            };
            lvCategories.Columns.Add("", 50);
            lvCategories.Columns.Add("Catégorie", 300);
            lvCategories.Columns.Add("Produits", 140, HorizontalAlignment.Right);
            lvCategories.SelectedIndexChanged += (s, e) =>
            {
                if (lvCategories.SelectedItems.Count > 0) Editer((CategorieProduit)lvCategories.SelectedItems[0].Tag);
            };

            BoutonArrondi btnHaut = FlecheOrdre("\uE70E", 102);    // flèche haut
            btnHaut.Click += (s, e) => Deplacer(-1);
            BoutonArrondi btnBas = FlecheOrdre("\uE70D", 150);     // flèche bas
            btnBas.Click += (s, e) => Deplacer(1);

            BoutonArrondi btnNouvelle = new BoutonArrondi
            {
                Text = "Nouvelle catégorie",
                Icone = "\uE710",
                TailleIcone = 12f,
                BackColor = Theme.Gris,
                ForeColor = Theme.Texte,
                CouleurIcone = Theme.Texte,
                Font = Theme.Police(10.5f, FontStyle.Bold),
                Bounds = new Rectangle(24, 362, 200, 38)
            };
            btnNouvelle.Click += (s, e) => { lvCategories.SelectedItems.Clear(); Editer(null); txtNom.Focus(); };

            // ---- Édition ----
            PanneauArrondi edition = new PanneauArrondi { Bounds = new Rectangle(24, 412, 572, 124), BackColor = Theme.GrisClair, Rayon = 12 };
            lblEdition = Theme.Etiquette("Nouvelle catégorie", Theme.Police(12f, FontStyle.Bold), Theme.Texte);
            lblEdition.SetBounds(16, 10, 400, 26);

            Label lNom = Theme.Etiquette("Nom", Theme.Police(10f), Theme.TexteDoux);
            lNom.SetBounds(16, 40, 200, 20);
            txtNom = new TextBox { Bounds = new Rectangle(16, 62, 250, 30), Font = Theme.Police(11f) };
            Theme.TexteIndicatif(txtNom, "ex. Pizzas");

            Label lEmoji = Theme.Etiquette("Emoji (facultatif)", Theme.Police(10f), Theme.TexteDoux);
            lEmoji.SetBounds(280, 40, 160, 20);
            txtEmoji = new TextBox { Bounds = new Rectangle(280, 62, 70, 30), Font = new Font("Segoe UI Emoji", 12f), MaxLength = 8 };
            Theme.TexteIndicatif(txtEmoji, "🍕");
            txtNom.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Enregistrer(); } };

            BoutonArrondi btnEnregistrer = new BoutonArrondi { Text = "Enregistrer", BackColor = Theme.Vert, Font = Theme.Police(11f, FontStyle.Bold), Bounds = new Rectangle(366, 58, 190, 38) };
            btnEnregistrer.Click += (s, e) => Enregistrer();

            edition.Controls.AddRange(new Control[] { lblEdition, lNom, txtNom, lEmoji, txtEmoji, btnEnregistrer });

            btnSupprimer = new BoutonArrondi
            {
                Text = "Supprimer",
                Icone = Theme.IcSuppr,
                TailleIcone = 12f,
                BackColor = Theme.RougeClair,
                ForeColor = Theme.Rouge,
                CouleurIcone = Theme.Rouge,
                Font = Theme.Police(10.5f, FontStyle.Bold),
                Bounds = new Rectangle(24, 548, 150, 38)
            };
            btnSupprimer.Click += (s, e) => Supprimer();

            BoutonArrondi btnFermer = new BoutonArrondi
            {
                Text = "Fermer",
                BackColor = Theme.Gris,
                ForeColor = Theme.Texte,
                Font = Theme.Police(11f, FontStyle.Bold),
                Bounds = new Rectangle(476, 548, 120, 38)
            };
            btnFermer.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { titre, aide, lvCategories, btnHaut, btnBas, btnNouvelle, edition, btnSupprimer, btnFermer });
            Shown += (s, e) => Charger();
        }

        private BoutonArrondi FlecheOrdre(string icone, int y)
        {
            return new BoutonArrondi
            {
                Text = "",
                Icone = icone,
                TailleIcone = 12f,
                BackColor = Theme.Gris,
                CouleurIcone = Theme.Texte,
                Bounds = new Rectangle(554, y, 42, 40)
            };
        }

        // =====================================================================
        private void Charger(int selectionner = 0)
        {
            try
            {
                categories = Donnees.ChargerCategories();
                nbProduits = Donnees.CompterProduitsParCategorie();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de lire les catégories :\n\n" + ex.Message, "Catégories", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lvCategories.BeginUpdate();
            lvCategories.Items.Clear();
            foreach (CategorieProduit c in categories)
            {
                int nb;
                nbProduits.TryGetValue(c.Nom, out nb);
                ListViewItem item = new ListViewItem(new[] { c.Emoji ?? "", c.Nom, nb + " produit" + (nb > 1 ? "s" : "") }) { Tag = c };
                if (nb == 0) item.ForeColor = Theme.TexteDoux;
                lvCategories.Items.Add(item);
                if (c.Id == selectionner) item.Selected = true;
            }
            lvCategories.EndUpdate();

            if (lvCategories.SelectedItems.Count > 0) lvCategories.SelectedItems[0].EnsureVisible();
            else Editer(null);

            Catalogue.Charger();   // la caisse et les autres écrans voient les changements
        }

        private void Editer(CategorieProduit c)
        {
            enCours = c;
            lblEdition.Text = c == null ? "Nouvelle catégorie" : "Modifier : " + c.Nom;
            txtNom.Text = c == null ? "" : c.Nom;
            txtEmoji.Text = c == null ? "" : (c.Emoji ?? "");
            btnSupprimer.Visible = c != null;
        }

        private void Enregistrer()
        {
            string nom = txtNom.Text.Trim();
            if (nom.Length == 0) { MessageBox.Show(this, "Donne un nom à la catégorie.", "Catégories"); txtNom.Focus(); return; }
            if (string.Equals(nom, "Tous", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "« Tous » est réservé : c'est le bouton qui affiche tous les produits.", "Catégories");
                return;
            }
            if (categories.Any(x => x != enCours && string.Equals(x.Nom, nom, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, "Cette catégorie existe déjà.", "Catégories");
                return;
            }

            string ancienNom = enCours != null ? enCours.Nom : null;
            CategorieProduit c = enCours ?? new CategorieProduit();
            c.Nom = nom;
            c.Emoji = txtEmoji.Text.Trim();

            try
            {
                Donnees.EnregistrerCategorie(c, ancienNom);
                Charger(c.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible d'enregistrer :\n\n" + ex.Message, "Catégories", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Supprimer()
        {
            if (enCours == null) return;
            if (string.Equals(enCours.Nom, "Autres", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "« Autres » ne peut pas être supprimée : elle reçoit les produits des catégories supprimées.", "Catégories");
                return;
            }

            int nb;
            nbProduits.TryGetValue(enCours.Nom, out nb);
            bool carte = Fidelite.Cartes.Any(x => x.Active && string.Equals(x.Categorie, enCours.Nom, StringComparison.OrdinalIgnoreCase));
            string message = "Supprimer la catégorie « " + enCours.Nom + " » ?";
            if (nb > 0) message += "\n\nSes " + nb + " produit(s) iront dans « Autres ».";
            if (carte) message += "\n\nLa carte fidélité liée à cette catégorie sera désactivée.";

            if (MessageBox.Show(this, message, "Catégories", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                Donnees.SupprimerCategorie(enCours, "Autres");
                Fidelite.Charger();
                Charger();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de supprimer :\n\n" + ex.Message, "Catégories", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Deplacer(int sens)
        {
            if (enCours == null) return;
            int i = categories.IndexOf(enCours);
            int j = i + sens;
            if (i < 0 || j < 0 || j >= categories.Count) return;

            categories[i] = categories[j];
            categories[j] = enCours;
            try
            {
                Donnees.EnregistrerOrdreCategories(categories);
                Charger(enCours.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de changer l'ordre :\n\n" + ex.Message, "Catégories", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
