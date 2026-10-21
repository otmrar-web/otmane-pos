using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace OtmanePOS
{
    public class LigneTicket
    {
        public string Nom { get; set; }
        public int Quantite { get; set; }
        public decimal Total { get; set; }
        public bool Taxable { get; set; }
    }

    // Tout ce qu'il faut pour dessiner un ticket (vente en cours ou vente de l'historique)
    public class DonneesTicket
    {
        public int NumeroVente { get; set; }            // Id dans Paiements (0 = inconnu)
        public DateTime Date { get; set; }
        public string ModePaiement { get; set; }        // "Comptant", "Stripe", "TPE", "Carte", "Gratuit"
        public string NumeroTransaction { get; set; }
        public List<LigneTicket> Lignes { get; set; }
        public decimal SousTotal { get; set; }
        public decimal Rabais { get; set; }
        public decimal TPS { get; set; }
        public decimal TVQ { get; set; }
        public decimal Total { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal? Recu { get; set; }              // comptant : connu seulement pour la vente en cours
        public decimal? Rendu { get; set; }
        public string PrenomClient { get; set; }
        public List<string> LignesFidelite { get; set; } = new List<string>();   // « Café : 5/9 »... (vente en cours)
        public List<KeyValuePair<string, decimal>> Recompenses { get; set; } = new List<KeyValuePair<string, decimal>>();
        public bool Copie { get; set; }                 // réimpression depuis l'historique
        public InfoCarte Carte { get; set; }            // Visa, Mastercard, débit...

        public bool EstComptant { get { return ModePaiement == "Comptant"; } }

        public static DonneesTicket DepuisVente(Vente v)
        {
            return new DonneesTicket
            {
                NumeroVente = v.Id,
                Date = v.Date,
                ModePaiement = v.ModePaiement,
                NumeroTransaction = v.NumeroTransaction,
                Lignes = v.Lignes.Select(l => new LigneTicket { Nom = l.Nom, Quantite = l.Quantite, Total = l.Total, Taxable = l.Taxable }).ToList(),
                SousTotal = v.SousTotal,
                Rabais = v.Rabais,
                TPS = v.TPS,
                TVQ = v.TVQ,
                Total = v.Total,
                MontantPaye = v.MontantPaye,
                Recu = v.EstComptant ? v.Recu : (decimal?)null,
                Rendu = v.EstComptant ? v.Rendu : (decimal?)null,
                PrenomClient = v.Client != null ? v.Client.Prenom : null,
                Recompenses = (v.ResultatsFidelite ?? new List<ResultatCarte>())
                    .Where(r => r.ProduitOffert != null && r.Rabais > 0)
                    .Select(r => new KeyValuePair<string, decimal>("Gratuit (" + r.Carte.Nom + ")", r.Rabais)).ToList(),
                LignesFidelite = v.Client == null ? new List<string>() : (v.ResultatsFidelite ?? new List<ResultatCarte>())
                    .Where(r => r.Avant > 0 || r.Gagnes > 0 || r.Utilises > 0)
                    .Select(r => r.Carte.Nom + " : " + Math.Min(r.Apres, r.Carte.TamponsRequis) + "/" + r.Carte.TamponsRequis +
                                 (r.Apres >= r.Carte.TamponsRequis ? " - prochain gratuit !" : "")).ToList(),
                Carte = v.Carte
            };
        }
    }

    public static class Ticket
    {
        public const float Largeur = 315;   // 80 mm, en centièmes de pouce

        private static PrintDocument Document(DonneesTicket t)
        {
            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "Ticket " + (t.NumeroVente > 0 ? "n° " + t.NumeroVente : "");
            doc.DefaultPageSettings.PaperSize = new PaperSize("Ticket 80 mm", (int)Largeur, 1100);
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            doc.PrintPage += (s, e) => Dessiner(e.Graphics, t, 0, 0);
            return doc;
        }

        // Fenêtre d'aperçu (bouton « Imprimer » dedans)
        public static void Apercu(IWin32Window proprietaire, DonneesTicket t)
        {
            using (PrintPreviewDialog apercu = new PrintPreviewDialog { Document = Document(t), Width = 500, Height = 800 })
                apercu.ShowDialog(proprietaire);
        }

        // Choix de l'imprimante puis impression directe
        public static void Imprimer(IWin32Window proprietaire, DonneesTicket t)
        {
            PrintDocument doc = Document(t);
            using (PrintDialog choix = new PrintDialog { Document = doc, UseEXDialog = true })
            {
                if (choix.ShowDialog(proprietaire) == DialogResult.OK) doc.Print();
            }
        }

        // Dessine le ticket et retourne la hauteur utilisée
        public static float Dessiner(Graphics g, DonneesTicket t, float gauche, float haut)
        {
            float x = gauche + 10, l = Largeur - 20, y = haut + 10;

            using (Font titre = new Font("Consolas", 12f, FontStyle.Bold))
            using (Font normal = new Font("Consolas", 8.5f))
            using (Font gras = new Font("Consolas", 10f, FontStyle.Bold))
            using (StringFormat centre = new StringFormat { Alignment = StringAlignment.Center })
            using (StringFormat droite = new StringFormat { Alignment = StringAlignment.Far })
            {
                // En-tête du commerce (Paramètres)
                float hNom = g.MeasureString(Parametres.NomCommerce.ToUpperInvariant(), titre, (int)l).Height;
                g.DrawString(Parametres.NomCommerce.ToUpperInvariant(), titre, Brushes.Black, new RectangleF(x, y, l, hNom + 2), centre); y += hNom + 4;
                foreach (string info in new[] { Parametres.Adresse, Parametres.Ville, Parametres.Telephone })
                {
                    if (string.IsNullOrWhiteSpace(info)) continue;
                    g.DrawString(info, normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 16;
                }
                y += 6;
                if (t.Copie)
                {
                    g.DrawString("*** COPIE ***", gras, Brushes.Black, new RectangleF(x, y, l, 20), centre); y += 20;
                }
                if (t.NumeroVente > 0)
                {
                    g.DrawString("Vente n° " + t.NumeroVente, normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 18;
                }
                g.DrawString(t.Date.ToString("yyyy-MM-dd HH:mm", Theme.Culture), normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 18;

                bool gratuit = t.ModePaiement == "Gratuit";
                string entete;
                if (t.EstComptant) entete = "Paiement comptant";
                else if (gratuit) entete = "Commande gratuite (fidélité)";
                else
                {
                    string ou = t.ModePaiement == "TPE" ? " - TPE" : t.ModePaiement == "Stripe" ? " - en ligne" : "";
                    entete = (t.Carte != null ? t.Carte.LibelleComplet : "Paiement par carte") + ou;
                }
                g.DrawString(entete, gras, Brushes.Black, new RectangleF(x, y, l, 20), centre); y += 20;
                if (!t.EstComptant && !gratuit && !string.IsNullOrEmpty(t.NumeroTransaction))
                {
                    g.DrawString(t.NumeroTransaction, normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 18;
                }
                y += 8;
                g.DrawLine(Pens.Black, x, y, x + l, y); y += 6;

                foreach (LigneTicket lp in t.Lignes)
                {
                    string nom = lp.Quantite + " x " + lp.Nom + (lp.Taxable ? "" : " (NT)");
                    g.DrawString(nom, normal, Brushes.Black, new RectangleF(x, y, l - 70, 18));
                    g.DrawString(Theme.Argent(lp.Total), normal, Brushes.Black, new RectangleF(x, y, l, 18), droite);
                    y += 18;
                }

                y += 4; g.DrawLine(Pens.Black, x, y, x + l, y); y += 6;
                y = Ligne(g, "Sous-total", t.SousTotal, normal, x, y, l, droite);
                foreach (KeyValuePair<string, decimal> r in t.Recompenses)
                    y = Ligne(g, r.Key, -r.Value, normal, x, y, l, droite);
                y = Ligne(g, Parametres.LibelleTPS, t.TPS, normal, x, y, l, droite);
                y = Ligne(g, Parametres.LibelleTVQ, t.TVQ, normal, x, y, l, droite);
                y += 4;
                y = Ligne(g, "TOTAL", t.Total, gras, x, y, l, droite);

                if (t.EstComptant)
                {
                    y += 6;
                    if (t.MontantPaye != t.Total)
                        y = Ligne(g, "Arrondi comptant", t.MontantPaye - t.Total, normal, x, y, l, droite);
                    y = Ligne(g, "Payé comptant", t.MontantPaye, normal, x, y, l, droite);
                    if (t.Recu.HasValue) y = Ligne(g, "Reçu", t.Recu.Value, normal, x, y, l, droite);
                    if (t.Rendu.HasValue) y = Ligne(g, "Monnaie rendue", t.Rendu.Value, normal, x, y, l, droite);
                }
                y += 14;

                if (!string.IsNullOrEmpty(t.PrenomClient))
                {
                    g.DrawString("Carte fidélité - " + t.PrenomClient, gras, Brushes.Black, new RectangleF(x, y, l, 20), centre); y += 20;
                    foreach (string ligne in t.LignesFidelite)
                    {
                        g.DrawString(ligne, normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 18;
                    }
                    y += 10;
                }

                if (t.Lignes.Any(lp => !lp.Taxable))
                {
                    g.DrawString("NT = non taxable", normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 18;
                }
                // Numéros d'inscription aux taxes
                if (!string.IsNullOrWhiteSpace(Parametres.NumeroTPS))
                {
                    g.DrawString("TPS : " + Parametres.NumeroTPS, normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 16;
                }
                if (!string.IsNullOrWhiteSpace(Parametres.NumeroTVQ))
                {
                    g.DrawString("TVQ : " + Parametres.NumeroTVQ, normal, Brushes.Black, new RectangleF(x, y, l, 18), centre); y += 16;
                }
                if (!string.IsNullOrWhiteSpace(Parametres.NumeroTPS) || !string.IsNullOrWhiteSpace(Parametres.NumeroTVQ)) y += 8;

                if (!string.IsNullOrWhiteSpace(Parametres.MessageTicket))
                {
                    float hMsg = g.MeasureString(Parametres.MessageTicket, normal, (int)l).Height;
                    g.DrawString(Parametres.MessageTicket, normal, Brushes.Black, new RectangleF(x, y, l, hMsg + 2), centre); y += hMsg + 2;
                }
            }
            return y + 10 - haut;
        }

        private static float Ligne(Graphics g, string libelle, decimal montant, Font f, float x, float y, float l, StringFormat droite)
        {
            g.DrawString(libelle, f, Brushes.Black, new RectangleF(x, y, l, 20));
            g.DrawString(Theme.Argent(montant), f, Brushes.Black, new RectangleF(x, y, l, 20), droite);
            return y + 20;
        }
    }
}
