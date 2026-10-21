using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OtmanePOS
{
    // Historique des ventes : choisir un jour, cliquer une vente, imprimer son ticket
    public class FormHistorique : Form
    {
        private DateTimePicker choixDate;
        private Label lblResume;
        private ListView lvVentes;
        private ApercuTicket apercu;
        private Panel zoneApercu;
        private BoutonArrondi btnImprimer, btnApercu;

        private List<VenteHistorique> ventes = new List<VenteHistorique>();
        private DonneesTicket ticketChoisi;

        public FormHistorique()
        {
            Text = "Otmane POS - Historique des ventes";
            BackColor = Theme.Fond;
            Font = Theme.Police(10f);
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1250, 860);
            MinimumSize = new Size(1000, 700);
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
                if (e.KeyCode == Keys.F5) Charger();
                if (e.Control && e.KeyCode == Keys.P) { e.SuppressKeyPress = true; ImprimerChoisi(); }
            };

            TableLayoutPanel racine = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Fond,
                Padding = new Padding(24, 18, 24, 20),
                ColumnCount = 1,
                RowCount = 2
            };
            racine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            racine.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            racine.Controls.Add(CreerEntete(), 0, 0);
            racine.Controls.Add(CreerContenu(), 0, 1);
            Controls.Add(racine);

            Shown += (s, e) => Charger();
        }

        // =====================================================================
        // EN-TÊTE : titre, choix du jour, résumé
        // =====================================================================
        private Control CreerEntete()
        {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 0, 0, 12) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label titre = Theme.Etiquette("Historique des ventes", Theme.Police(20f, FontStyle.Bold), Theme.Texte);
            titre.AutoSize = true;
            titre.Anchor = AnchorStyles.Left;
            titre.Margin = new Padding(0, 0, 24, 0);

            FlowLayoutPanel nav = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            BoutonArrondi precedent = PetitBoutonNav("\uE76B");   // flèche gauche
            precedent.Click += (s, e) => choixDate.Value = choixDate.Value.AddDays(-1);

            choixDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Long,
                Font = Theme.Police(12f),
                Width = 290,
                Margin = new Padding(8, 8, 8, 0),
                Value = DateTime.Today
            };
            choixDate.ValueChanged += (s, e) => Charger();

            BoutonArrondi suivant = PetitBoutonNav("\uE76C");     // flèche droite
            suivant.Click += (s, e) => choixDate.Value = choixDate.Value.AddDays(1);

            Font fAujourdhui = Theme.Police(11f);
            BoutonArrondi aujourdhui = new BoutonArrondi
            {
                Text = "Aujourd'hui",
                Font = fAujourdhui,
                BackColor = Theme.Gris,
                ForeColor = Theme.Texte,
                Size = new Size(TextRenderer.MeasureText("Aujourd'hui", fAujourdhui).Width + 36, 42),
                Margin = new Padding(12, 0, 0, 0)
            };
            aujourdhui.Click += (s, e) => choixDate.Value = DateTime.Today;

            nav.Controls.AddRange(new Control[] { precedent, choixDate, suivant, aujourdhui });

            lblResume = Theme.Etiquette("", Theme.Police(12f, FontStyle.Bold), Theme.Texte, ContentAlignment.MiddleRight);
            lblResume.Dock = DockStyle.Fill;

            t.Controls.Add(titre, 0, 0);
            t.Controls.Add(nav, 1, 0);
            t.Controls.Add(lblResume, 2, 0);
            return t;
        }

        private BoutonArrondi PetitBoutonNav(string icone)
        {
            return new BoutonArrondi
            {
                Text = "",
                Icone = icone,
                TailleIcone = 12f,
                BackColor = Color.White,
                CouleurIcone = Theme.Texte,
                Size = new Size(42, 42),
                Margin = new Padding(0)
            };
        }

        // =====================================================================
        // CONTENU : liste des ventes | ticket
        // =====================================================================
        private Control CreerContenu()
        {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ---- Liste des ventes ----
            PanneauArrondi carteListe = new PanneauArrondi { Dock = DockStyle.Fill, Bordure = Theme.Gris, Padding = new Padding(18, 10, 18, 14), Margin = new Padding(0, 0, 8, 0) };
            Label tListe = Theme.Etiquette("Ventes du jour", Theme.Police(14f, FontStyle.Bold), Theme.Texte);
            tListe.Dock = DockStyle.Top;
            tListe.Height = 40;

            lvVentes = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                Font = Theme.Police(10.5f),
                ForeColor = Theme.Texte,
                BackColor = Color.White,
                SmallImageList = new ImageList { ImageSize = new Size(1, 32) }   // lignes plus hautes
            };
            lvVentes.Columns.Add("N°", 80);
            lvVentes.Columns.Add("Heure", 80);
            lvVentes.Columns.Add("Paiement", 200);
            lvVentes.Columns.Add("Client", 200);
            lvVentes.Columns.Add("Montant", 120, HorizontalAlignment.Right);
            lvVentes.Resize += (s, e) =>
            {
                int fixe = 80 + 80 + 200 + 120;
                lvVentes.Columns[3].Width = Math.Max(120, lvVentes.ClientSize.Width - fixe - 4);
            };
            lvVentes.SelectedIndexChanged += (s, e) => AfficherVenteChoisie();
            lvVentes.DoubleClick += (s, e) => ImprimerChoisi();

            carteListe.Controls.Add(lvVentes);
            carteListe.Controls.Add(tListe);

            // ---- Ticket ----
            PanneauArrondi carteTicket = new PanneauArrondi { Dock = DockStyle.Fill, Bordure = Theme.Gris, Padding = new Padding(18, 10, 18, 16), Margin = new Padding(8, 0, 0, 0) };
            Label tTicket = Theme.Etiquette("Ticket", Theme.Police(14f, FontStyle.Bold), Theme.Texte);
            tTicket.Dock = DockStyle.Top;
            tTicket.Height = 40;

            zoneApercu = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.GrisClair };
            apercu = new ApercuTicket();
            zoneApercu.Controls.Add(apercu);
            zoneApercu.Resize += (s, e) => CentrerApercu();
            apercu.Resize += (s, e) => CentrerApercu();

            TableLayoutPanel boutons = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 66, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 14, 0, 0), BackColor = Color.White };
            boutons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            boutons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            boutons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            btnImprimer = new BoutonArrondi
            {
                Text = "Imprimer le ticket",
                Icone = Theme.IcImprimer,
                TailleIcone = 16f,
                Font = Theme.Police(12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnImprimer.Click += (s, e) => ImprimerChoisi();

            btnApercu = new BoutonArrondi
            {
                Text = "Aperçu",
                BackColor = Theme.Gris,
                ForeColor = Theme.Texte,
                Font = Theme.Police(11f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnApercu.Click += (s, e) => { if (ticketChoisi != null) Ticket.Apercu(this, ticketChoisi); };

            boutons.Controls.Add(btnImprimer, 0, 0);
            boutons.Controls.Add(btnApercu, 1, 0);

            carteTicket.Controls.Add(zoneApercu);
            carteTicket.Controls.Add(boutons);
            carteTicket.Controls.Add(tTicket);

            t.Controls.Add(carteListe, 0, 0);
            t.Controls.Add(carteTicket, 1, 0);
            return t;
        }

        private void CentrerApercu()
        {
            int x = Math.Max(0, (zoneApercu.ClientSize.Width - apercu.Width) / 2);
            apercu.Location = new Point(x + zoneApercu.AutoScrollPosition.X, 16 + zoneApercu.AutoScrollPosition.Y);
        }

        // =====================================================================
        // DONNÉES
        // =====================================================================
        private void Charger()
        {
            if (lvVentes == null) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                DateTime jour = choixDate.Value.Date;
                ventes = Historique.Ventes(jour, jour.AddDays(1));

                lvVentes.BeginUpdate();
                lvVentes.Items.Clear();
                foreach (VenteHistorique v in ventes)
                {
                    string mode = v.ModeAffiche + (v.Statut == "Réussi" ? "" : " (" + v.Statut + ")");
                    ListViewItem item = new ListViewItem(new[]
                    {
                        v.Id.ToString(),
                        v.Date.ToString("HH:mm", Theme.Culture),
                        mode,
                        v.Client,
                        Theme.Argent(v.Montant)
                    }) { Tag = v };
                    if (v.Statut != "Réussi") item.ForeColor = Theme.Rouge;
                    lvVentes.Items.Add(item);
                }
                lvVentes.EndUpdate();

                List<VenteHistorique> reussies = ventes.Where(v => v.Statut == "Réussi").ToList();
                lblResume.Text = reussies.Count + " vente" + (reussies.Count > 1 ? "s" : "") + " : " + Theme.Argent(reussies.Sum(v => v.Montant));

                if (lvVentes.Items.Count > 0) lvVentes.Items[0].Selected = true;
                else AfficherTicket(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impossible de charger l'historique :\n\n" + ex.Message, "Historique",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void AfficherVenteChoisie()
        {
            if (lvVentes.SelectedItems.Count == 0) return;
            VenteHistorique v = (VenteHistorique)lvVentes.SelectedItems[0].Tag;
            try
            {
                AfficherTicket(Historique.TicketDe(v));
            }
            catch (Exception ex)
            {
                AfficherTicket(null);
                MessageBox.Show(this, "Impossible de lire la vente n° " + v.Id + " :\n\n" + ex.Message, "Historique",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AfficherTicket(DonneesTicket t)
        {
            ticketChoisi = t;
            apercu.Afficher(t);
            btnImprimer.Enabled = btnApercu.Enabled = t != null;
            zoneApercu.AutoScrollPosition = new Point(0, 0);
            CentrerApercu();
        }

        private void ImprimerChoisi()
        {
            if (ticketChoisi == null)
            {
                MessageBox.Show(this, "Choisis d'abord une vente dans la liste.", "Historique",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Ticket.Imprimer(this, ticketChoisi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Impression impossible :\n\n" + ex.Message, "Historique",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Aperçu du ticket à l'écran (le même dessin que l'impression)
    public class ApercuTicket : Control
    {
        private DonneesTicket ticket;
        private readonly Font policeVide = Theme.Police(11f);

        public ApercuTicket()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Size = new Size((int)Ticket.Largeur + 2, 200);
        }

        public void Afficher(DonneesTicket t)
        {
            ticket = t;
            int hauteur = 200;
            if (t != null)
            {
                using (Bitmap b = new Bitmap(1, 1))
                using (Graphics g = Graphics.FromImage(b))
                    hauteur = (int)Math.Ceiling(Ticket.Dessiner(g, t, 0, 0)) + 4;
            }
            Size = new Size((int)Ticket.Largeur + 2, hauteur);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            using (Pen bord = new Pen(Theme.Gris)) g.DrawRectangle(bord, 0, 0, Width - 1, Height - 1);

            if (ticket == null)
            {
                TextRenderer.DrawText(g, "Choisis une vente dans la liste", policeVide, ClientRectangle, Theme.TexteDoux,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Ticket.Dessiner(g, ticket, 1, 0);
        }
    }
}
