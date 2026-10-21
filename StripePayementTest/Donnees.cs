using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace OtmanePOS
{
    public static class Donnees
    {
        // Base SQLite (voir Base.cs)
        public static string ChaineConnexion { get { return Base.ChaineConnexion; } }

        private static readonly string[] OrdreTailles = { "Petit", "Moyen", "Grand" };

        // ------------------------------------------------------------------
        // Lecture de la table Produits (toutes les lignes, telles quelles)
        // ------------------------------------------------------------------
        public static List<Produit> ChargerProduits()
        {
            List<Produit> liste = new List<Produit>();
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand("SELECT * FROM Produits ORDER BY Id", cn))
            {
                cn.Open();
                using (SqliteDataReader r = cmd.ExecuteReader())
                {
                    // Colonnes facultatives (voir le script SQL)
                    bool aCategorie = ColonneExiste(r, "Categorie");
                    bool aImage = ColonneExiste(r, "Image");

                    while (r.Read())
                    {
                        liste.Add(new Produit
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            CodeBarre = Convert.ToString(r["CodeBarre"]).Trim(),
                            Nom = Convert.ToString(r["Nom"]).Trim(),
                            Prix = Base.Argent(r["Prix"]),
                            Stock = Convert.ToInt32(r["Stock"]),
                            QuantitePromo = r["QuantitePromo"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["QuantitePromo"]),
                            PrixPromo = r["PrixPromo"] == DBNull.Value ? (decimal?)null : Base.Argent(r["PrixPromo"]),
                            Taxable = Convert.ToBoolean(r["Taxable"]),
                            Categorie = aCategorie && r["Categorie"] != DBNull.Value ? Convert.ToString(r["Categorie"]) : null,
                            FichierImage = aImage && r["Image"] != DBNull.Value ? Convert.ToString(r["Image"]) : null
                        });
                    }
                }
            }
            foreach (Produit p in liste) Completer(p);
            return liste;
        }

        private static bool ColonneExiste(SqliteDataReader r, string nom)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), nom, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // ------------------------------------------------------------------
        // Gestion des produits (écran « Gestion des produits »)
        // ------------------------------------------------------------------
        public static void EnregistrerProduit(Produit p)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                string sql = p.Id == 0
                    ? "INSERT INTO Produits (CodeBarre, Nom, Prix, Stock, QuantitePromo, PrixPromo, Taxable, Categorie, Image) " +
                      "VALUES (@code, @nom, @prix, @stock, @qpromo, @ppromo, @taxable, @cat, @image); SELECT last_insert_rowid();"
                    : "UPDATE Produits SET CodeBarre = @code, Nom = @nom, Prix = @prix, Stock = @stock, QuantitePromo = @qpromo, " +
                      "PrixPromo = @ppromo, Taxable = @taxable, Categorie = @cat, Image = @image WHERE Id = @id; SELECT @id;";
                using (SqliteCommand cmd = new SqliteCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@code", p.CodeBarre ?? "");
                    cmd.Parameters.AddWithValue("@nom", p.Nom);
                    cmd.Parameters.AddWithValue("@prix", p.Prix);
                    cmd.Parameters.AddWithValue("@stock", p.Stock);
                    cmd.Parameters.AddWithValue("@qpromo", p.QuantitePromo.HasValue ? (object)p.QuantitePromo.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@ppromo", p.PrixPromo.HasValue ? (object)p.PrixPromo.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@taxable", p.Taxable ? 1 : 0);
                    cmd.Parameters.AddWithValue("@cat", string.IsNullOrWhiteSpace(p.Categorie) ? (object)DBNull.Value : p.Categorie);
                    cmd.Parameters.AddWithValue("@image", string.IsNullOrWhiteSpace(p.FichierImage) ? (object)DBNull.Value : p.FichierImage);
                    cmd.Parameters.AddWithValue("@id", p.Id);
                    p.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // L'historique garde le nom du produit : on peut supprimer sans rien perdre
        public static void SupprimerProduit(int id)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand("DELETE FROM Produits WHERE Id = @id", cn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ------------------------------------------------------------------
        // Catégories
        // ------------------------------------------------------------------
        public static List<CategorieProduit> ChargerCategories()
        {
            List<CategorieProduit> liste = new List<CategorieProduit>();
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand("SELECT Id, Nom, Ordre, Emoji FROM Categories ORDER BY Ordre, Nom", cn))
            {
                cn.Open();
                using (SqliteDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        liste.Add(new CategorieProduit
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            Nom = Convert.ToString(r["Nom"]),
                            Ordre = Convert.ToInt32(r["Ordre"]),
                            Emoji = r["Emoji"] == DBNull.Value ? null : Convert.ToString(r["Emoji"])
                        });
            }
            return liste;
        }

        // Nombre de produits par catégorie (clé = nom de la catégorie)
        public static Dictionary<string, int> CompterProduitsParCategorie()
        {
            Dictionary<string, int> res = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand("SELECT Categorie, COUNT(*) AS Nb FROM Produits WHERE Categorie IS NOT NULL GROUP BY Categorie", cn))
            {
                cn.Open();
                using (SqliteDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        string nom = Convert.ToString(r["Categorie"]);
                        int nb;
                        res.TryGetValue(nom, out nb);
                        res[nom] = nb + Convert.ToInt32(r["Nb"]);
                    }
            }
            return res;
        }

        // Crée (Id = 0) ou modifie une catégorie. Un renommage suit dans les produits et les cartes fidélité.
        public static void EnregistrerCategorie(CategorieProduit c, string ancienNom)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                using (SqliteTransaction tr = cn.BeginTransaction())
                {
                    string sql = c.Id == 0
                        ? "INSERT INTO Categories (Nom, Ordre, Emoji) VALUES (@nom, (SELECT IFNULL(MAX(Ordre), 0) + 1 FROM Categories WHERE Ordre < 99), @emoji); SELECT last_insert_rowid();"
                        : "UPDATE Categories SET Nom = @nom, Emoji = @emoji WHERE Id = @id; SELECT @id;";
                    using (SqliteCommand cmd = new SqliteCommand(sql, cn, tr))
                    {
                        cmd.Parameters.AddWithValue("@nom", c.Nom);
                        cmd.Parameters.AddWithValue("@emoji", string.IsNullOrWhiteSpace(c.Emoji) ? (object)DBNull.Value : c.Emoji.Trim());
                        cmd.Parameters.AddWithValue("@id", c.Id);
                        c.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (!string.IsNullOrEmpty(ancienNom) && ancienNom != c.Nom)
                    {
                        using (SqliteCommand cmd = new SqliteCommand(
                            "UPDATE Produits SET Categorie = @nouveau WHERE Categorie = @ancien COLLATE NOCASE; " +
                            "UPDATE CartesFidelite SET Categorie = @nouveau WHERE Categorie = @ancien COLLATE NOCASE;", cn, tr))
                        {
                            cmd.Parameters.AddWithValue("@nouveau", c.Nom);
                            cmd.Parameters.AddWithValue("@ancien", ancienNom);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tr.Commit();
                }
            }
        }

        // Supprime une catégorie : ses produits vont dans « remplacement », ses cartes fidélité sont désactivées
        public static void SupprimerCategorie(CategorieProduit c, string remplacement)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                using (SqliteTransaction tr = cn.BeginTransaction())
                {
                    using (SqliteCommand cmd = new SqliteCommand(
                        "INSERT OR IGNORE INTO Categories (Nom, Ordre, Emoji) VALUES (@remp, 99, '🛒'); " +
                        "UPDATE Produits SET Categorie = @remp WHERE Categorie = @nom COLLATE NOCASE; " +
                        "UPDATE CartesFidelite SET Active = 0 WHERE Categorie = @nom COLLATE NOCASE; " +
                        "DELETE FROM Categories WHERE Id = @id;", cn, tr))
                    {
                        cmd.Parameters.AddWithValue("@remp", remplacement);
                        cmd.Parameters.AddWithValue("@nom", c.Nom);
                        cmd.Parameters.AddWithValue("@id", c.Id);
                        cmd.ExecuteNonQuery();
                    }
                    tr.Commit();
                }
            }
        }

        // Enregistre l'ordre des boutons (liste déjà triée)
        public static void EnregistrerOrdreCategories(IList<CategorieProduit> ordre)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                using (SqliteTransaction tr = cn.BeginTransaction())
                {
                    for (int i = 0; i < ordre.Count; i++)
                    {
                        ordre[i].Ordre = i + 1;
                        using (SqliteCommand cmd = new SqliteCommand("UPDATE Categories SET Ordre = @ordre WHERE Id = @id", cn, tr))
                        {
                            cmd.Parameters.AddWithValue("@ordre", ordre[i].Ordre);
                            cmd.Parameters.AddWithValue("@id", ordre[i].Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tr.Commit();
                }
            }
        }

        // ------------------------------------------------------------------
        // Regroupe "Latte - Petit / Moyen / Grand" en une seule carte "Latte"
        // ------------------------------------------------------------------
        public static List<Produit> RegrouperParTaille(List<Produit> tous)
        {
            List<Produit> cartes = new List<Produit>();
            Dictionary<string, Produit> groupes = new Dictionary<string, Produit>(StringComparer.OrdinalIgnoreCase);

            foreach (Produit p in tous)
            {
                string baseNom, taille;
                if (!SeparerTaille(p.Nom, out baseNom, out taille))
                {
                    cartes.Add(p);
                    continue;
                }

                p.Taille = taille;
                Produit groupe;
                if (!groupes.TryGetValue(baseNom, out groupe))
                {
                    groupe = new Produit
                    {
                        Nom = baseNom,
                        Categorie = p.Categorie,
                        FichierImage = p.FichierImage,
                        Emoji = p.Emoji,
                        Taxable = p.Taxable,
                        Variantes = new List<Produit>()
                    };
                    groupes[baseNom] = groupe;
                    cartes.Add(groupe);
                }
                if (string.IsNullOrEmpty(groupe.FichierImage)) groupe.FichierImage = p.FichierImage;
                groupe.Variantes.Add(p);
            }

            foreach (Produit g in groupes.Values)
                g.Variantes.Sort((a, b) => Array.IndexOf(OrdreTailles, a.Taille).CompareTo(Array.IndexOf(OrdreTailles, b.Taille)));

            return cartes;
        }

        private static bool SeparerTaille(string nom, out string baseNom, out string taille)
        {
            baseNom = nom;
            taille = null;
            int i = nom.LastIndexOf(" - ", StringComparison.Ordinal);
            if (i <= 0) return false;

            string fin = nom.Substring(i + 3).Trim();
            foreach (string t in OrdreTailles)
            {
                if (string.Equals(fin, t, StringComparison.OrdinalIgnoreCase))
                {
                    baseNom = nom.Substring(0, i).Trim();
                    taille = t;
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Catégorie et emoji devinés à partir du nom (si la colonne est vide)
        // ------------------------------------------------------------------
        private static void Completer(Produit p)
        {
            string n = p.Nom.ToLowerInvariant();
            bool estEau = n == "eau" || n.StartsWith("eau ");

            if (string.IsNullOrEmpty(p.Categorie) && Catalogue.Categories.Count > 0)
            {
                if (Contient(n, "café", "cafe", "latte", "cappuccino", "moccachino", "mocca", "espresso", "americano"))
                    p.Categorie = "Cafés";
                else if (Contient(n, "muffin", "croissant", "biscuit", "beigne", "gâteau", "gateau", "brownie"))
                    p.Categorie = "Pâtisseries";
                else if (Contient(n, "sandwich", "wrap", "panini", "sous-marin"))
                    p.Categorie = "Sandwichs";
                else if (estEau || Contient(n, "cola", "sprite", "pop", "jus", "boisson", "chocolat", "thé", "liqueur", "red bull"))
                    p.Categorie = "Boissons";
                else
                    p.Categorie = "Autres";

                // Catégorie devinée mais supprimée par le client : on ne l'invente pas
                if (!Catalogue.Existe(p.Categorie)) p.Categorie = Catalogue.Existe("Autres") ? "Autres" : null;
            }

            if (string.IsNullOrEmpty(p.Emoji))
            {
                if (Contient(n, "chocolat")) p.Emoji = "🍫";
                else if (estEau) p.Emoji = "💧";
                else if (Contient(n, "chips")) p.Emoji = "🥔";
                else if (Contient(n, "loterie", "lotto", "gratteux")) p.Emoji = "🎟";
                else if (Contient(n, "pizza")) p.Emoji = "🍕";
                else if (Contient(n, "muffin")) p.Emoji = "🧁";
                else p.Emoji = Catalogue.EmojiDe(p.Categorie) ?? "🛒";   // emoji choisi pour la catégorie
            }
        }

        private static bool Contient(string texte, params string[] mots)
        {
            foreach (string m in mots) if (texte.Contains(m)) return true;
            return false;
        }

        // ------------------------------------------------------------------
        // Clients (carte de fidélité)
        // ------------------------------------------------------------------
        public static Client ChercherClient(string telephone)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                Client client;
                using (SqliteCommand cmd = new SqliteCommand("SELECT Id, Prenom, Nom, Telephone FROM Clients WHERE Telephone = @tel", cn))
                {
                    cmd.Parameters.AddWithValue("@tel", telephone);
                    using (SqliteDataReader r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        client = new Client
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            Prenom = Convert.ToString(r["Prenom"]),
                            Nom = r["Nom"] == DBNull.Value ? "" : Convert.ToString(r["Nom"]),
                            Telephone = Convert.ToString(r["Telephone"])
                        };
                    }
                }

                // Ses tampons sur chaque carte
                using (SqliteCommand cmd = new SqliteCommand("SELECT CarteId, Tampons FROM TamponsClients WHERE ClientId = @id", cn))
                {
                    cmd.Parameters.AddWithValue("@id", client.Id);
                    using (SqliteDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            client.TamponsParCarte[Convert.ToInt32(r["CarteId"])] = Convert.ToInt32(r["Tampons"]);
                }
                return client;
            }
        }

        public static Client CreerClient(string prenom, string nom, string telephone)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand(
                "INSERT INTO Clients (Prenom, Nom, Telephone) VALUES (@prenom, @nom, @tel); SELECT last_insert_rowid();", cn))
            {
                cmd.Parameters.AddWithValue("@prenom", prenom);
                cmd.Parameters.AddWithValue("@nom", string.IsNullOrWhiteSpace(nom) ? (object)DBNull.Value : nom);
                cmd.Parameters.AddWithValue("@tel", telephone);
                cn.Open();
                int id = Convert.ToInt32(cmd.ExecuteScalar());
                return new Client { Id = id, Prenom = prenom, Nom = nom, Telephone = telephone };
            }
        }

        // ------------------------------------------------------------------
        // Cartes fidélité
        // ------------------------------------------------------------------
        public static List<CarteFidelite> ChargerCartes()
        {
            List<CarteFidelite> cartes = new List<CarteFidelite>();
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            using (SqliteCommand cmd = new SqliteCommand("SELECT Id, Nom, Categorie, TamponsRequis, Active FROM CartesFidelite ORDER BY Id", cn))
            {
                cn.Open();
                using (SqliteDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        cartes.Add(new CarteFidelite
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            Nom = Convert.ToString(r["Nom"]),
                            Categorie = Convert.ToString(r["Categorie"]),
                            TamponsRequis = Convert.ToInt32(r["TamponsRequis"]),
                            Active = Convert.ToBoolean(r["Active"])
                        });
            }
            return cartes;
        }

        // Crée (Id = 0) ou modifie une carte
        public static void EnregistrerCarte(CarteFidelite c)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                string sql = c.Id == 0
                    ? "INSERT INTO CartesFidelite (Nom, Categorie, TamponsRequis, Active) VALUES (@nom, @cat, @requis, @active); SELECT last_insert_rowid();"
                    : "UPDATE CartesFidelite SET Nom = @nom, Categorie = @cat, TamponsRequis = @requis, Active = @active WHERE Id = @id; SELECT @id;";
                using (SqliteCommand cmd = new SqliteCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@nom", c.Nom);
                    cmd.Parameters.AddWithValue("@cat", c.Categorie);
                    cmd.Parameters.AddWithValue("@requis", c.TamponsRequis);
                    cmd.Parameters.AddWithValue("@active", c.Active);
                    cmd.Parameters.AddWithValue("@id", c.Id);
                    c.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // ------------------------------------------------------------------
        // Enregistre une vente : Paiements + DetailsVente + stock + tampons
        // Tout se fait dans une transaction : si une étape échoue, rien n'est enregistré.
        // ------------------------------------------------------------------
        public static int EnregistrerVente(Vente vente, string statut)
        {
            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                using (SqliteTransaction tr = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Le paiement (avec le client s'il y en a un)
                        int paiementId;
                        string sqlPaiement =
                            "INSERT INTO Paiements (Montant, DatePaiement, Statut, NumeroTransaction, ClientId, " +
                            "ModePaiement, MarqueCarte, TypeCarte, Derniers4) " +
                            "VALUES (@montant, @date, @statut, @numero, @client, " +
                            "@mode, @marque, @type, @derniers4); SELECT last_insert_rowid();";
                        using (SqliteCommand cmd = new SqliteCommand(sqlPaiement, cn, tr))
                        {
                            cmd.Parameters.AddWithValue("@montant", vente.MontantPaye);
                            cmd.Parameters.AddWithValue("@date", Base.Date(vente.Date));
                            cmd.Parameters.AddWithValue("@statut", statut);
                            cmd.Parameters.AddWithValue("@numero", vente.NumeroTransaction ?? "");
                            cmd.Parameters.AddWithValue("@client", vente.Client != null ? (object)vente.Client.Id : DBNull.Value);
                            cmd.Parameters.AddWithValue("@mode", (object)vente.ModePaiement ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@marque", vente.Carte != null && vente.Carte.Marque != null ? (object)vente.Carte.Marque : DBNull.Value);
                            cmd.Parameters.AddWithValue("@type", vente.Carte != null && vente.Carte.Financement != null ? (object)vente.Carte.Financement : DBNull.Value);
                            cmd.Parameters.AddWithValue("@derniers4", vente.Carte != null && vente.Carte.Derniers4 != null ? (object)vente.Carte.Derniers4 : DBNull.Value);
                            paiementId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Les produits vendus + le stock
                        foreach (LignePanier l in vente.Lignes)
                        {
                            using (SqliteCommand cmd = new SqliteCommand(
                                "INSERT INTO DetailsVente (PaiementId, ProduitId, NomProduit, Quantite, PrixUnitaire, Total) " +
                                "VALUES (@paiement, @produit, @nom, @qte, @prix, @total); " +
                                "UPDATE Produits SET Stock = Stock - @qte WHERE Id = @produit AND Stock < 9999;", cn, tr))
                            {
                                cmd.Parameters.AddWithValue("@paiement", paiementId);
                                cmd.Parameters.AddWithValue("@produit", l.Produit.Id);
                                cmd.Parameters.AddWithValue("@nom", l.Nom);
                                cmd.Parameters.AddWithValue("@qte", l.Quantite);
                                cmd.Parameters.AddWithValue("@prix", l.Prix);
                                cmd.Parameters.AddWithValue("@total", l.Total);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 3. Les produits offerts (fidélité) : une ligne négative par récompense
                        //    et 4. les tampons du client sur chaque carte
                        if (vente.ResultatsFidelite != null)
                        {
                            foreach (ResultatCarte r in vente.ResultatsFidelite)
                            {
                                if (r.ProduitOffert != null && r.Rabais > 0)
                                {
                                    string nom = "Gratuit (" + r.Carte.Nom + ") : " + r.ProduitOffert.Nom;
                                    if (nom.Length > 100) nom = nom.Substring(0, 100);
                                    using (SqliteCommand cmd = new SqliteCommand(
                                        "INSERT INTO DetailsVente (PaiementId, ProduitId, NomProduit, Quantite, PrixUnitaire, Total) " +
                                        "VALUES (@paiement, @produit, @nom, 1, @prix, @prix)", cn, tr))
                                    {
                                        cmd.Parameters.AddWithValue("@paiement", paiementId);
                                        cmd.Parameters.AddWithValue("@produit", r.ProduitOffert.Id);
                                        cmd.Parameters.AddWithValue("@nom", nom);
                                        cmd.Parameters.AddWithValue("@prix", -r.Rabais);
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                if (vente.Client != null && (r.Gagnes != 0 || r.Utilises != 0))
                                {
                                    using (SqliteCommand cmd = new SqliteCommand(
                                        "INSERT INTO TamponsClients (ClientId, CarteId, Tampons) VALUES (@client, @carte, @delta) " +
                                        "ON CONFLICT (ClientId, CarteId) DO UPDATE SET Tampons = Tampons + @delta; " +
                                        "UPDATE Clients SET CafesGratuits = CafesGratuits + @gratuit WHERE Id = @client;", cn, tr))
                                    {
                                        cmd.Parameters.AddWithValue("@delta", r.Gagnes - r.Utilises);
                                        cmd.Parameters.AddWithValue("@client", vente.Client.Id);
                                        cmd.Parameters.AddWithValue("@carte", r.Carte.Id);
                                        cmd.Parameters.AddWithValue("@gratuit", r.Utilises > 0 ? 1 : 0);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tr.Commit();
                        return paiementId;
                    }
                    catch
                    {
                        tr.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
