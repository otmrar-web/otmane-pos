using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OtmanePOS
{
    public static class Theme
    {
        // ---- Couleurs ----
        public static readonly Color Sidebar      = Color.FromArgb(13, 37, 66);
        public static readonly Color SidebarSurvol = Color.FromArgb(30, 60, 98);
        public static readonly Color SidebarActif = Color.FromArgb(147, 197, 253);
        public static readonly Color SidebarPied  = Color.FromArgb(24, 52, 88);
        public static readonly Color Fond         = Color.FromArgb(238, 242, 248);
        public static readonly Color Bleu         = Color.FromArgb(0, 102, 255);
        public static readonly Color Vert         = Color.FromArgb(22, 150, 60);
        public static readonly Color Rouge        = Color.FromArgb(220, 38, 38);
        public static readonly Color RougeClair   = Color.FromArgb(254, 226, 226);
        public static readonly Color Gris         = Color.FromArgb(226, 232, 240);
        public static readonly Color GrisClair    = Color.FromArgb(243, 246, 250);
        public static readonly Color Texte        = Color.FromArgb(15, 23, 42);
        public static readonly Color TexteDoux    = Color.FromArgb(100, 116, 139);
        public static readonly Color Selection    = Color.FromArgb(219, 234, 254);

        // ---- Icônes (police Segoe MDL2 Assets, installée sur Windows 10/11) ----
        public const string PoliceIcones = "Segoe MDL2 Assets";
        public const string IcRecherche = "\uE721";
        public const string IcPanier    = "\uE7BF";
        public const string IcProduit   = "\uE7B8";
        public const string IcVentes    = "\uE8A5";
        public const string IcClients   = "\uE716";
        public const string IcRapports  = "\uE9F9";
        public const string IcParam     = "\uE713";
        public const string IcScanner   = "\uED14";
        public const string IcCamera    = "\uE722";
        public const string IcCarte     = "\uE8C7";
        public const string IcTPE       = "\uE8EF";
        public const string IcComptant  = "\uE825";
        public const string IcSuppr     = "\uE74D";
        public const string IcImprimer  = "\uE749";
        public const string IcMagasin   = "\uE719";
        public const string IcPersonne  = "\uE77B";

        public static readonly CultureInfo Culture = new CultureInfo("fr-CA");

        public static string Argent(decimal v)
        {
            return v.ToString("N2", Culture) + " $";
        }

        public static Font Police(float taille, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", taille, style);
        }

        public static Font Icone(float taille)
        {
            return new Font(PoliceIcones, taille);
        }

        public static GraphicsPath Arrondi(Rectangle r, int rayon)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rayon * 2;
            if (d <= 0) { p.AddRectangle(r); return p; }
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color Assombrir(Color c, int n)
        {
            return Color.FromArgb(c.A, Math.Max(0, c.R - n), Math.Max(0, c.G - n), Math.Max(0, c.B - n));
        }

        public static Label Etiquette(string texte, Font police, Color couleur,
                                      ContentAlignment align = ContentAlignment.MiddleLeft)
        {
            return new Label
            {
                Text = texte,
                Font = police,
                ForeColor = couleur,
                TextAlign = align,
                AutoSize = false,
                BackColor = Color.Transparent
            };
        }

        // Texte gris indicatif dans un TextBox (fonctionne aussi sur .NET Framework)
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public static void TexteIndicatif(TextBox tb, string texte)
        {
            const int EM_SETCUEBANNER = 0x1501;
            if (tb.IsHandleCreated) SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, texte);
            tb.HandleCreated += (s, e) => SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, texte);
        }
    }
}
