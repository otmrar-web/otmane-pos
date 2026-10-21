using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace OtmanePOS
{
    public class FormCaisse : Form
    {
        // Taxes du Québec
        // Taux réglables dans Paramètres
        private static decimal TauxTPS { get { return Parametres.TauxTPS; } }
        private static decimal TauxTVQ { get { return Parametres.TauxTVQ; } }

        private List<Produit> tousLesProduits = new List<Produit>();   // toutes les lignes de la table
        private List<Produit> produits = new List<Produit>();          // cartes affichées (tailles regroupées)
        private readonly List<LignePanier> panier = new List<LignePanier>();
        private readonly Dictionary<string, Image> cacheImages = new Dictionary<string, Image>();
        private readonly List<BoutonArrondi> boutonsMenu = new List<BoutonArrondi>();
        private readonly List<BoutonArrondi> boutonsCategories = new List<BoutonArrondi>();
        private FlowLayoutPanel flowCategories;

        private string categorieActive = "Tous";
        private LignePanier ligneSelectionnee;
        private bool cameraActive;
        private bool paiementEnCours;
        private Vente venteEnAttente;   // vente Stripe Checkout en attente de la réponse du navigateur
        private Vente derniereVente;    // pour « Imprimer ticket »

        // Fidélité (carte de tampons)
        private Client clientActuel;
        private bool utiliserGratuits = true;
        private List<ResultatCarte> resultatsFidelite = new List<ResultatCarte>();   // une ligne par carte active
        private Label lblClientNom, lblRabais, lblRabaisLibelle;
        private readonly List<Label> lignesCartes = new List<Label>();
        private CheckBox chkCafeGratuit;
        private BoutonArrondi btnClient, btnRetirerClient;
        private TableLayoutPanel ttTotaux;
        private PanneauArrondi panBandeClient;
        private Label lblClientIcone;
        private Label lblTPSLibelle, lblTVQLibelle, lblNomCommerce;
        private Control enveloppeClient;
        public decimal Rabais { get; private set; }

        // Webcam (AForge) + décodeur de codes-barres de ton projet
        private VideoCaptureDevice videoSource;
        private readonly StripePayementTest.Reader lecteurCodes = new StripePayementTest.Reader();
        private readonly object verrouImage = new object();
        private Bitmap derniereImage;
        private System.Windows.Forms.Timer minuteurCamera;

        private TextBox txtRecherche, txtMontantRecu;
        private FlowLayoutPanel flowProduits, flowPanier;
        private Label lblPanier, lblSousTotal, lblTPS, lblTVQ, lblRendu;
        private PanneauArrondi panTotaux;
        private PanneauArrondi panCamera;

        /// <summary>Ton flux webcam s'affiche ici.</summary>
        public PictureBox pbCamera;

        public decimal SousTotal { get; private set; }
        public decimal TPS { get; private set; }
        public decimal TVQ { get; private set; }
        public decimal Total { get; private set; }

        public FormCaisse()
        {
            Text = "Otmane POS - Caisse";
            BackColor = Theme.Fond;
            Font = Theme.Police(10f);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1440, 900);
            MinimumSize = new Size(1280, 780);
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;

            // Base SQLite : créée automatiquement au premier lancement
            try
            {
                Base.Initialiser();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir la base de données :\n" + Base.Fichier + "\n\n" + ex.Message,
                    "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Parametres.Charger();
            Catalogue.Charger();   // avant les produits : sert à deviner catégories et emojis
            Fidelite.Charger();
            ChargerProduits();

            Control principal = CreerZonePrincipale();
            Control menu = CreerMenuLateral();
            Controls.Add(principal);
            Controls.Add(menu);
            principal.BringToFront();

            AfficherProduits();
            RafraichirPanier();

            // Stripe : la clé du commerce vient de Paramètres (on n'en parle qu'au moment d'un paiement par carte)
            ServiceStripe.Initialiser();
            ServiceStripe.DemarrerServeurLocal(StripeReussi, StripeAnnule,
                msg => MessageBox.Show(msg, "Stripe", MessageBoxButtons.OK, MessageBoxIcon.Warning));
        }

        // =====================================================================
        // PRODUITS : chargés depuis la table Produits de MaBase
        // Les images vont dans un dossier "Images" à côté du .exe
        // (ex. bin\Debug\Images\latte.png). Sans image, un emoji est affiché.
        // =====================================================================
        private void ChargerProduits()
        {
            try
            {
                tousLesProduits = Donnees.ChargerProduits();
                produits = Donnees.RegrouperParTaille(tousLesProduits);
            }
            catch (Exception ex)
            {
                tousLesProduits = new List<Produit>();
                produits = new List<Produit>();
                MessageBox.Show("Impossible de charger les produits depuis la base de données.\n\n" + ex.Message +
                    "\n\nFichier de la base : " + Base.Fichier, "Otmane POS",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Image ImageDe(Produit p)
        {
            if (string.IsNullOrEmpty(p.FichierImage)) return null;
            Image img;
            if (cacheImages.TryGetValue(p.FichierImage, out img)) return img;

            img = null;
            // Photos ajoutées dans « Gestion des produits » (dossier de données), sinon à côté du .exe
            string chemin = Path.Combine(Base.DossierImages, p.FichierImage);
            if (!File.Exists(chemin)) chemin = Path.Combine(Application.StartupPath, "Images", p.FichierImage);
            if (File.Exists(chemin))
            {
                try
                {
                    using (FileStream fs = new FileStream(chemin, FileMode.Open, FileAccess.Read))
                    using (Image tmp = Image.FromStream(fs))
                        img = new Bitmap(tmp);
                }
                catch { img = null; }
            }
            cacheImages[p.FichierImage] = img;
            return img;
        }

        // =====================================================================
        // MENU LATÉRAL
        // =====================================================================
        private Control CreerMenuLateral()
        {
            Panel menu = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Sidebar, Padding = new Padding(12, 20, 12, 16) };

            Label icLogo = Theme.Etiquette(Theme.IcPanier, Theme.Icone(30f), Color.White, ContentAlignment.MiddleCenter);
            icLogo.Bounds = new Rectangle(12, 20, 60, 70);
            Label txtLogo = Theme.Etiquette("Otmane\nPOS", Theme.Police(17f, FontStyle.Bold), Color.White);
            txtLogo.Bounds = new Rectangle(76, 18, 150, 74);
            menu.Controls.Add(icLogo);
            menu.Controls.Add(txtLogo);

            string[,] items =
            {
                { "Caisse", Theme.IcPanier },
                { "Produits", Theme.IcProduit },
                { "Ventes", Theme.IcVentes },
                { "Clients", Theme.IcClients },
                { "Rapports", Theme.IcRapports },
                { "Paramètres", Theme.IcParam }
            };

            int y = 118;
            for (int i = 0; i < items.GetLength(0); i++)
            {
                string nom = items[i, 0];
                BoutonArrondi b = new BoutonArrondi
                {
                    Text = nom,
                    Icone = items[i, 1],
                    TailleIcone = 17f,
                    AlignerGauche = true,
                    Bounds = new Rectangle(12, y, 206, 56)
                };
                b.Click += (s, e) => NaviguerVers(nom);
                boutonsMenu.Add(b);
                menu.Controls.Add(b);
                y += 64;
            }
            ActiverMenu("Caisse");

            // Pied : commerce / utilisateur
            PanneauArrondi pied = new PanneauArrondi { Dock = DockStyle.Bottom, Height = 112, BackColor = Theme.SidebarPied, Rayon = 12 };
            Color doux = Color.FromArgb(170, 185, 205);

            Label ic1 = Theme.Etiquette(Theme.IcMagasin, Theme.Icone(18f), Color.White, ContentAlignment.MiddleCenter);
            ic1.Bounds = new Rectangle(10, 12, 40, 36);
            Label t1 = Theme.Etiquette(Parametres.NomCommerce, Theme.Police(11.5f, FontStyle.Bold), Color.White);
            lblNomCommerce = t1;
            t1.AutoSize = true;
            t1.Location = new Point(54, 18);
            Label ic2 = Theme.Etiquette(Theme.IcPersonne, Theme.Icone(11f), doux, ContentAlignment.MiddleCenter);
            ic2.Bounds = new Rectangle(16, 54, 26, 24);
            Label t2 = Theme.Etiquette("Admin", Theme.Police(10.5f), doux);
            t2.AutoSize = true;
            t2.Location = new Point(46, 56);
            Label ic3 = Theme.Etiquette("●", Theme.Police(11f), Color.FromArgb(34, 197, 94), ContentAlignment.MiddleCenter);
            ic3.Bounds = new Rectangle(16, 78, 26, 24);
            Label t3 = Theme.Etiquette("En ligne", Theme.Police(10.5f), Color.White);
            t3.AutoSize = true;
            t3.Location = new Point(46, 80);
            pied.Controls.AddRange(new Control[] { ic1, t1, ic2, t2, ic3, t3 });
            menu.Controls.Add(pied);

            return menu;
        }

        private void ActiverMenu(string nom)
        {
            foreach (BoutonArrondi b in boutonsMenu)
            {
                bool actif = b.Text == nom;
                b.BackColor = actif ? Theme.SidebarActif : Theme.Sidebar;
                b.ForeColor = actif ? Theme.Sidebar : Color.White;
                b.CouleurSurvol = actif ? Theme.SidebarActif : Theme.SidebarSurvol;
                b.Font = Theme.Police(12f, actif ? FontStyle.Bold : FontStyle.Regular);
                b.Invalidate();
            }
        }

        private void NaviguerVers(string nom)
        {
            switch (nom)
            {
                case "Caisse": ActiverMenu("Caisse"); break;
                case "Produits": OuvrirProduits(); break;
                case "Ventes": OuvrirHistorique(); break;
                case "Clients": OuvrirClients(); break;
                case "Rapports": OuvrirRapports(); break;
                case "Paramètres": OuvrirParametres(); break;
                default:
                    MessageBox.Show("La section « " + nom + " » n'est pas encore branchée.", "Otmane POS");
                    break;
            }
        }

        // Tes formulaires existants
        private void OuvrirHistorique()
        {
            using (var form = new FormHistorique()) form.ShowDialog(this);
        }

        private void OuvrirProduits()
        {
            using (var form = new FormGestionProduits()) form.ShowDialog(this);
            cacheImages.Clear();   // les photos ont pu changer
            Catalogue.Charger();   // les catégories ont pu changer
            // Recharge au retour : prix, stock, nouveaux produits et catégories
            ChargerProduits();
            ConstruireBoutonsCategories();
            AfficherProduits();
        }

        private void OuvrirParametres()
        {
            using (var form = new FormParametres())
                if (form.ShowDialog(this) == DialogResult.OK) AppliquerParametres();
        }

        // Met l'écran à jour après un changement de paramètres
        private void AppliquerParametres()
        {
            lblNomCommerce.Text = Parametres.NomCommerce;
            lblTPSLibelle.Text = Parametres.LibelleTPS + " :";
            lblTVQLibelle.Text = Parametres.LibelleTVQ + " :";
            Fidelite.Charger();   // les cartes ont pu changer
            ServiceStripe.Initialiser();   // la clé Stripe a pu changer
            enveloppeClient.Visible = Fidelite.Actif;
            if (!Fidelite.Actif) clientActuel = null;
            RafraichirPanier();   // recalcule taxes, fidélité et totaux
        }

        private void OuvrirRapports()
        {
            using (var form = new FormRapports()) form.ShowDialog(this);
        }

        private void OuvrirClients()
        {
            // Chercher ou créer une carte fidélité, et l'associer à la vente en cours
            ChoisirClient();
        }

        // =====================================================================
        // ZONE PRINCIPALE
        // =====================================================================
        private Control CreerZonePrincipale()
        {
            TableLayoutPanel zone = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Fond,
                Padding = new Padding(20, 20, 20, 16),
                ColumnCount = 1,
                RowCount = 3
            };
            zone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            zone.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            zone.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            zone.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));

            zone.Controls.Add(CreerBarreHaut(), 0, 0);
            zone.Controls.Add(CreerContenu(), 0, 1);
            zone.Controls.Add(CreerActionsRapides(), 0, 2);
            return zone;
        }

        private Label Titre(string texte)
        {
            Label l = Theme.Etiquette(texte, Theme.Police(16f, FontStyle.Bold), Theme.Texte);
            l.Dock = DockStyle.Fill;
            return l;
        }

        // ---- Barre du haut : recherche + Scanner + Caméra ----
        private Control CreerBarreHaut()
        {
            TableLayoutPanel barre = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            barre.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            barre.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            barre.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            barre.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            PanneauArrondi recherche = new PanneauArrondi { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 0), Bordure = Theme.Gris, Rayon = 10 };
            Label loupe = Theme.Etiquette(Theme.IcRecherche, Theme.Icone(15f), Theme.Texte, ContentAlignment.MiddleCenter);
            loupe.Size = new Size(40, 40);
            txtRecherche = new TextBox { BorderStyle = BorderStyle.None, Font = Theme.Police(13f), ForeColor = Theme.Texte, BackColor = Color.White };
            Theme.TexteIndicatif(txtRecherche, "Rechercher un produit ou scanner un code-barres...");
            recherche.Controls.Add(loupe);
            recherche.Controls.Add(txtRecherche);
            recherche.Resize += (s, e) =>
            {
                loupe.Location = new Point(12, (recherche.Height - loupe.Height) / 2);
                txtRecherche.Location = new Point(58, (recherche.Height - txtRecherche.Height) / 2);
                txtRecherche.Width = Math.Max(50, recherche.Width - 74);
            };
            recherche.Click += (s, e) => txtRecherche.Focus();
            loupe.Click += (s, e) => txtRecherche.Focus();
            txtRecherche.TextChanged += (s, e) => AfficherProduits();
            txtRecherche.KeyDown += RechercheKeyDown;

            BoutonArrondi btnScanner = new BoutonArrondi
            {
                Text = "Scanner",
                Icone = Theme.IcScanner,
                TailleIcone = 18f,
                Font = Theme.Police(12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 14, 0)
            };
            // Douchette USB : elle "tape" le code puis Entrée dans la zone de recherche
            btnScanner.Click += (s, e) => { txtRecherche.Clear(); txtRecherche.Focus(); };

            BoutonArrondi btnCamera = new BoutonArrondi
            {
                Text = "Caméra",
                Icone = Theme.IcCamera,
                TailleIcone = 18f,
                Font = Theme.Police(12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            btnCamera.Click += (s, e) => BasculerCamera();

            barre.Controls.Add(recherche, 0, 0);
            barre.Controls.Add(btnScanner, 1, 0);
            barre.Controls.Add(btnCamera, 2, 0);
            return barre;
        }

        // ---- Contenu : Produits | Panier | Paiement ----
        private Control CreerContenu()
        {
            TableLayoutPanel contenu = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 18, 0, 14) };
            contenu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            contenu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
            contenu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21));
            contenu.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            contenu.Controls.Add(CreerColonneProduits(), 0, 0);
            contenu.Controls.Add(CreerColonnePanier(), 1, 0);
            contenu.Controls.Add(CreerColonnePaiement(), 2, 0);
            return contenu;
        }

        private Control CreerColonneProduits()
        {
            Panel col = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 0) };

            Label titre = Titre("Produits");
            titre.Dock = DockStyle.Top;
            titre.Height = 40;

            FlowLayoutPanel cats = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(0, 4, 0, 8)
            };
            flowCategories = cats;
            ConstruireBoutonsCategories();

            flowProduits = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 6, 0, 0) };

            panCamera = new PanneauArrondi { Dock = DockStyle.Bottom, Height = 240, Visible = false, BackColor = Color.Black, Padding = new Padding(8), Rayon = 12 };
            pbCamera = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
            panCamera.Controls.Add(pbCamera);

            // Ordre d'ajout important pour le Dock
            col.Controls.Add(flowProduits);
            col.Controls.Add(panCamera);
            col.Controls.Add(cats);
            col.Controls.Add(titre);
            return col;
        }

        // Un bouton « Tous » + un bouton par catégorie qui contient des produits (ordre de la table Categories)
        private void ConstruireBoutonsCategories()
        {
            if (flowCategories == null) return;

            HashSet<string> utilisees = new HashSet<string>(
                produits.Where(p => !string.IsNullOrEmpty(p.Categorie)).Select(p => p.Categorie), StringComparer.OrdinalIgnoreCase);
            List<string> noms = new List<string> { "Tous" };
            noms.AddRange(Catalogue.Noms.Where(n => utilisees.Contains(n)));

            if (!noms.Contains(categorieActive, StringComparer.OrdinalIgnoreCase)) categorieActive = "Tous";

            flowCategories.SuspendLayout();
            foreach (BoutonArrondi ancien in boutonsCategories) ancien.Dispose();
            boutonsCategories.Clear();
            flowCategories.Controls.Clear();

            foreach (string c in noms)
            {
                string cat = c;
                Font f = Theme.Police(11f);
                BoutonArrondi b = new BoutonArrondi
                {
                    Text = cat,
                    Font = f,
                    Tag = cat,
                    Size = new Size(Math.Max(90, TextRenderer.MeasureText(cat, f).Width + 36), 44),
                    Margin = new Padding(0, 0, 10, 8)
                };
                b.Click += (s, e) => { categorieActive = cat; ActiverCategorie(); AfficherProduits(); };
                boutonsCategories.Add(b);
                flowCategories.Controls.Add(b);
            }
            flowCategories.ResumeLayout();
            ActiverCategorie();
        }

        private void ActiverCategorie()
        {
            foreach (BoutonArrondi b in boutonsCategories)
            {
                bool actif = string.Equals((string)b.Tag, categorieActive, StringComparison.OrdinalIgnoreCase);
                b.BackColor = actif ? Theme.Bleu : Theme.Gris;
                b.ForeColor = actif ? Color.White : Theme.Texte;
                b.Invalidate();
            }
        }

        private Control CreerColonnePanier()
        {
            PanneauArrondi carte = new PanneauArrondi { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 0), Padding = new Padding(16, 12, 16, 16), Bordure = Theme.Gris };

            lblPanier = Titre("Panier (0)");
            lblPanier.Dock = DockStyle.Top;
            lblPanier.Height = 48;

            EntetePanier entete = new EntetePanier();

            flowPanier = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White
            };
            flowPanier.Resize += (s, e) => AjusterLignes();

            TableLayoutPanel boutons = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 64, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 12, 0, 0), BackColor = Color.White };
            boutons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            boutons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            boutons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            BoutonArrondi btnVider = new BoutonArrondi
            {
                Text = "Vider le panier",
                Icone = Theme.IcSuppr,
                TailleIcone = 13f,
                BackColor = Theme.Rouge,
                Font = Theme.Police(10.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0)
            };
            BoutonArrondi btnSupprSel = new BoutonArrondi
            {
                Text = "Supprimer",
                Icone = Theme.IcSuppr,
                TailleIcone = 13f,
                BackColor = Theme.RougeClair,
                ForeColor = Theme.Rouge,
                Font = Theme.Police(10f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnVider.Click += (s, e) => ViderPanier(true);
            btnSupprSel.Click += (s, e) => SupprimerSelection();
            boutons.Controls.Add(btnVider, 0, 0);
            boutons.Controls.Add(btnSupprSel, 1, 0);

            carte.Controls.Add(flowPanier);
            carte.Controls.Add(boutons);
            carte.Controls.Add(entete);
            carte.Controls.Add(CreerBandeClient());
            carte.Controls.Add(lblPanier);
            return carte;
        }

        // Bande « cartes fidélité » sous le titre du panier (une ligne par carte)
        private Control CreerBandeClient()
        {
            Panel enveloppe = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.White, Padding = new Padding(0, 0, 0, 10) };
            PanneauArrondi bande = new PanneauArrondi { Dock = DockStyle.Fill, BackColor = Theme.Selection, Rayon = 10 };
            panBandeClient = bande;

            Label icone = Theme.Etiquette(Theme.IcPersonne, Theme.Icone(16f), Theme.Bleu, ContentAlignment.MiddleCenter);
            icone.Size = new Size(36, 36);
            lblClientIcone = icone;

            lblClientNom = Theme.Etiquette("Aucun client", Theme.Police(11f, FontStyle.Bold), Theme.Texte);

            chkCafeGratuit = new CheckBox
            {
                Text = "Utiliser les gratuits",
                Checked = true,
                AutoSize = true,
                Font = Theme.Police(9.5f, FontStyle.Bold),
                ForeColor = Theme.Vert,
                BackColor = Theme.Selection,
                Visible = false,
                Cursor = Cursors.Hand
            };
            chkCafeGratuit.CheckedChanged += (s, e) => { utiliserGratuits = chkCafeGratuit.Checked; RafraichirTotaux(); };

            btnClient = new BoutonArrondi { Text = "Client", Icone = Theme.IcRecherche, TailleIcone = 12f, Font = Theme.Police(10f, FontStyle.Bold), Rayon = 8 };
            btnClient.Click += (s, e) => ChoisirClient();

            btnRetirerClient = new BoutonArrondi
            {
                Text = "",
                Icone = "\uE711",   // croix (Segoe MDL2 Assets)
                TailleIcone = 11f,
                CouleurIcone = Theme.TexteDoux,
                BackColor = Color.White,
                ForeColor = Theme.TexteDoux,
                CouleurSurvol = Theme.RougeClair,
                Rayon = 8,
                Visible = false
            };
            btnRetirerClient.Click += (s, e) => RetirerClient();

            bande.Controls.AddRange(new Control[] { icone, lblClientNom, chkCafeGratuit, btnClient, btnRetirerClient });
            bande.Resize += (s, e) => MiseEnPageBandeClient();

            enveloppe.Controls.Add(bande);
            enveloppe.Visible = Fidelite.Actif;
            enveloppeClient = enveloppe;
            return enveloppe;
        }

        private const int HauteurLigneCarte = 18;

        private void MiseEnPageBandeClient()
        {
            if (panBandeClient == null) return;
            int w = panBandeClient.Width;
            int hBoutons = 62;   // les boutons restent centrés sur le haut de la bande

            lblClientIcone.Location = new Point(8, (hBoutons - lblClientIcone.Height) / 2);
            btnClient.SetBounds(w - 128, (hBoutons - 36) / 2, 118, 36);
            btnRetirerClient.SetBounds(w - 170, (hBoutons - 32) / 2, 36, 32);

            int droite = btnRetirerClient.Visible ? w - 176 : w - 134;
            int finNom = droite;
            if (chkCafeGratuit.Visible)
            {
                chkCafeGratuit.Location = new Point(droite - chkCafeGratuit.Width, 7);
                finNom = chkCafeGratuit.Left - 4;
            }
            lblClientNom.SetBounds(46, 4, Math.Max(40, finNom - 46), 24);

            int y = 28;
            foreach (Label l in lignesCartes)
            {
                l.SetBounds(46, y, Math.Max(40, droite - 46), HauteurLigneCarte);
                y += HauteurLigneCarte;
            }
        }

        private void ChoisirClient()
        {
            if (!Fidelite.Actif)
            {
                MessageBox.Show(Parametres.FideliteActive
                        ? "Aucune carte fidélité active. Crée une carte dans Paramètres."
                        : "La fidélité est désactivée. Active-la dans Paramètres.",
                    "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (FormChoixClient f = new FormChoixClient())
            {
                if (f.ShowDialog(this) != DialogResult.OK || f.ClientChoisi == null) return;
                clientActuel = f.ClientChoisi;
                utiliserGratuits = true;
                chkCafeGratuit.Checked = true;
            }
            RafraichirTotaux();
        }

        private void RetirerClient()
        {
            clientActuel = null;
            RafraichirTotaux();
        }

        // Une ligne de texte (et de couleur) par carte
        private void RafraichirBandeClient()
        {
            if (lblClientNom == null) return;

            List<KeyValuePair<string, Color>> lignes = new List<KeyValuePair<string, Color>>();
            bool gratuitUtilisable = false;

            if (clientActuel == null)
            {
                lblClientNom.Text = "Aucun client";
                string noms = string.Join(", ", Fidelite.CartesActives.Select(c => c.Nom));
                lignes.Add(new KeyValuePair<string, Color>("Cartes fidélité (" + noms + ") : entre le téléphone", Theme.TexteDoux));
                btnClient.Text = "Client";
            }
            else
            {
                lblClientNom.Text = clientActuel.NomComplet;
                btnClient.Text = "Changer";

                foreach (ResultatCarte r in resultatsFidelite)
                {
                    CarteFidelite c = r.Carte;
                    if (r.ProduitOffert != null)
                        lignes.Add(new KeyValuePair<string, Color>("★ " + c.Nom + " : gratuit appliqué (" + r.ProduitOffert.Nom + ")", Theme.Vert));
                    else if (r.RecompenseDisponible && r.ProduitDansPanier)
                        lignes.Add(new KeyValuePair<string, Color>("★ " + c.Nom + " : gratuit disponible", Theme.Vert));
                    else if (r.RecompenseDisponible)
                        lignes.Add(new KeyValuePair<string, Color>("★ " + c.Nom + " : gratuit disponible (ajoute un produit « " + c.Categorie + " »)", Theme.Vert));
                    else
                        lignes.Add(new KeyValuePair<string, Color>(
                            c.Nom + "  " + Fidelite.Pastilles(r.Avant, c.TamponsRequis) + "  " + r.Avant + "/" + c.TamponsRequis +
                            (r.Gagnes > 0 ? "   (+" + r.Gagnes + " → " + r.Apres + ")" : ""), Theme.Bleu));

                    if (r.RecompenseDisponible && r.ProduitDansPanier) gratuitUtilisable = true;
                }
            }

            // Recrée les étiquettes si le nombre de lignes a changé
            while (lignesCartes.Count > lignes.Count)
            {
                Label l = lignesCartes[lignesCartes.Count - 1];
                lignesCartes.RemoveAt(lignesCartes.Count - 1);
                panBandeClient.Controls.Remove(l);
                l.Dispose();
            }
            while (lignesCartes.Count < lignes.Count)
            {
                Label l = Theme.Etiquette("", Theme.Police(9.5f), Theme.Bleu);
                lignesCartes.Add(l);
                panBandeClient.Controls.Add(l);
            }
            for (int i = 0; i < lignes.Count; i++)
            {
                lignesCartes[i].Text = lignes[i].Key;
                lignesCartes[i].ForeColor = lignes[i].Value;
            }

            btnRetirerClient.Visible = clientActuel != null;
            chkCafeGratuit.Visible = gratuitUtilisable;

            // Hauteur de la bande selon le nombre de cartes
            int hauteurBande = Math.Max(62, 28 + lignes.Count * HauteurLigneCarte + 8);
            if (enveloppeClient.Height != hauteurBande + 10) enveloppeClient.Height = hauteurBande + 10;

            MiseEnPageBandeClient();
        }

        private Control CreerColonnePaiement()
        {
            PanneauArrondi carte = new PanneauArrondi { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(18, 12, 18, 18), Bordure = Theme.Gris };

            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (int h in new[] { 48, 30, 52, 30, 64, 64, 64 }) t.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            t.Controls.Add(Titre("Paiement"), 0, 0);

            Label lMontant = Theme.Etiquette("Montant reçu", Theme.Police(11f), Theme.Texte);
            lMontant.Dock = DockStyle.Fill;
            t.Controls.Add(lMontant, 0, 1);

            // Champ montant + "$"
            PanneauArrondi champ = new PanneauArrondi { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2), Bordure = Theme.Gris, Rayon = 8 };
            txtMontantRecu = new TextBox { BorderStyle = BorderStyle.None, Font = Theme.Police(14f), ForeColor = Theme.Texte, BackColor = Color.White };
            Theme.TexteIndicatif(txtMontantRecu, "0,00");
            Label dollar = Theme.Etiquette("$", Theme.Police(14f, FontStyle.Bold), Theme.Texte, ContentAlignment.MiddleCenter);
            dollar.Size = new Size(40, 40);
            Panel sep = new Panel { BackColor = Theme.Gris, Width = 1 };
            champ.Controls.AddRange(new Control[] { txtMontantRecu, dollar, sep });
            champ.Resize += (s, e) =>
            {
                dollar.Location = new Point(champ.Width - 44, (champ.Height - dollar.Height) / 2);
                sep.SetBounds(champ.Width - 48, 1, 1, Math.Max(1, champ.Height - 2));
                txtMontantRecu.Location = new Point(14, (champ.Height - txtMontantRecu.Height) / 2);
                txtMontantRecu.Width = Math.Max(40, champ.Width - 70);
            };
            txtMontantRecu.TextChanged += (s, e) => CalculerRendu();
            txtMontantRecu.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.Handled = true;
                e.SuppressKeyPress = true;
                PayerComptant();
            };
            t.Controls.Add(champ, 0, 2);

            lblRendu = Theme.Etiquette("", Theme.Police(10f, FontStyle.Bold), Theme.TexteDoux);
            lblRendu.Dock = DockStyle.Fill;
            t.Controls.Add(lblRendu, 0, 3);

            BoutonArrondi btnStripe = new BoutonArrondi
            {
                Text = "Payer avec Stripe",
                Icone = Theme.IcCarte,
                TailleIcone = 20f,
                BackColor = Theme.Vert,
                Font = Theme.Police(12.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 4)
            };
            btnStripe.Click += (s, e) => PayerAvecStripe();
            t.Controls.Add(btnStripe, 0, 4);

            BoutonArrondi btnTPE = new BoutonArrondi
            {
                Text = "Payer par TPE",
                Icone = Theme.IcTPE,
                TailleIcone = 20f,
                BackColor = Theme.Bleu,
                Font = Theme.Police(12.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 4)
            };
            btnTPE.Click += (s, e) => PayerParTPE();
            t.Controls.Add(btnTPE, 0, 5);

            BoutonArrondi btnComptant = new BoutonArrondi
            {
                Text = "Payer comptant",
                Icone = Theme.IcComptant,
                TailleIcone = 20f,
                BackColor = Theme.Sidebar,
                Font = Theme.Police(12.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 8)
            };
            btnComptant.Click += (s, e) => PayerComptant();
            t.Controls.Add(btnComptant, 0, 6);

            // Bloc des totaux
            PanneauArrondi totaux = new PanneauArrondi { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0), Padding = new Padding(14, 10, 14, 10), BackColor = Theme.GrisClair, Rayon = 10 };
            TableLayoutPanel tt = new TableLayoutPanel { Dock = DockStyle.Top, Height = 118, ColumnCount = 2, RowCount = 5, BackColor = Theme.GrisClair };
            ttTotaux = tt;
            tt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            tt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            foreach (int h in new[] { 34, 0, 34, 34, 16 }) tt.RowStyles.Add(new RowStyle(SizeType.Absolute, h));

            lblSousTotal = Valeur();
            lblTPS = Valeur();
            lblTVQ = Valeur();
            lblRabais = Valeur();
            lblRabais.ForeColor = Theme.Vert;
            lblRabaisLibelle = Libelle("Gratuit fidélité :");
            lblRabaisLibelle.ForeColor = Theme.Vert;
            lblRabais.Visible = lblRabaisLibelle.Visible = false;
            tt.Controls.Add(Libelle("Sous-total :"), 0, 0); tt.Controls.Add(lblSousTotal, 1, 0);
            tt.Controls.Add(lblRabaisLibelle, 0, 1); tt.Controls.Add(lblRabais, 1, 1);
            lblTPSLibelle = Libelle(Parametres.LibelleTPS + " :");
            lblTVQLibelle = Libelle(Parametres.LibelleTVQ + " :");
            tt.Controls.Add(lblTPSLibelle, 0, 2); tt.Controls.Add(lblTPS, 1, 2);
            tt.Controls.Add(lblTVQLibelle, 0, 3); tt.Controls.Add(lblTVQ, 1, 3);

            Panel ligne = new Panel { BackColor = Color.FromArgb(203, 213, 225), Dock = DockStyle.Fill, Margin = new Padding(0, 7, 0, 7) };
            tt.Controls.Add(ligne, 0, 4);
            tt.SetColumnSpan(ligne, 2);

            // Le TOTAL est dessiné directement dans l'espace sous les taxes
            panTotaux = totaux;
            totaux.Controls.Add(tt);
            totaux.Paint += (s, e) =>
            {
                Rectangle zone = new Rectangle(totaux.Padding.Left, tt.Bottom,
                    totaux.Width - totaux.Padding.Horizontal, totaux.Height - tt.Bottom - totaux.Padding.Bottom);
                if (zone.Width <= 0 || zone.Height <= 0) return;

                string texte = "TOTAL : " + Theme.Argent(Total);
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
                float taille = 24f;
                while (taille > 10f)
                {
                    using (Font essai = Theme.Police(taille, FontStyle.Bold))
                        if (TextRenderer.MeasureText(e.Graphics, texte, essai, Size.Empty, flags).Width <= zone.Width - 8) break;
                    taille -= 1f;
                }
                using (Font police = Theme.Police(taille, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, texte, police, zone, Theme.Bleu, flags);
            };

            t.Controls.Add(totaux, 0, 7);

            carte.Controls.Add(t);
            return carte;
        }

        private Label Libelle(string texte)
        {
            Label l = Theme.Etiquette(texte, Theme.Police(11f), Theme.TexteDoux);
            l.Dock = DockStyle.Fill;
            return l;
        }

        private Label Valeur()
        {
            Label l = Theme.Etiquette("0,00 $", Theme.Police(11f), Theme.Texte, ContentAlignment.MiddleRight);
            l.Dock = DockStyle.Fill;
            return l;
        }

        // ---- Actions rapides ----
        private Control CreerActionsRapides()
        {
            PanneauArrondi carte = new PanneauArrondi { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(16, 6, 16, 16), Bordure = Theme.Gris };

            Label titre = Titre("Actions rapides");
            titre.Dock = DockStyle.Top;
            titre.Height = 44;

            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.White };
            for (int i = 0; i < 4; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AjouterAction(t, 0, "Imprimer ticket", Theme.IcImprimer, ImprimerTicket);
            AjouterAction(t, 1, "Historique des ventes", Theme.IcVentes, OuvrirHistorique);
            AjouterAction(t, 2, "Gestion des clients", Theme.IcClients, OuvrirClients);
            AjouterAction(t, 3, "Gestion des produits", Theme.IcProduit, OuvrirProduits);

            carte.Controls.Add(t);
            carte.Controls.Add(titre);
            return carte;
        }

        private void AjouterAction(TableLayoutPanel t, int col, string texte, string icone, Action action)
        {
            BoutonArrondi b = new BoutonArrondi
            {
                Text = texte,
                Icone = icone,
                TailleIcone = 22f,
                AlignerGauche = true,
                BackColor = Theme.GrisClair,
                ForeColor = Theme.Texte,
                CouleurIcone = Theme.Sidebar,
                Font = Theme.Police(12f),
                Dock = DockStyle.Fill,
                Margin = new Padding(col == 0 ? 0 : 7, 0, col == 3 ? 0 : 7, 0)
            };
            b.Click += (s, e) => action();
            t.Controls.Add(b, col, 0);
        }

        // =====================================================================
        // AFFICHAGE DES PRODUITS
        // =====================================================================
        private void AfficherProduits()
        {
            if (flowProduits == null) return;

            string filtre = txtRecherche == null ? "" : txtRecherche.Text.Trim();
            // Des chiffres seulement = code-barres en cours de saisie : on ne filtre pas
            if (filtre.Length > 0 && filtre.All(char.IsDigit)) filtre = "";

            flowProduits.SuspendLayout();
            while (flowProduits.Controls.Count > 0) flowProduits.Controls[0].Dispose();

            foreach (Produit p in produits)
            {
                if (categorieActive != "Tous" && !string.Equals(p.Categorie, categorieActive, StringComparison.OrdinalIgnoreCase)) continue;
                if (filtre.Length > 0 && p.Nom.IndexOf(filtre, StringComparison.OrdinalIgnoreCase) < 0) continue;

                CarteProduit carte = new CarteProduit(p, ImageDe(p));
                carte.Click += (s, e) => ClicProduit((CarteProduit)s);
                flowProduits.Controls.Add(carte);
            }
            if (flowProduits.Controls.Count == 0 && produits.Count == 0)
            {
                Label vide = Theme.Etiquette("Aucun produit pour l'instant.\nAjoute tes produits dans « Gestion des produits » (en bas de l'écran).",
                    Theme.Police(12f), Theme.TexteDoux, ContentAlignment.MiddleCenter);
                vide.Size = new Size(Math.Max(300, flowProduits.ClientSize.Width - 20), 120);
                flowProduits.Controls.Add(vide);
            }
            flowProduits.ResumeLayout();
        }

        private void ClicProduit(CarteProduit carte)
        {
            Produit p = carte.Produit;
            if (p.ATailles) ChoisirTaille(p, carte, new Point(0, carte.Height));
            else AjouterAuPanier(p, p);
        }

        // Remplace l'ancien FormBoissons : le choix de taille s'ouvre sous la carte
        private void ChoisirTaille(Produit carte, Control ancre, Point position)
        {
            ContextMenuStrip menu = new ContextMenuStrip { Font = Theme.Police(12f), ShowImageMargin = false };
            ToolStripLabel entete = new ToolStripLabel("Taille — " + carte.Nom)
            {
                Font = Theme.Police(10f, FontStyle.Bold),
                ForeColor = Theme.TexteDoux
            };
            menu.Items.Add(entete);

            foreach (Produit v in carte.Variantes)
            {
                Produit variante = v;
                ToolStripMenuItem item = new ToolStripMenuItem(variante.Taille + "     " + Theme.Argent(variante.Prix)) { Padding = new Padding(8, 6, 8, 6) };
                item.Click += (s, e) => AjouterAuPanier(variante, carte);
                menu.Items.Add(item);
            }
            menu.Show(ancre, position);
        }

        // =====================================================================
        // PANIER
        // =====================================================================
        private Produit CarteDe(Produit p)
        {
            Produit carte = produits.FirstOrDefault(c => c.ATailles && c.Variantes.Contains(p));
            return carte ?? p;
        }

        private bool StockSuffisant(Produit p, int quantiteVoulue)
        {
            if (p.StockIllimite || quantiteVoulue <= p.Stock) return true;
            MessageBox.Show("Stock insuffisant pour « " + p.Nom + " » : il en reste " + Math.Max(0, p.Stock) + ".",
                "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // produit = la vraie ligne de la table ; carte = pour l'image et l'emoji
        private void AjouterAuPanier(Produit produit, Produit carte)
        {
            LignePanier ligne = panier.FirstOrDefault(l => l.Produit.Id == produit.Id);
            int quantite = ligne == null ? 1 : ligne.Quantite + 1;
            if (!StockSuffisant(produit, quantite)) return;

            if (ligne != null)
            {
                ligne.Quantite = quantite;
            }
            else
            {
                ligne = new LignePanier { Produit = produit, Quantite = 1, Image = ImageDe(carte), Emoji = carte.Emoji ?? produit.Emoji };
                panier.Add(ligne);
            }
            ligneSelectionnee = ligne;
            RafraichirPanier();
        }

        private void RafraichirPanier()
        {
            if (flowPanier == null) return;
            if (ligneSelectionnee != null && !panier.Contains(ligneSelectionnee)) ligneSelectionnee = null;

            flowPanier.SuspendLayout();
            while (flowPanier.Controls.Count > 0) flowPanier.Controls[0].Dispose();

            foreach (LignePanier l in panier)
            {
                LignePanier ligne = l;
                LignePanierControl c = new LignePanierControl(ligne);
                c.Selectionnee = ligne == ligneSelectionnee;

                // BeginInvoke : on reconstruit la liste après la fin du clic
                c.Plus += (s, e) => BeginInvoke(new Action(() =>
                {
                    if (!StockSuffisant(ligne.Produit, ligne.Quantite + 1)) return;
                    ligne.Quantite++;
                    RafraichirPanier();
                }));
                c.Moins += (s, e) => BeginInvoke(new Action(() =>
                {
                    ligne.Quantite--;
                    if (ligne.Quantite <= 0) panier.Remove(ligne);
                    RafraichirPanier();
                }));
                c.Supprimer += (s, e) => BeginInvoke(new Action(() => { panier.Remove(ligne); RafraichirPanier(); }));
                c.Choisie += (s, e) =>
                {
                    ligneSelectionnee = ligne;
                    foreach (LignePanierControl autre in flowPanier.Controls) autre.Selectionnee = autre.Ligne == ligne;
                };
                flowPanier.Controls.Add(c);
            }
            AjusterLignes();
            flowPanier.ResumeLayout();

            lblPanier.Text = "Panier (" + panier.Count + ")";
            RafraichirTotaux();
        }

        private void AjusterLignes()
        {
            int w = Math.Max(200, flowPanier.Width - 20);
            foreach (Control c in flowPanier.Controls) c.Width = w;
        }

        private void SupprimerSelection()
        {
            if (ligneSelectionnee == null)
            {
                MessageBox.Show("Clique d'abord sur un produit du panier.", "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            panier.Remove(ligneSelectionnee);
            ligneSelectionnee = null;
            RafraichirPanier();
        }

        private void ViderPanier(bool confirmer)
        {
            if (panier.Count == 0) return;
            if (confirmer && MessageBox.Show("Vider tout le panier ?", "Otmane POS",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            panier.Clear();
            ligneSelectionnee = null;
            clientActuel = null;
            txtMontantRecu.Clear();
            RafraichirPanier();
        }

        // =====================================================================
        // TOTAUX
        // =====================================================================
        private void RafraichirTotaux()
        {
            SousTotal = panier.Sum(x => x.Total);

            // Fidélité : un produit offert par carte qui a assez de tampons
            resultatsFidelite = CalculerFidelite();
            Rabais = resultatsFidelite.Sum(r => r.Rabais);

            decimal baseTaxable = panier.Where(x => x.Taxable).Sum(x => x.Total)   // colonne Taxable
                                  - resultatsFidelite.Where(r => r.RabaisTaxable).Sum(r => r.Rabais);
            TPS = Math.Round(baseTaxable * TauxTPS, 2, MidpointRounding.AwayFromZero);
            TVQ = Math.Round(baseTaxable * TauxTVQ, 2, MidpointRounding.AwayFromZero);
            Total = SousTotal - Rabais + TPS + TVQ;

            lblSousTotal.Text = Theme.Argent(SousTotal);
            lblTPS.Text = Theme.Argent(TPS);
            lblTVQ.Text = Theme.Argent(TVQ);

            bool aRabais = Rabais > 0;
            lblRabais.Text = "-" + Theme.Argent(Rabais);
            lblRabais.Visible = lblRabaisLibelle.Visible = aRabais;
            ttTotaux.RowStyles[1].Height = aRabais ? 34 : 0;
            ttTotaux.Height = aRabais ? 152 : 118;
            RafraichirBandeClient();
            if (panTotaux != null) panTotaux.Invalidate();
            CalculerRendu();
        }

        private List<ResultatCarte> CalculerFidelite()
        {
            List<ResultatCarte> resultats = new List<ResultatCarte>();
            if (clientActuel == null || !Fidelite.Actif) return resultats;

            // Évite d'offrir deux fois la même unité si deux cartes visent la même catégorie
            Dictionary<LignePanier, int> dejaOffert = new Dictionary<LignePanier, int>();
            Func<LignePanier, int> offerts = l => dejaOffert.ContainsKey(l) ? dejaOffert[l] : 0;

            foreach (CarteFidelite carte in Fidelite.CartesActives)
            {
                List<LignePanier> lignes = panier.Where(l => carte.Accepte(l.Produit)).ToList();
                ResultatCarte r = new ResultatCarte
                {
                    Carte = carte,
                    Avant = clientActuel.TamponsSur(carte.Id),
                    ProduitDansPanier = lignes.Count > 0
                };
                r.RecompenseDisponible = r.Avant >= carte.TamponsRequis;

                if (r.RecompenseDisponible && utiliserGratuits)
                {
                    IEnumerable<LignePanier> candidats = lignes.Where(l => offerts(l) < l.Quantite);
                    LignePanier offerte = (Parametres.OffrirLePlusCher
                        ? candidats.OrderByDescending(l => l.Prix)
                        : candidats.OrderBy(l => l.Prix)).FirstOrDefault();
                    if (offerte != null)
                    {
                        dejaOffert[offerte] = offerts(offerte) + 1;
                        r.ProduitOffert = offerte.Produit;
                        r.Rabais = offerte.Prix;
                        r.RabaisTaxable = offerte.Taxable;
                        r.Utilises = carte.TamponsRequis;
                    }
                }

                // 1 tampon par unité achetée (le produit offert ne compte pas)
                r.Gagnes = lignes.Sum(l => l.Quantite) - (r.ProduitOffert != null ? 1 : 0);
                resultats.Add(r);
            }
            return resultats;
        }

        // Arrondi du comptant aux 5 cents près (règle canadienne depuis la fin du 1 cent)
        private static decimal ArrondiComptant(decimal montant)
        {
            if (!Parametres.ArrondiComptant) return montant;
            return Math.Round(montant * 20m, MidpointRounding.AwayFromZero) / 20m;
        }

        private bool LireMontantRecu(out decimal recu)
        {
            string texte = txtMontantRecu.Text.Trim().Replace('.', ',').Replace("$", "").Trim();
            return decimal.TryParse(texte, NumberStyles.Number, Theme.Culture, out recu) && recu > 0;
        }

        private void CalculerRendu()
        {
            if (lblRendu == null || txtMontantRecu == null) return;
            decimal recu;
            if (panier.Count > 0 && LireMontantRecu(out recu))
            {
                decimal aPayer = ArrondiComptant(Total);
                decimal rendu = recu - aPayer;
                lblRendu.Text = rendu >= 0 ? "Monnaie à rendre : " + Theme.Argent(rendu) : "Il manque : " + Theme.Argent(-rendu);
                lblRendu.ForeColor = rendu >= 0 ? Theme.Vert : Theme.Rouge;
            }
            else
            {
                lblRendu.Text = "";
            }
        }

        // =====================================================================
        // CODE-BARRES
        // =====================================================================
        private void RechercheKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            string code = txtRecherche.Text.Trim();
            if (code.Length == 0) return;
            if (TraiterCodeBarre(code)) return;

            // Pas un code connu : si un seul produit correspond à la recherche, on l'ajoute
            if (flowProduits.Controls.Count == 1)
                ClicProduit((CarteProduit)flowProduits.Controls[0]);
            else
                MessageBox.Show("Aucun produit trouvé pour « " + code + " ».", "Otmane POS",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool TraiterCodeBarre(string code)
        {
            // On cherche dans toutes les lignes : "LATTE-M" ajoute directement le Latte moyen
            Produit p = tousLesProduits.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.CodeBarre) && string.Equals(x.CodeBarre, code, StringComparison.OrdinalIgnoreCase));
            if (p == null) return false;

            AjouterAuPanier(p, CarteDe(p));
            txtRecherche.Clear();
            return true;
        }

        /// <summary>
        /// Appelle cette méthode depuis ton code caméra quand un code-barres est décodé.
        /// Elle peut être appelée depuis un autre thread.
        /// </summary>
        public void CodeBarreDetecte(string code)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(CodeBarreDetecte), code); return; }
            if (!TraiterCodeBarre(code.Trim()))
                MessageBox.Show("Code-barres inconnu : " + code, "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // =====================================================================
        // CAMÉRA (AForge) : repris de ton ancien scanner intégré
        // =====================================================================
        private void BasculerCamera()
        {
            if (cameraActive) ArreterCamera();
            else DemarrerCamera();
        }

        private void DemarrerCamera()
        {
            FilterInfoCollection appareils = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (appareils.Count == 0)
            {
                MessageBox.Show("Aucune webcam détectée.", "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Évite les caméras virtuelles (OBS...)
            FilterInfo choisi = null;
            foreach (FilterInfo a in appareils)
            {
                if (a.Name.IndexOf("OBS", StringComparison.OrdinalIgnoreCase) < 0 &&
                    a.Name.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    choisi = a;
                    break;
                }
            }
            if (choisi == null) choisi = appareils[0];

            videoSource = new VideoCaptureDevice(choisi.MonikerString);
            videoSource.NewFrame += NouvelleImage;
            videoSource.Start();

            cameraActive = true;
            panCamera.Visible = true;

            minuteurCamera = new System.Windows.Forms.Timer { Interval = 150 };
            minuteurCamera.Tick += AnalyserImage;
            minuteurCamera.Start();
        }

        private void ArreterCamera()
        {
            if (minuteurCamera != null) { minuteurCamera.Stop(); minuteurCamera.Dispose(); minuteurCamera = null; }
            cameraActive = false;

            if (videoSource != null)
            {
                videoSource.NewFrame -= NouvelleImage;
                if (videoSource.IsRunning) videoSource.SignalToStop();
                videoSource = null;
            }

            if (panCamera != null) panCamera.Visible = false;
            if (pbCamera != null && pbCamera.Image != null)
            {
                Image ancienne = pbCamera.Image;
                pbCamera.Image = null;
                ancienne.Dispose();
            }
        }

        // Appelé par AForge sur un autre thread : on garde juste la dernière image
        private void NouvelleImage(object sender, NewFrameEventArgs e)
        {
            try
            {
                Bitmap copie = (Bitmap)e.Frame.Clone();
                lock (verrouImage)
                {
                    if (derniereImage != null) derniereImage.Dispose();
                    derniereImage = copie;
                }
            }
            catch { }
        }

        // Toutes les 150 ms, sur le thread de l'interface : affichage + lecture du code
        private void AnalyserImage(object sender, EventArgs e)
        {
            if (!cameraActive) return;

            Bitmap image;
            lock (verrouImage)
            {
                if (derniereImage == null) return;
                image = derniereImage;
                derniereImage = null;
            }

            try
            {
                Image ancienne = pbCamera.Image;
                pbCamera.Image = image;
                if (ancienne != null) ancienne.Dispose();

                string code = lecteurCodes.Decoder(image);
                if (!string.IsNullOrEmpty(code))
                {
                    ArreterCamera();
                    CodeBarreDetecte(code);
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ArreterCamera();
            ServiceStripe.ArreterServeur();
            base.OnFormClosing(e);
        }

        // =====================================================================
        // PAIEMENTS
        // =====================================================================
        private bool PanierVide()
        {
            if (panier.Count > 0) return false;
            MessageBox.Show("Le panier est vide.", "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        private void AfficherStatut(string texte, Color couleur)
        {
            lblRendu.Text = texte;
            lblRendu.ForeColor = couleur;
        }

        // Photo du panier au moment du paiement
        private Vente CreerVente(string modePaiement)
        {
            return new Vente
            {
                Lignes = panier.Select(l => new LignePanier { Produit = l.Produit, Quantite = l.Quantite, Image = l.Image, Emoji = l.Emoji }).ToList(),
                SousTotal = SousTotal,
                TPS = TPS,
                TVQ = TVQ,
                Total = Total,
                ModePaiement = modePaiement,
                Date = DateTime.Now,
                MontantPaye = Total,
                Client = clientActuel,
                Rabais = Rabais,
                ResultatsFidelite = resultatsFidelite.ToList()
            };
        }

        // ---- Commande à 0 $ (ex. café gratuit seul) : rien à encaisser ----
        private bool TraiterCommandeGratuite()
        {
            if (Total > 0) return false;

            if (MessageBox.Show("Le total est de 0,00 $ (produit offert avec la carte fidélité).\n\nValider la commande sans paiement ?",
                    "Commande gratuite", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return true;

            Vente vente = CreerVente("Gratuit");
            vente.MontantPaye = 0;
            vente.NumeroTransaction = "GRATUIT-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            FinaliserVente(vente);
            return true;
        }

        // Stripe refuse les paiements de moins de 0,50 $
        private bool MontantTropPetitPourStripe()
        {
            if (Total >= 0.50m) return false;
            MessageBox.Show("Stripe n'accepte pas les paiements de moins de 0,50 $.\nEncaisse ce montant au comptant.",
                "Paiement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        // ---- Stripe Checkout (navigateur) ----
        // Compte Stripe connecté (et lecteur choisi pour le TPE) ?
        private bool StripePret(bool lecteurRequis)
        {
            if (!ServiceStripe.EstConfigure)
            {
                MessageBox.Show("Aucun compte Stripe n'est connecté.\n\nVa dans Paramètres → Stripe pour coller ta clé secrète.\n" +
                    "En attendant, tu peux encaisser au comptant.", "Stripe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (lecteurRequis && string.IsNullOrWhiteSpace(Parametres.ReaderId))
            {
                MessageBox.Show("Aucun lecteur n'est choisi.\n\nVa dans Paramètres → Stripe, clique sur « Tester la connexion » et choisis ton lecteur.",
                    "TPE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private void PayerAvecStripe()
        {
            if (PanierVide() || paiementEnCours) return;
            if (TraiterCommandeGratuite() || MontantTropPetitPourStripe()) return;
            if (!StripePret(false)) return;
            try
            {
                venteEnAttente = CreerVente("Stripe");
                ServiceStripe.OuvrirCheckout(venteEnAttente.Total);
                AfficherStatut("⏳ Paiement Stripe ouvert dans le navigateur...", Theme.Bleu);
            }
            catch (Exception ex)
            {
                venteEnAttente = null;
                AfficherStatut("❌ Erreur Stripe", Theme.Rouge);
                MessageBox.Show("Erreur Stripe : " + ex.Message, "Stripe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Appelé par le serveur local quand Stripe redirige vers /success
        private async void StripeReussi(string numeroTransaction, decimal montantPaye)
        {
            if (venteEnAttente == null) return;
            Vente vente = venteEnAttente;
            venteEnAttente = null;
            vente.NumeroTransaction = numeroTransaction;

            Activate();   // ramène la caisse au premier plan
            AfficherStatut("⏳ Lecture du type de carte...", Theme.Bleu);
            vente.Carte = await ServiceStripe.LireCarteAsync(numeroTransaction);
            FinaliserVente(vente);
        }

        private void StripeAnnule()
        {
            venteEnAttente = null;
            AfficherStatut("Paiement annulé.", Theme.TexteDoux);
        }

        // ---- TPE (Stripe Terminal) ----
        private async void PayerParTPE()
        {
            if (PanierVide() || paiementEnCours) return;
            if (TraiterCommandeGratuite() || MontantTropPetitPourStripe()) return;
            if (!StripePret(true)) return;

            Vente vente = CreerVente("TPE");
            paiementEnCours = true;
            CancellationTokenSource annulation = new CancellationTokenSource();
            FormAttenteTPE attente = new FormAttenteTPE(vente.Total, annulation);
            try
            {
                // Seulement avec un lecteur simulé (mode test) : on demande un numéro de carte de test
                Func<string> demanderCarteTest = () =>
                {
                    using (var formCarte = new StripePayementTest.FormCarteTest())
                    {
                        IWin32Window parent = attente.Visible ? (IWin32Window)attente : this;
                        if (formCarte.ShowDialog(parent) != DialogResult.OK || !formCarte.Confirme) return null;
                        return formCarte.NumeroCarte;
                    }
                };

                Task<string> paiement = ServiceStripe.PayerParTPEAsync(vente.Total, demanderCarteTest,
                    s => { attente.Message(s); AfficherStatut("⏳ " + s, Theme.Bleu); }, annulation.Token);

                // La fenêtre d'attente se ferme toute seule quand le lecteur répond
                paiement.ContinueWith(t => attente.Terminer(), TaskScheduler.FromCurrentSynchronizationContext());
                if (!paiement.IsCompleted) attente.ShowDialog(this);

                vente.NumeroTransaction = await paiement;
                vente.Carte = await ServiceStripe.LireCarteAsync(vente.NumeroTransaction);
                FinaliserVente(vente);
            }
            catch (OperationCanceledException)
            {
                AfficherStatut("Paiement annulé.", Theme.TexteDoux);
            }
            catch (Exception ex)
            {
                AfficherStatut("❌ Échec du paiement", Theme.Rouge);
                MessageBox.Show("❌ " + ex.Message, "TPE", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                paiementEnCours = false;
                attente.Dispose();
                annulation.Dispose();
            }
        }

        // ---- Comptant ----
        private void PayerComptant()
        {
            if (PanierVide() || paiementEnCours) return;
            if (TraiterCommandeGratuite()) return;

            decimal aPayer = ArrondiComptant(Total);
            decimal recu;
            if (!LireMontantRecu(out recu))
            {
                MessageBox.Show("Entre le montant reçu du client dans « Montant reçu ».\n\nÀ payer au comptant : " + Theme.Argent(aPayer),
                    "Paiement comptant", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtMontantRecu.Focus();
                return;
            }
            if (recu < aPayer)
            {
                MessageBox.Show("Montant insuffisant : il manque " + Theme.Argent(aPayer - recu) + ".",
                    "Paiement comptant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMontantRecu.Focus();
                return;
            }

            decimal rendu = recu - aPayer;
            string confirmation = "À payer : " + Theme.Argent(aPayer) +
                (aPayer != Total ? "  (arrondi de " + Theme.Argent(Total) + ")" : "") +
                "\nReçu : " + Theme.Argent(recu) +
                "\n\nMonnaie à rendre : " + Theme.Argent(rendu) +
                "\n\nConfirmer le paiement comptant ?";
            if (MessageBox.Show(confirmation, "Paiement comptant", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Vente vente = CreerVente("Comptant");
            vente.MontantPaye = aPayer;
            vente.Recu = recu;
            vente.Rendu = rendu;
            vente.NumeroTransaction = "COMPTANT-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            FinaliserVente(vente);
        }

        // ---- Après un paiement réussi (Stripe, TPE ou comptant) ----
        private void FinaliserVente(Vente vente)
        {
            try
            {
                // Paiements + DetailsVente + stock, avec le même statut que ton ancien code
                vente.Id = Donnees.EnregistrerVente(vente, "Réussi");
                if (vente.Client != null && vente.ResultatsFidelite != null)
                    foreach (ResultatCarte r in vente.ResultatsFidelite)
                        vente.Client.TamponsParCarte[r.Carte.Id] = r.Apres;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Le paiement est passé, mais la vente n'a pas pu être enregistrée :\n\n" + ex.Message,
                    "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            derniereVente = vente;

            panier.Clear();
            ligneSelectionnee = null;
            clientActuel = null;
            txtMontantRecu.Clear();
            RafraichirPanier();

            // Stock à jour
            ChargerProduits();
            AfficherProduits();

            AfficherStatut(vente.EstComptant ? "✅ Monnaie rendue : " + Theme.Argent(vente.Rendu)
                                           : "✅ Paiement réussi : " + Theme.Argent(vente.MontantPaye), Theme.Vert);

            string details = vente.EstComptant
                ? "Monnaie à rendre : " + Theme.Argent(vente.Rendu)
                : (vente.Carte != null ? "Carte : " + vente.Carte.LibelleComplet + "\n" : "") +
                  "Transaction : " + vente.NumeroTransaction;
            if (vente.Client != null)
            {
                details += "\n\nCartes de " + vente.Client.Prenom + " :";
                foreach (ResultatCarte r in vente.ResultatsFidelite ?? new List<ResultatCarte>())
                {
                    details += "\n  " + r.Carte.Nom + " : " + r.Apres + "/" + r.Carte.TamponsRequis;
                    if (r.Utilises > 0) details += "  (gratuit utilisé)";
                    if (r.Apres >= r.Carte.TamponsRequis) details += "  ★ prochain gratuit !";
                }
            }
            if (MessageBox.Show("✅ Paiement de " + Theme.Argent(vente.MontantPaye) + " réussi (" + vente.ModePaiement + ").\n" +
                    details + "\n\nImprimer le ticket ?", "Otmane POS",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                ImprimerTicket();
        }

        // =====================================================================
        // TICKET (dernière vente)
        // =====================================================================
        private void ImprimerTicket()
        {
            if (derniereVente == null)
            {
                MessageBox.Show("Aucune vente à imprimer pour l'instant.\n\nPour un ancien ticket, ouvre « Historique des ventes ».",
                    "Otmane POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Ticket.Apercu(this, DonneesTicket.DepuisVente(derniereVente));
        }
    }
}
