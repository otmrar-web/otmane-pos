using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace OtmanePOS
{
    public class ResumeVentes
    {
        public int NbVentes { get; set; }
        public int NbComptant { get; set; }
        public int NbCarte { get; set; }
        public int NbGratuit { get; set; }
        public int CafesGratuits { get; set; }
        public decimal ChiffreAffaires { get; set; }
        public decimal Comptant { get; set; }
        public decimal Carte { get; set; }

        // Panier moyen des ventes payées (les commandes gratuites ne comptent pas)
        public decimal PanierMoyen
        {
            get { int payees = NbVentes - NbGratuit; return payees > 0 ? ChiffreAffaires / payees : 0; }
        }
    }

    public class LigneRapport
    {
        public string Libelle { get; set; }
        public int Quantite { get; set; }
        public decimal Montant { get; set; }
        public int Tampons { get; set; }
    }

    // Requêtes des rapports (lecture seule)
    public static class Rapports
    {
        // Seulement les paiements réussis de la période
        private const string Filtre = " p.Statut = 'Réussi' AND p.DatePaiement >= @debut AND p.DatePaiement < @fin ";

        private static SqliteCommand Commande(SqliteConnection cn, string sql, DateTime debut, DateTime fin)
        {
            SqliteCommand cmd = new SqliteCommand(sql, cn);
            cmd.Parameters.AddWithValue("@debut", Base.Date(debut));
            cmd.Parameters.AddWithValue("@fin", Base.Date(fin));
            return cmd;
        }

        public static ResumeVentes Resume(DateTime debut, DateTime fin)
        {
            ResumeVentes r = new ResumeVentes();
            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            {
                cn.Open();
                string sql =
                    "SELECT COUNT(*) AS Nb, IFNULL(SUM(p.Montant), 0) AS CA, " +
                    "IFNULL(SUM(CASE WHEN p.NumeroTransaction LIKE 'COMPTANT-%' THEN p.Montant ELSE 0 END), 0) AS Comptant, " +
                    "IFNULL(SUM(CASE WHEN p.NumeroTransaction LIKE 'COMPTANT-%' THEN 1 ELSE 0 END), 0) AS NbComptant, " +
                    "IFNULL(SUM(CASE WHEN p.NumeroTransaction LIKE 'GRATUIT-%' THEN 1 ELSE 0 END), 0) AS NbGratuit " +
                    "FROM Paiements p WHERE" + Filtre;
                using (SqliteCommand cmd = Commande(cn, sql, debut, fin))
                using (SqliteDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        r.NbVentes = Convert.ToInt32(rd["Nb"]);
                        r.ChiffreAffaires = Base.Argent(rd["CA"]);
                        r.Comptant = Base.Argent(rd["Comptant"]);
                        r.NbComptant = Convert.ToInt32(rd["NbComptant"]);
                        r.NbGratuit = Convert.ToInt32(rd["NbGratuit"]);
                    }
                }
                r.Carte = r.ChiffreAffaires - r.Comptant;
                r.NbCarte = r.NbVentes - r.NbComptant - r.NbGratuit;

                sql = "SELECT COUNT(*) FROM DetailsVente d JOIN Paiements p ON p.Id = d.PaiementId " +
                      "WHERE" + Filtre + "AND d.Total < 0";   // une ligne négative = un produit offert
                using (SqliteCommand cmd = Commande(cn, sql, debut, fin))
                    r.CafesGratuits = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return r;
        }

        // Répartition des paiements : Comptant, Visa, Visa Débit, Mastercard, Débit (Interac)...
        public static List<LigneRapport> RepartitionPaiements(DateTime debut, DateTime fin)
        {
            string sql =
                "SELECT IFNULL(p.ModePaiement, CASE WHEN p.NumeroTransaction LIKE 'COMPTANT-%' THEN 'Comptant' " +
                "WHEN p.NumeroTransaction LIKE 'GRATUIT-%' THEN 'Gratuit' ELSE 'Carte' END) AS Mode, " +
                "p.MarqueCarte, p.TypeCarte, COUNT(*) AS Nb, SUM(p.Montant) AS Total " +
                "FROM Paiements p WHERE" + Filtre +
                "GROUP BY IFNULL(p.ModePaiement, CASE WHEN p.NumeroTransaction LIKE 'COMPTANT-%' THEN 'Comptant' " +
                "WHEN p.NumeroTransaction LIKE 'GRATUIT-%' THEN 'Gratuit' ELSE 'Carte' END), p.MarqueCarte, p.TypeCarte";

            Dictionary<string, LigneRapport> parLibelle = new Dictionary<string, LigneRapport>();
            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            {
                cn.Open();
                using (SqliteCommand cmd = Commande(cn, sql, debut, fin))
                using (SqliteDataReader rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        string mode = Convert.ToString(rd["Mode"]);
                        if (mode == "Gratuit") continue;

                        string libelle = mode == "Comptant" ? "Comptant"
                            : rd["MarqueCarte"] == DBNull.Value ? "Carte (type inconnu)"
                            : new InfoCarte
                            {
                                Marque = Convert.ToString(rd["MarqueCarte"]),
                                Financement = rd["TypeCarte"] == DBNull.Value ? null : Convert.ToString(rd["TypeCarte"])
                            }.Libelle;

                        LigneRapport ligne;
                        if (!parLibelle.TryGetValue(libelle, out ligne))
                        {
                            ligne = new LigneRapport { Libelle = libelle };
                            parLibelle[libelle] = ligne;
                        }
                        ligne.Quantite += Convert.ToInt32(rd["Nb"]);
                        ligne.Montant += Base.Argent(rd["Total"]);
                    }
            }
            return parLibelle.Values.OrderByDescending(l => l.Montant).ToList();
        }

        public static List<LigneRapport> MeilleursProduits(DateTime debut, DateTime fin)
        {
            string sql =
                "SELECT d.NomProduit, SUM(d.Quantite) AS Qte, SUM(d.Total) AS Total " +
                "FROM DetailsVente d JOIN Paiements p ON p.Id = d.PaiementId " +
                "WHERE" + Filtre + "AND d.Total >= 0 " +          // sans les lignes « café gratuit »
                "GROUP BY d.NomProduit ORDER BY Qte DESC, Total DESC LIMIT 10";

            List<LigneRapport> liste = new List<LigneRapport>();
            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            {
                cn.Open();
                using (SqliteCommand cmd = Commande(cn, sql, debut, fin))
                using (SqliteDataReader rd = cmd.ExecuteReader())
                    while (rd.Read())
                        liste.Add(new LigneRapport
                        {
                            Libelle = Convert.ToString(rd["NomProduit"]),
                            Quantite = Convert.ToInt32(rd["Qte"]),
                            Montant = Base.Argent(rd["Total"])
                        });
            }
            return liste;
        }

        public static List<LigneRapport> MeilleursClients(DateTime debut, DateTime fin)
        {
            string sql =
                "SELECT c.Prenom, c.Nom, COUNT(p.Id) AS Visites, SUM(p.Montant) AS Total " +
                "FROM Paiements p JOIN Clients c ON c.Id = p.ClientId " +
                "WHERE" + Filtre +
                "GROUP BY c.Id, c.Prenom, c.Nom ORDER BY Total DESC, Visites DESC LIMIT 10";

            List<LigneRapport> liste = new List<LigneRapport>();
            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            {
                cn.Open();
                using (SqliteCommand cmd = Commande(cn, sql, debut, fin))
                using (SqliteDataReader rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        string nom = rd["Nom"] == DBNull.Value ? "" : Convert.ToString(rd["Nom"]);
                        liste.Add(new LigneRapport
                        {
                            Libelle = (Convert.ToString(rd["Prenom"]) + " " + nom).Trim(),
                            Quantite = Convert.ToInt32(rd["Visites"]),
                            Montant = Base.Argent(rd["Total"])
                        });
                    }
            }
            return liste;
        }

        // Clé = heure (0-23)
        public static Dictionary<int, decimal> VentesParHeure(DateTime debut, DateTime fin)
        {
            string sql = "SELECT CAST(strftime('%H', p.DatePaiement) AS INTEGER) AS H, SUM(p.Montant) AS T FROM Paiements p " +
                         "WHERE" + Filtre + "GROUP BY H";
            Dictionary<int, decimal> res = new Dictionary<int, decimal>();
            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            {
                cn.Open();
                using (SqliteCommand cmd = Commande(cn, sql, debut, fin))
                using (SqliteDataReader rd = cmd.ExecuteReader())
                    while (rd.Read()) res[Convert.ToInt32(rd["H"])] = Base.Argent(rd["T"]);
            }
            return res;
        }

        // Clé = date (sans l'heure)
        public static Dictionary<DateTime, decimal> VentesParJour(DateTime debut, DateTime fin)
        {
            string sql = "SELECT date(p.DatePaiement) AS J, SUM(p.Montant) AS T FROM Paiements p " +
                         "WHERE" + Filtre + "GROUP BY J";
            Dictionary<DateTime, decimal> res = new Dictionary<DateTime, decimal>();
            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            {
                cn.Open();
                using (SqliteCommand cmd = Commande(cn, sql, debut, fin))
                using (SqliteDataReader rd = cmd.ExecuteReader())
                    while (rd.Read()) res[Base.LireDate(rd["J"]).Date] = Base.Argent(rd["T"]);
            }
            return res;
        }
    }

    // Une vente de la table Paiements, pour l'historique
    public class VenteHistorique
    {
        public int Id { get; set; }
        public decimal Montant { get; set; }
        public DateTime Date { get; set; }
        public string Statut { get; set; }
        public string NumeroTransaction { get; set; }
        public string PrenomClient { get; set; }
        public string NomClient { get; set; }

        public string ModeEnregistre { get; set; }   // colonne ModePaiement
        public InfoCarte Carte { get; set; }

        // Colonne ModePaiement si elle est remplie, sinon déduit du numéro de transaction
        public string Mode
        {
            get
            {
                if (!string.IsNullOrEmpty(ModeEnregistre)) return ModeEnregistre;
                string n = NumeroTransaction ?? "";
                if (n.StartsWith("COMPTANT-")) return "Comptant";
                if (n.StartsWith("GRATUIT-")) return "Gratuit";
                if (n.StartsWith("pi_")) return "Carte";
                return "Autre";
            }
        }

        public string Client
        {
            get { return string.IsNullOrEmpty(PrenomClient) ? "" : (PrenomClient + " " + NomClient).Trim(); }
        }

        // Pour la liste : « Visa Débit (TPE) », « Mastercard (en ligne) », « Comptant »...
        public string ModeAffiche
        {
            get
            {
                string mode = Mode;
                if (mode == "Comptant" || mode == "Gratuit") return mode;
                string ou = mode == "TPE" ? " (TPE)" : mode == "Stripe" ? " (en ligne)" : "";
                return (Carte != null ? Carte.Libelle : "Carte") + ou;
            }
        }
    }

    public static class Historique
    {
        private static decimal TauxTPS { get { return Parametres.TauxTPS; } }
        private static decimal TauxTVQ { get { return Parametres.TauxTVQ; } }

        public static List<VenteHistorique> Ventes(DateTime debut, DateTime fin)
        {
            string sql =
                "SELECT p.Id, p.Montant, p.DatePaiement, p.Statut, p.NumeroTransaction, c.Prenom, c.Nom, " +
                "p.ModePaiement, p.MarqueCarte, p.TypeCarte, p.Derniers4 " +
                "FROM Paiements p LEFT JOIN Clients c ON c.Id = p.ClientId " +
                "WHERE p.DatePaiement >= @debut AND p.DatePaiement < @fin ORDER BY p.Id DESC";

            List<VenteHistorique> liste = new List<VenteHistorique>();
            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@debut", Base.Date(debut));
                cmd.Parameters.AddWithValue("@fin", Base.Date(fin));
                cn.Open();
                using (SqliteDataReader rd = cmd.ExecuteReader())
                    while (rd.Read())
                        liste.Add(new VenteHistorique
                        {
                            Id = Convert.ToInt32(rd["Id"]),
                            Montant = Base.Argent(rd["Montant"]),
                            Date = Base.LireDate(rd["DatePaiement"]),
                            Statut = Convert.ToString(rd["Statut"]),
                            NumeroTransaction = rd["NumeroTransaction"] == DBNull.Value ? "" : Convert.ToString(rd["NumeroTransaction"]),
                            PrenomClient = rd["Prenom"] == DBNull.Value ? null : Convert.ToString(rd["Prenom"]),
                            NomClient = rd["Nom"] == DBNull.Value ? "" : Convert.ToString(rd["Nom"]),
                            ModeEnregistre = rd["ModePaiement"] == DBNull.Value ? null : Convert.ToString(rd["ModePaiement"]),
                            Carte = rd["MarqueCarte"] == DBNull.Value ? null : new InfoCarte
                            {
                                Marque = Convert.ToString(rd["MarqueCarte"]),
                                Financement = rd["TypeCarte"] == DBNull.Value ? null : Convert.ToString(rd["TypeCarte"]),
                                Derniers4 = rd["Derniers4"] == DBNull.Value ? null : Convert.ToString(rd["Derniers4"])
                            }
                        });
            }
            return liste;
        }

        // Reconstruit le ticket d'une vente à partir de DetailsVente
        // (les taxes sont recalculées avec la colonne Taxable des produits)
        public static DonneesTicket TicketDe(VenteHistorique v)
        {
            string sql =
                "SELECT d.NomProduit, d.Quantite, d.Total, IFNULL(pr.Taxable, 1) AS Taxable " +
                "FROM DetailsVente d LEFT JOIN Produits pr ON pr.Id = d.ProduitId " +
                "WHERE d.PaiementId = @id ORDER BY d.Id";

            List<LigneTicket> lignes = new List<LigneTicket>();
            decimal rabais = 0, baseTaxable = 0;
            List<KeyValuePair<string, decimal>> recompenses = new List<KeyValuePair<string, decimal>>();

            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@id", v.Id);
                cn.Open();
                using (SqliteDataReader rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        decimal total = Base.Argent(rd["Total"]);
                        bool taxable = Convert.ToBoolean(rd["Taxable"]);
                        if (taxable) baseTaxable += total;

                        if (total < 0)
                        {
                            // Ligne « Gratuit (Café) : Latte - Grand » (ou l'ancien « Café gratuit (fidélité) - ... »)
                            rabais += -total;
                            string nom = Convert.ToString(rd["NomProduit"]);
                            int coupe = nom.IndexOf(" : ");
                            if (coupe < 0) coupe = nom.IndexOf(" - ");
                            recompenses.Add(new KeyValuePair<string, decimal>(coupe > 0 ? nom.Substring(0, coupe) : "Gratuit fidélité", -total));
                        }
                        else
                            lignes.Add(new LigneTicket
                            {
                                Nom = Convert.ToString(rd["NomProduit"]),
                                Quantite = Convert.ToInt32(rd["Quantite"]),
                                Total = total,
                                Taxable = taxable
                            });
                    }
            }

            decimal sousTotal = lignes.Sum(l => l.Total);
            decimal tps = Math.Round(Math.Max(0, baseTaxable) * TauxTPS, 2, MidpointRounding.AwayFromZero);
            decimal tvq = Math.Round(Math.Max(0, baseTaxable) * TauxTVQ, 2, MidpointRounding.AwayFromZero);
            decimal totalCalcule = sousTotal - rabais + tps + tvq;
            string mode = v.Mode;

            return new DonneesTicket
            {
                NumeroVente = v.Id,
                Date = v.Date,
                ModePaiement = mode,
                NumeroTransaction = v.NumeroTransaction,
                Lignes = lignes,
                SousTotal = sousTotal,
                Rabais = rabais,
                Recompenses = recompenses,
                TPS = tps,
                TVQ = tvq,
                // Comptant : total avant l'arrondi aux 5 cents ; sinon, le montant réellement facturé
                Total = mode == "Comptant" ? totalCalcule : v.Montant,
                MontantPaye = v.Montant,
                PrenomClient = v.PrenomClient,
                Carte = v.Carte,
                Copie = true
            };
        }
    }
}
