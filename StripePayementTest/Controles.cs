using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OtmanePOS
{
    // ============================================================
    // Panneau blanc aux coins arrondis (avec bordure optionnelle)
    // ============================================================
    public class PanneauArrondi : Panel
    {
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Rayon { get; set; }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Bordure { get; set; }

        public PanneauArrondi()
        {
            Rayon = 14;
            Bordure = Color.Empty;
            BackColor = Color.White;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color fondParent = Parent != null ? Parent.BackColor : SystemColors.Control;
            using (SolidBrush b = new SolidBrush(fondParent)) g.FillRectangle(b, ClientRectangle);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = Theme.Arrondi(new Rectangle(0, 0, Width - 1, Height - 1), Rayon))
            {
                using (SolidBrush b = new SolidBrush(BackColor)) g.FillPath(b, p);
                if (Bordure != Color.Empty)
                    using (Pen pen = new Pen(Bordure, 1f)) g.DrawPath(pen, p);
            }
        }
    }

    // ============================================================
    // Bouton arrondi avec icône
    // ============================================================
    public class BoutonArrondi : Button
    {
        private bool survol;
        private float tailleIcone;
        private Font policeIcone;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Rayon { get; set; }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Icone { get; set; }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CouleurIcone { get; set; }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CouleurSurvol { get; set; }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AlignerGauche { get; set; }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float TailleIcone
        {
            get { return tailleIcone; }
            set { tailleIcone = value; policeIcone = Theme.Icone(value); Invalidate(); }
        }

        public BoutonArrondi()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Rayon = 10;
            Icone = "";
            CouleurIcone = Color.Empty;
            CouleurSurvol = Color.Empty;
            TailleIcone = 16f;
            BackColor = Theme.Bleu;
            ForeColor = Color.White;
            Font = Theme.Police(11f, FontStyle.Bold);
        }

        protected override void OnMouseEnter(EventArgs e) { survol = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { survol = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color fondParent = Parent != null ? Parent.BackColor : SystemColors.Control;
            using (SolidBrush b = new SolidBrush(fondParent)) g.FillRectangle(b, ClientRectangle);

            Color fond = BackColor;
            if (survol) fond = CouleurSurvol != Color.Empty ? CouleurSurvol : Theme.Assombrir(BackColor, 18);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = Theme.Arrondi(new Rectangle(0, 0, Width - 1, Height - 1), Rayon))
            using (SolidBrush b = new SolidBrush(fond))
                g.FillPath(b, p);

            TextFormatFlags f = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
                                TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
            Size tTexte = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(g, Text, Font, Size.Empty, f);
            Size tIcone = string.IsNullOrEmpty(Icone) ? Size.Empty : TextRenderer.MeasureText(g, Icone, policeIcone, Size.Empty, f);
            int espace = (tIcone.Width > 0 && tTexte.Width > 0) ? 12 : 0;
            int largeur = tIcone.Width + espace + tTexte.Width;
            int x = AlignerGauche ? 20 : Math.Max(4, (Width - largeur) / 2);

            if (tIcone.Width > 0)
                TextRenderer.DrawText(g, Icone, policeIcone, new Rectangle(x, 0, tIcone.Width + 2, Height),
                    CouleurIcone != Color.Empty ? CouleurIcone : ForeColor, f);
            if (tTexte.Width > 0)
                TextRenderer.DrawText(g, Text, Font, new Rectangle(x + tIcone.Width + espace, 0, Width, Height),
                    ForeColor, f);
        }
    }

    // ============================================================
    // Carte d'un produit dans la grille
    // ============================================================
    public class CarteProduit : PanneauArrondi
    {
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Produit Produit { get; private set; }

        public CarteProduit(Produit produit, Image image)
        {
            Produit = produit;
            Size = new Size(150, 190);
            Margin = new Padding(0, 0, 12, 12);
            BackColor = Color.White;
            Bordure = Theme.Gris;
            Rayon = 12;
            Cursor = Cursors.Hand;

            Control visuel;
            if (image != null)
            {
                visuel = new PictureBox { Image = image, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
            }
            else
            {
                visuel = new Label
                {
                    Text = string.IsNullOrEmpty(produit.Emoji) ? "🛒" : produit.Emoji,
                    Font = new Font("Segoe UI Emoji", 38f),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.White
                };
            }
            visuel.Bounds = new Rectangle(15, 8, 120, 92);

            Label nom = Theme.Etiquette(produit.Nom, Theme.Police(10.5f, FontStyle.Bold), Theme.Texte, ContentAlignment.MiddleCenter);
            nom.BackColor = Color.White;
            nom.Bounds = new Rectangle(4, 102, 142, 50);

            Label prix = Theme.Etiquette(produit.PrixAffiche, Theme.Police(12.5f, FontStyle.Bold), Theme.Bleu, ContentAlignment.MiddleCenter);
            prix.BackColor = Color.White;
            prix.Bounds = new Rectangle(4, 152, 142, 30);

            Controls.Add(visuel);
            Controls.Add(nom);
            Controls.Add(prix);

            foreach (Control c in Controls)
            {
                c.Cursor = Cursors.Hand;
                c.Click += (s, e) => OnClick(e);
                c.MouseEnter += (s, e) => Survol(true);
                c.MouseLeave += (s, e) => VerifierSortie();
            }
            MouseEnter += (s, e) => Survol(true);
            MouseLeave += (s, e) => VerifierSortie();
        }

        private void Survol(bool actif)
        {
            Bordure = actif ? Theme.Bleu : Theme.Gris;
            Invalidate();
        }

        private void VerifierSortie()
        {
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position))) Survol(false);
        }
    }

    // ============================================================
    // Positions des colonnes du panier (partagées entête / lignes)
    // ============================================================
    public static class ColonnesPanier
    {
        public const int LargeurQte = 92;
        public static int Prix(int w)  { return (int)(w * 0.33); }
        public static int Qte(int w)   { return (int)(w * 0.47); }
        public static int Total(int w) { return (int)(w * 0.72); }
    }

    public class EntetePanier : Panel
    {
        private Label lProduit, lPrix, lQte, lTotal;

        public EntetePanier()
        {
            Dock = DockStyle.Top;
            BackColor = Theme.GrisClair;
            lProduit = Cree("Produit", ContentAlignment.MiddleLeft);
            lPrix = Cree("Prix", ContentAlignment.MiddleLeft);
            lQte = Cree("Qté", ContentAlignment.MiddleCenter);
            lTotal = Cree("Total", ContentAlignment.MiddleLeft);
            Height = 40;
        }

        private Label Cree(string texte, ContentAlignment align)
        {
            Label l = Theme.Etiquette(texte, Theme.Police(10f, FontStyle.Bold), Theme.Texte, align);
            Controls.Add(l);
            return l;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lTotal == null) return;
            int w = Width - 20;
            int h = Height;
            lProduit.SetBounds(10, 0, ColonnesPanier.Prix(w) - 10, h);
            lPrix.SetBounds(ColonnesPanier.Prix(w), 0, ColonnesPanier.Qte(w) - ColonnesPanier.Prix(w), h);
            lQte.SetBounds(ColonnesPanier.Qte(w), 0, ColonnesPanier.LargeurQte, h);
            lTotal.SetBounds(ColonnesPanier.Total(w), 0, w - ColonnesPanier.Total(w), h);
        }
    }

    // ============================================================
    // Une ligne du panier : image, nom, prix, - qté +, total, poubelle
    // ============================================================
    public class LignePanierControl : Panel
    {
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LignePanier Ligne { get; private set; }
        public event EventHandler Plus, Moins, Supprimer, Choisie;

        private bool selectionnee;
        private Control visuel;
        private Label lNom, lPrix, lQte, lTotal;
        private BoutonArrondi bMoins, bPlus, bSuppr;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selectionnee
        {
            get { return selectionnee; }
            set
            {
                selectionnee = value;
                BackColor = value ? Theme.Selection : Color.White;
                if (bSuppr != null) { bSuppr.BackColor = BackColor; bSuppr.CouleurSurvol = Theme.RougeClair; }
                Invalidate(true);
            }
        }

        public LignePanierControl(LignePanier ligne)
        {
            Ligne = ligne;
            Margin = new Padding(0);
            BackColor = Color.White;
            DoubleBuffered = true;

            if (ligne.Image != null)
            {
                visuel = new PictureBox { Image = ligne.Image, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Theme.GrisClair };
            }
            else
            {
                visuel = new Label
                {
                    Text = string.IsNullOrEmpty(ligne.Emoji) ? "🛒" : ligne.Emoji,
                    Font = new Font("Segoe UI Emoji", 18f),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Theme.GrisClair
                };
            }
            visuel.Size = new Size(40, 40);

            lNom = Theme.Etiquette(ligne.Nom.Replace(" - ", "\n"), Theme.Police(9f), Theme.Texte);
            lPrix = Theme.Etiquette(Theme.Argent(ligne.Prix), Theme.Police(9.5f), Theme.Texte);
            lQte = Theme.Etiquette(ligne.Quantite.ToString(), Theme.Police(10.5f), Theme.Texte, ContentAlignment.MiddleCenter);
            lQte.BackColor = Color.White;
            lTotal = Theme.Etiquette(Theme.Argent(ligne.Total), Theme.Police(10f, FontStyle.Bold), Theme.Texte);

            bMoins = PetitBouton("−");
            bPlus = PetitBouton("+");
            bSuppr = new BoutonArrondi
            {
                Text = "",
                Icone = Theme.IcSuppr,
                TailleIcone = 13f,
                BackColor = Color.White,
                CouleurIcone = Theme.TexteDoux,
                CouleurSurvol = Theme.RougeClair,
                Rayon = 6
            };

            bMoins.Click += (s, e) => Declencher(Moins);
            bPlus.Click += (s, e) => Declencher(Plus);
            bSuppr.Click += (s, e) => Declencher(Supprimer);

            Controls.AddRange(new Control[] { visuel, lNom, lPrix, bMoins, lQte, bPlus, lTotal, bSuppr });

            Click += (s, e) => Declencher(Choisie);
            foreach (Control c in new Control[] { visuel, lNom, lPrix, lQte, lTotal })
                c.Click += (s, e) => Declencher(Choisie);

            Height = 80;
        }

        private void Declencher(EventHandler evt)
        {
            if (evt != null) evt(this, EventArgs.Empty);
        }

        private BoutonArrondi PetitBouton(string texte)
        {
            return new BoutonArrondi
            {
                Text = texte,
                Font = Theme.Police(12f, FontStyle.Bold),
                BackColor = Theme.GrisClair,
                ForeColor = Theme.Texte,
                Rayon = 6
            };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (bSuppr == null) return;
            int w = Width, h = Height;
            int xPrix = ColonnesPanier.Prix(w), xQte = ColonnesPanier.Qte(w), xTotal = ColonnesPanier.Total(w);
            int yb = (h - 30) / 2;

            visuel.Location = new Point(8, (h - visuel.Height) / 2);
            lNom.SetBounds(54, 4, Math.Max(20, xPrix - 56), h - 8);
            lPrix.SetBounds(xPrix, 0, xQte - xPrix, h);
            bMoins.SetBounds(xQte, yb, 28, 30);
            lQte.SetBounds(xQte + 28, yb, 36, 30);
            bPlus.SetBounds(xQte + 64, yb, 28, 30);
            lTotal.SetBounds(xTotal, 0, Math.Max(20, w - xTotal - 34), h);
            bSuppr.SetBounds(w - 32, yb, 30, 30);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Theme.Gris)) e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }
    }
}
