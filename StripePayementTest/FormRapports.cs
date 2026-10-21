using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace OtmanePOS
{
    public class FormRapports : Form
    {
        private DateTime debut, fin;
        private string periodeActive = "Aujourd'hui";
        private readonly List<BoutonArrondi> boutonsPeriode = new List<BoutonArrondi>();

        private Label lblPeriode, lblTitreGraphique;
        private Label kCA, kCASous, kNb, kNbSous, kPanier, kPanierSous, kGratuit, kGratuitSous;
        private GraphiqueBarres graphique;
        private ListeRepartition repartition;
        private ListView lvProduits, lvClients;

        public FormRapports()
        {
            Text = "Otmane POS - Rapports";
            BackColor = Theme.Fond;
            Font = Theme.Police(10f);
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1300, 880);
            MinimumSize = new Size(1100, 760);
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F5) Charger(); if (e.KeyCode == Keys.Escape) Close(); };

            TableLayoutPanel racine = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Fond,
                Padding = new Padding(24, 18, 24, 20),
                ColumnCount = 1,
                RowCount = 4
            };
            racine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 136));
            racine.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            racine.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

            racine.Controls.Add(CreerEntete(), 0, 0);
            racine.Controls.Add(CreerChiffresCles(), 0, 1);
            racine.Controls.Add(CreerLigneGraphique(), 0, 2);
            racine.Controls.Add(CreerLigneClassements(), 0, 3);
            Controls.Add(racine);

            ChoisirPeriode("Aujourd'hui");
        }

        // =====================================================================
        // EN-TÊTE : titre + période
        // =====================================================================
        private Control CreerEntete()
        {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label titre = Theme.Etiquette("Rapports", Theme.Police(20f, FontStyle.Bold), Theme.Texte);
            titre.AutoSize = true;
            titre.Anchor = AnchorStyles.Left;
            titre.Margin = new Padding(0, 0, 18, 0);

            lblPeriode = Theme.Etiquette("", Theme.Police(12f), Theme.TexteDoux);
            lblPeriode.Dock = DockStyle.Fill;

            FlowLayoutPanel boutons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            foreach (string p in new[] { "Aujourd'hui", "Hier", "7 derniers jours", "Ce mois" })
            {
                string periode = p;
                Font f = Theme.Police(11f);
                BoutonArrondi b = new BoutonArrondi
                {
                    Text = periode,
                    Tag = periode,
                    Font = f,
                    Size = new Size(TextRenderer.MeasureText(periode, f).Width + 36, 44),
                    Margin = new Padding(10, 0, 0, 0)
                };
                b.Click += (s, e) => ChoisirPeriode(periode);
                boutonsPeriode.Add(b);
                boutons.Controls.Add(b);
            }

            BoutonArrondi actualiser = new BoutonArrondi
            {
                Text = "",
                Icone = "\uE72C",          // Actualiser (Segoe MDL2 Assets)
                TailleIcone = 14f,
                BackColor = Color.White,
                CouleurIcone = Theme.Texte,
                Size = new Size(44, 44),
                Margin = new Padding(10, 0, 0, 0)
            };
            actualiser.Click += (s, e) => Charger();
            boutons.Controls.Add(actualiser);

            t.Controls.Add(titre, 0, 0);
            t.Controls.Add(lblPeriode, 1, 0);
            t.Controls.Add(boutons, 2, 0);
            return t;
        }

        private void ChoisirPeriode(string periode)
        {
            periodeActive = periode;
            DateTime aujourdhui = DateTime.Today;
            switch (periode)
            {
                case "Hier": debut = aujourdhui.AddDays(-1); fin = aujourdhui; break;
                case "7 derniers jours": debut = aujourdhui.AddDays(-6); fin = aujourdhui.AddDays(1); break;
                case "Ce mois": debut = new DateTime(aujourdhui.Year, aujourdhui.Month, 1); fin = aujourdhui.AddDays(1); break;
                default: debut = aujourdhui; fin = aujourdhui.AddDays(1); break;
            }

            foreach (BoutonArrondi b in boutonsPeriode)
            {
                bool actif = (string)b.Tag == periodeActive;
                b.BackColor = actif ? Theme.Bleu : Theme.Gris;
                b.ForeColor = actif ? Color.White : Theme.Texte;
                b.Invalidate();
            }

            DateTime dernierJour = fin.AddDays(-1);
            lblPeriode.Text = debut == dernierJour
                ? debut.ToString("dddd d MMMM yyyy", Theme.Culture)
                : "du " + debut.ToString("d MMMM", Theme.Culture) + " au " + dernierJour.ToString("d MMMM yyyy", Theme.Culture);

            Charger();
        }

        // =====================================================================
        // CHIFFRES CLÉS
        // =====================================================================
        private Control CreerChiffresCles()
        {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 0, 0, 16) };
            for (int i = 0; i < 4; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            t.Controls.Add(CarteChiffre("Chiffre d'affaires", "\uE8C7", 0, out kCA, out kCASous), 0, 0);
            t.Controls.Add(CarteChiffre("Ventes", Theme.IcVentes, 1, out kNb, out kNbSous), 1, 0);
            t.Controls.Add(CarteChiffre("Panier moyen", Theme.IcPanier, 2, out kPanier, out kPanierSous), 2, 0);
            t.Controls.Add(CarteChiffre("Gratuits fidélité", Theme.IcClients, 3, out kGratuit, out kGratuitSous), 3, 0);
            return t;
        }

        private Control CarteChiffre(string titre, string icone, int colonne, out Label valeur, out Label sous)
        {
            PanneauArrondi carte = new PanneauArrondi
            {
                Dock = DockStyle.Fill,
                Bordure = Theme.Gris,
                Padding = new Padding(18, 14, 18, 12),
                Margin = new Padding(colonne == 0 ? 0 : 7, 0, colonne == 3 ? 0 : 7, 0)
            };

            Label lTitre = Theme.Etiquette(titre, Theme.Police(10.5f), Theme.TexteDoux);
            lTitre.Dock = DockStyle.Top;
            lTitre.Height = 24;

            valeur = Theme.Etiquette("—", Theme.Police(22f, FontStyle.Bold), Theme.Texte);
            valeur.Dock = DockStyle.Top;
            valeur.Height = 48;

            sous = Theme.Etiquette("", Theme.Police(9.5f), Theme.TexteDoux);
            sous.Dock = DockStyle.Top;
            sous.Height = 22;

            Label lIcone = Theme.Etiquette(icone, Theme.Icone(18f), Theme.Bleu, ContentAlignment.MiddleCenter);
            lIcone.Size = new Size(40, 40);

            carte.Controls.Add(sous);
            carte.Controls.Add(valeur);
            carte.Controls.Add(lTitre);
            carte.Controls.Add(lIcone);
            lIcone.BringToFront();
            carte.Resize += (s, e) => lIcone.Location = new Point(carte.Width - 56, 12);
            return carte;
        }

        // =====================================================================
        // GRAPHIQUE + MODES DE PAIEMENT
        // =====================================================================
        private Control CreerLigneGraphique()
        {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 16) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            PanneauArrondi carteGraph = CarteSection("Ventes par heure", new Padding(0, 0, 8, 0), out lblTitreGraphique);
            graphique = new GraphiqueBarres { Dock = DockStyle.Fill };
            carteGraph.Controls.Add(graphique);
            graphique.BringToFront();

            Label ignore;
            PanneauArrondi carteModes = CarteSection("Modes de paiement", new Padding(8, 0, 0, 0), out ignore);
            repartition = new ListeRepartition { Dock = DockStyle.Fill };
            carteModes.Controls.Add(repartition);
            repartition.BringToFront();

            t.Controls.Add(carteGraph, 0, 0);
            t.Controls.Add(carteModes, 1, 0);
            return t;
        }

        // =====================================================================
        // CLASSEMENTS
        // =====================================================================
        private Control CreerLigneClassements()
        {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label ignore;
            PanneauArrondi carteProduits = CarteSection("Meilleurs produits", new Padding(0, 0, 8, 0), out ignore);
            lvProduits = CreerListe(new[] { "Produit", "Quantité", "Ventes" }, new[] { 110, 120 });
            carteProduits.Controls.Add(lvProduits);
            lvProduits.BringToFront();

            PanneauArrondi carteClients = CarteSection("Meilleurs clients", new Padding(8, 0, 0, 0), out ignore);
            lvClients = CreerListe(new[] { "Client", "Visites", "Dépensé" }, new[] { 90, 120 });
            carteClients.Controls.Add(lvClients);
            lvClients.BringToFront();

            t.Controls.Add(carteProduits, 0, 0);
            t.Controls.Add(carteClients, 1, 0);
            return t;
        }

        private PanneauArrondi CarteSection(string titre, Padding marge, out Label lblTitre)
        {
            PanneauArrondi carte = new PanneauArrondi { Dock = DockStyle.Fill, Bordure = Theme.Gris, Padding = new Padding(18, 10, 18, 14), Margin = marge };
            lblTitre = Theme.Etiquette(titre, Theme.Police(14f, FontStyle.Bold), Theme.Texte);
            lblTitre.Dock = DockStyle.Top;
            lblTitre.Height = 40;
            carte.Controls.Add(lblTitre);
            return carte;
        }

        // Liste à colonnes : la première prend la place restante, les autres ont une largeur fixe (alignées à droite)
        private ListView CreerListe(string[] colonnes, int[] largeursFixes)
        {
            ListView lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                Font = Theme.Police(10.5f),
                ForeColor = Theme.Texte,
                BackColor = Color.White,
                MultiSelect = false,
                SmallImageList = new ImageList { ImageSize = new Size(1, 30) }   // lignes plus hautes
            };
            for (int i = 0; i < colonnes.Length; i++)
                lv.Columns.Add(colonnes[i], i == 0 ? 200 : largeursFixes[i - 1], i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right);

            lv.Resize += (s, e) =>
            {
                int fixe = largeursFixes.Sum();
                lv.Columns[0].Width = Math.Max(120, lv.ClientSize.Width - fixe - 4);
            };
            return lv;
        }

        private static void RemplirListe(ListView lv, IEnumerable<string[]> lignes, string messageVide)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            foreach (string[] l in lignes) lv.Items.Add(new ListViewItem(l));
            if (lv.Items.Count == 0)
                lv.Items.Add(new ListViewItem(messageVide) { ForeColor = Theme.TexteDoux });
            lv.EndUpdate();
        }

        // =====================================================================
        // CHARGEMENT
        // =====================================================================
        private void Charger()
        {
            if (kCA == null) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                ChargerResume();
                ChargerGraphique();
                ChargerProduits();
                ChargerClients();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ChargerResume()
        {
            try
            {
                ResumeVentes r = Rapports.Resume(debut, fin);

                kCA.Text = Theme.Argent(r.ChiffreAffaires);
                kCASous.Text = "taxes incluses";
                kNb.Text = r.NbVentes.ToString();
                kNbSous.Text = r.NbGratuit > 0 ? "dont " + r.NbGratuit + " commande(s) gratuite(s)" : "paiements réussis";
                kPanier.Text = Theme.Argent(r.PanierMoyen);
                kPanierSous.Text = "par vente payée";
                kGratuit.Text = r.CafesGratuits.ToString();
                kGratuitSous.Text = "produits offerts avec les cartes";

                ChargerRepartition();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de charger le résumé des ventes :\n\n" + ex.Message, "Rapports",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Color CouleurPaiement(string libelle)
        {
            if (libelle == "Comptant") return Theme.Vert;
            if (libelle.StartsWith("Débit")) return Color.FromArgb(234, 179, 8);        // jaune
            if (libelle.StartsWith("Visa")) return Theme.Bleu;
            if (libelle.StartsWith("Mastercard")) return Color.FromArgb(234, 88, 12);   // orange
            if (libelle.StartsWith("American")) return Color.FromArgb(14, 165, 233);
            return Theme.TexteDoux;
        }

        private void ChargerRepartition()
        {
            try
            {
                repartition.Definir(Rapports.RepartitionPaiements(debut, fin)
                    .Select(l => new ElementRepartition(l.Libelle, l.Montant, l.Quantite, CouleurPaiement(l.Libelle)))
                    .ToList());
            }
            catch (Exception ex)
            {
                repartition.Definir(new List<ElementRepartition>
                {
                    new ElementRepartition("Erreur de lecture", 0, 0, Theme.Rouge)
                });
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void ChargerGraphique()
        {
            try
            {
                List<string> etiquettes = new List<string>();
                List<decimal> valeurs = new List<decimal>();

                if ((fin - debut).TotalDays <= 1)
                {
                    // Une journée : par heure, de 6 h à 22 h (élargi si des ventes ont eu lieu en dehors)
                    lblTitreGraphique.Text = "Ventes par heure";
                    Dictionary<int, decimal> parHeure = Rapports.VentesParHeure(debut, fin);
                    int premiere = Math.Min(6, parHeure.Count > 0 ? parHeure.Keys.Min() : 6);
                    int derniere = Math.Max(22, parHeure.Count > 0 ? parHeure.Keys.Max() : 22);
                    for (int h = premiere; h <= derniere; h++)
                    {
                        decimal v;
                        etiquettes.Add(h + " h");
                        valeurs.Add(parHeure.TryGetValue(h, out v) ? v : 0);
                    }
                }
                else
                {
                    lblTitreGraphique.Text = "Ventes par jour";
                    Dictionary<DateTime, decimal> parJour = Rapports.VentesParJour(debut, fin);
                    bool court = (fin - debut).TotalDays <= 8;
                    for (DateTime j = debut; j < fin; j = j.AddDays(1))
                    {
                        decimal v;
                        etiquettes.Add(court ? j.ToString("ddd d", Theme.Culture) : j.Day.ToString());
                        valeurs.Add(parJour.TryGetValue(j.Date, out v) ? v : 0);
                    }
                }

                graphique.Definir(etiquettes, valeurs);
            }
            catch (Exception ex)
            {
                graphique.Definir(new List<string>(), new List<decimal>());
                lblTitreGraphique.Text = "Ventes (erreur : " + ex.Message + ")";
            }
        }

        private void ChargerProduits()
        {
            try
            {
                List<LigneRapport> liste = Rapports.MeilleursProduits(debut, fin);
                RemplirListe(lvProduits,
                    liste.Select((l, i) => new[] { (i + 1) + ".  " + l.Libelle, l.Quantite.ToString(), Theme.Argent(l.Montant) }),
                    "Aucune vente sur cette période");
            }
            catch (Exception ex)
            {
                RemplirListe(lvProduits, new List<string[]>(), "Erreur : " + ex.Message);
            }
        }

        private void ChargerClients()
        {
            try
            {
                List<LigneRapport> liste = Rapports.MeilleursClients(debut, fin);
                RemplirListe(lvClients,
                    liste.Select((l, i) => new[]
                    {
                        (i + 1) + ".  " + l.Libelle,
                        l.Quantite.ToString(),
                        Theme.Argent(l.Montant)
                    }),
                    "Aucun client fidélité sur cette période");
            }
            catch (Exception ex)
            {
                RemplirListe(lvClients, new List<string[]>(), "Erreur : " + ex.Message);
            }
        }
    }

    // =========================================================================
    // Graphique à barres simple
    // =========================================================================
    public class GraphiqueBarres : Control
    {
        private List<string> etiquettes = new List<string>();
        private List<decimal> valeurs = new List<decimal>();
        private readonly Font policeEtiquette = Theme.Police(8.5f);
        private readonly Font policeValeur = Theme.Police(8.5f, FontStyle.Bold);
        private readonly Font policeVide = Theme.Police(11f);

        public GraphiqueBarres()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        public void Definir(List<string> nouvellesEtiquettes, List<decimal> nouvellesValeurs)
        {
            etiquettes = nouvellesEtiquettes;
            valeurs = nouvellesValeurs;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int n = valeurs.Count;
            decimal max = n > 0 ? valeurs.Max() : 0;
            if (n == 0 || max <= 0)
            {
                TextRenderer.DrawText(g, "Aucune vente sur cette période", policeVide, ClientRectangle, Theme.TexteDoux,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            int bas = Height - 26;          // place pour les étiquettes
            int haut = 22;                  // place pour les montants
            int zone = Math.Max(10, bas - haut);
            float largeurCase = (float)Width / n;
            int largeurBarre = (int)Math.Max(4, Math.Min(44, largeurCase * 0.62f));
            int pas = largeurCase < 26 ? 2 : 1;   // une étiquette sur deux si c'est serré

            using (Pen ligne = new Pen(Theme.Gris)) g.DrawLine(ligne, 0, bas, Width, bas);

            for (int i = 0; i < n; i++)
            {
                int x = (int)(i * largeurCase + (largeurCase - largeurBarre) / 2);
                int h = (int)((float)(valeurs[i] / max) * zone);

                if (h > 0)
                {
                    Rectangle r = new Rectangle(x, bas - h, largeurBarre, h);
                    using (GraphicsPath p = Theme.Arrondi(r, Math.Min(5, largeurBarre / 2)))
                    using (SolidBrush b = new SolidBrush(valeurs[i] == max ? Theme.Bleu : Color.FromArgb(147, 197, 253)))
                        g.FillPath(b, p);

                    if (largeurCase >= 40)
                    {
                        string txt = valeurs[i].ToString("N0", Theme.Culture) + " $";
                        TextRenderer.DrawText(g, txt, policeValeur,
                            new Rectangle((int)(i * largeurCase), bas - h - 20, (int)largeurCase, 18),
                            Theme.Texte, TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom);
                    }
                }

                if (i % pas == 0)
                    TextRenderer.DrawText(g, etiquettes[i], policeEtiquette,
                        new Rectangle((int)(i * largeurCase) - 10, bas + 4, (int)largeurCase + 20, 20),
                        Theme.TexteDoux, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
            }
        }
    }

    // =========================================================================
    // Répartition (modes de paiement) : nom, montant, nombre et barre de proportion
    // =========================================================================
    public class ElementRepartition
    {
        public string Nom { get; private set; }
        public decimal Montant { get; private set; }
        public int Nombre { get; private set; }
        public Color Couleur { get; private set; }

        public ElementRepartition(string nom, decimal montant, int nombre, Color couleur)
        {
            Nom = nom; Montant = montant; Nombre = nombre; Couleur = couleur;
        }
    }

    public class ListeRepartition : Control
    {
        private List<ElementRepartition> elements = new List<ElementRepartition>();
        private readonly Font policeNom = Theme.Police(11f, FontStyle.Bold);
        private readonly Font policeDetail = Theme.Police(10f);

        public ListeRepartition()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        public void Definir(List<ElementRepartition> nouveaux)
        {
            elements = nouveaux;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            decimal total = elements.Sum(x => x.Montant);
            int y = 6;
            foreach (ElementRepartition el in elements)
            {
                decimal part = total > 0 ? el.Montant / total : 0;

                TextRenderer.DrawText(g, el.Nom, policeNom, new Rectangle(0, y, Width, 24), Theme.Texte,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                string detail = Theme.Argent(el.Montant) + "   (" + el.Nombre + " vente" + (el.Nombre > 1 ? "s" : "") + ", " +
                                Math.Round(part * 100) + " %)";
                TextRenderer.DrawText(g, detail, policeDetail, new Rectangle(0, y, Width, 24), Theme.TexteDoux,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                y += 30;

                Rectangle fond = new Rectangle(0, y, Width - 1, 12);
                using (GraphicsPath p = Theme.Arrondi(fond, 6))
                using (SolidBrush b = new SolidBrush(Theme.GrisClair))
                    g.FillPath(b, p);

                int largeur = (int)((Width - 1) * (float)part);
                if (largeur >= 12)
                {
                    using (GraphicsPath p = Theme.Arrondi(new Rectangle(0, y, largeur, 12), 6))
                    using (SolidBrush b = new SolidBrush(el.Couleur))
                        g.FillPath(b, p);
                }
                y += 36;
            }
        }
    }
}
