using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace OtmanePOS
{
    // Paramètres du commerce, lus dans la table Parametres au démarrage.
    // Si la table n'existe pas encore, les valeurs par défaut ci-dessous sont utilisées.
    public static class Parametres
    {
        // ---- Commerce (ticket) ----
        public static string NomCommerce { get; set; } = "Mon commerce";
        public static string Adresse { get; set; } = "";
        public static string Ville { get; set; } = "";
        public static string Telephone { get; set; } = "";
        public static string NumeroTPS { get; set; } = "";
        public static string NumeroTVQ { get; set; } = "";
        public static string MessageTicket { get; set; } = "Merci et à bientôt !";

        // ---- Taxes (en pourcentage : 5 = 5 %) ----
        public static decimal TauxTPSPourcent { get; set; } = 5m;
        public static decimal TauxTVQPourcent { get; set; } = 9.975m;
        public static bool ArrondiComptant { get; set; } = true;

        // ---- Fidélité ----
        public static bool FideliteActive { get; set; } = true;
        public static int TamponsPourGratuit { get; set; } = 9;
        public static string CategorieFidelite { get; set; } = "Cafés";
        public static bool OffrirLePlusCher { get; set; } = true;

        // ---- Paiements ----
        public static string ReaderId { get; set; } = "";

        // Clé secrète Stripe du commerce : en clair seulement en mémoire,
        // chiffrée avec Windows (DPAPI) dans la base
        public static string StripeCle { get; set; } = "";

        // ---- Calculés ----
        public static decimal TauxTPS { get { return TauxTPSPourcent / 100m; } }
        public static decimal TauxTVQ { get { return TauxTVQPourcent / 100m; } }
        public static string LibelleTPS { get { return "TPS (" + Pourcent(TauxTPSPourcent) + ")"; } }
        public static string LibelleTVQ { get { return "TVQ (" + Pourcent(TauxTVQPourcent) + ")"; } }

        public static bool TableTrouvee { get; private set; }

        public static string Pourcent(decimal p)
        {
            return p.ToString("0.###", Theme.Culture) + " %";   // 9,975 %
        }

        // ------------------------------------------------------------------
        public static void Charger()
        {
            Dictionary<string, string> v = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
                using (SqliteCommand cmd = new SqliteCommand("SELECT Cle, Valeur FROM Parametres", cn))
                {
                    cn.Open();
                    using (SqliteDataReader rd = cmd.ExecuteReader())
                        while (rd.Read())
                            v[Convert.ToString(rd["Cle"])] = rd["Valeur"] == DBNull.Value ? "" : Convert.ToString(rd["Valeur"]);
                }
                TableTrouvee = true;
            }
            catch
            {
                TableTrouvee = false;   // table absente : on garde les valeurs par défaut
                return;
            }

            NomCommerce = Texte(v, "NomCommerce", NomCommerce);
            Adresse = Texte(v, "Adresse", Adresse);
            Ville = Texte(v, "Ville", Ville);
            Telephone = Texte(v, "Telephone", Telephone);
            NumeroTPS = Texte(v, "NumeroTPS", NumeroTPS);
            NumeroTVQ = Texte(v, "NumeroTVQ", NumeroTVQ);
            MessageTicket = Texte(v, "MessageTicket", MessageTicket);
            TauxTPSPourcent = Nombre(v, "TauxTPS", TauxTPSPourcent);
            TauxTVQPourcent = Nombre(v, "TauxTVQ", TauxTVQPourcent);
            ArrondiComptant = Booleen(v, "ArrondiComptant", ArrondiComptant);
            FideliteActive = Booleen(v, "FideliteActive", FideliteActive);
            TamponsPourGratuit = (int)Nombre(v, "TamponsPourGratuit", TamponsPourGratuit);
            CategorieFidelite = Texte(v, "CategorieFidelite", CategorieFidelite);
            OffrirLePlusCher = Texte(v, "CafeOffert", OffrirLePlusCher ? "PlusCher" : "MoinsCher") != "MoinsCher";
            ReaderId = Texte(v, "ReaderId", ReaderId);
            StripeCle = Dechiffrer(Texte(v, "StripeCle", ""));

            if (TamponsPourGratuit < 1) TamponsPourGratuit = 9;
        }

        public static void Enregistrer()
        {
            Dictionary<string, string> v = new Dictionary<string, string>
            {
                { "NomCommerce", NomCommerce },
                { "Adresse", Adresse },
                { "Ville", Ville },
                { "Telephone", Telephone },
                { "NumeroTPS", NumeroTPS },
                { "NumeroTVQ", NumeroTVQ },
                { "MessageTicket", MessageTicket },
                { "TauxTPS", TauxTPSPourcent.ToString(CultureInfo.InvariantCulture) },
                { "TauxTVQ", TauxTVQPourcent.ToString(CultureInfo.InvariantCulture) },
                { "ArrondiComptant", ArrondiComptant ? "1" : "0" },
                { "FideliteActive", FideliteActive ? "1" : "0" },
                { "TamponsPourGratuit", TamponsPourGratuit.ToString(CultureInfo.InvariantCulture) },
                { "CategorieFidelite", CategorieFidelite },
                { "CafeOffert", OffrirLePlusCher ? "PlusCher" : "MoinsCher" },
                { "ReaderId", ReaderId },
                { "StripeCle", Chiffrer(StripeCle) }
            };

            using (SqliteConnection cn = new SqliteConnection(Donnees.ChaineConnexion))
            {
                cn.Open();
                using (SqliteTransaction tr = cn.BeginTransaction())
                {
                    foreach (KeyValuePair<string, string> p in v)
                    {
                        using (SqliteCommand cmd = new SqliteCommand(
                            "INSERT INTO Parametres (Cle, Valeur) VALUES (@cle, @valeur) " +
                            "ON CONFLICT (Cle) DO UPDATE SET Valeur = excluded.Valeur;", cn, tr))
                        {
                            cmd.Parameters.AddWithValue("@cle", p.Key);
                            cmd.Parameters.AddWithValue("@valeur", p.Value ?? "");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tr.Commit();
                }
            }
            TableTrouvee = true;
        }

        // ------------------------------------------------------------------
        // Chiffrement de la clé Stripe (lisible seulement par ce compte Windows)
        // ------------------------------------------------------------------
        private static readonly byte[] Entropie = Encoding.UTF8.GetBytes("OtmanePOS.Stripe.v1");

        private static string Chiffrer(string texte)
        {
            if (string.IsNullOrEmpty(texte)) return "";
            byte[] donnees = ProtectedData.Protect(Encoding.UTF8.GetBytes(texte), Entropie, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(donnees);
        }

        private static string Dechiffrer(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return "";
            try
            {
                byte[] donnees = ProtectedData.Unprotect(Convert.FromBase64String(base64), Entropie, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(donnees);
            }
            catch
            {
                return "";   // autre compte Windows ou base copiée d'un autre ordinateur : la clé doit être entrée de nouveau
            }
        }

        private static string Texte(Dictionary<string, string> v, string cle, string defaut)
        {
            string s;
            return v.TryGetValue(cle, out s) && s != null ? s : defaut;
        }

        private static decimal Nombre(Dictionary<string, string> v, string cle, decimal defaut)
        {
            string s;
            decimal d;
            if (v.TryGetValue(cle, out s) && decimal.TryParse((s ?? "").Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out d))
                return d;
            return defaut;
        }

        private static bool Booleen(Dictionary<string, string> v, string cle, bool defaut)
        {
            string s;
            if (!v.TryGetValue(cle, out s)) return defaut;
            return s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
