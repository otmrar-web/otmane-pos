using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace OtmanePOS
{
    // La base de données est un simple fichier SQLite, créé automatiquement au premier lancement.
    // Rien à installer chez le client : pas de SQL Server, pas de restauration.
    public static class Base
    {
        // %LOCALAPPDATA%\OtmanePOS\ (ex. C:\Users\Nom\AppData\Local\OtmanePOS\)
        public static string Dossier { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OtmanePOS");

        public static string Fichier { get { return Path.Combine(Dossier, "otmanepos.db"); } }

        // Photos des produits ajoutées depuis « Gestion des produits »
        public static string DossierImages { get { return Path.Combine(Dossier, "Images"); } }

        public static string ChaineConnexion
        {
            get { return new SqliteConnectionStringBuilder { DataSource = Fichier, ForeignKeys = true }.ToString(); }
        }

        private const int VersionSchema = 2;

        // ------------------------------------------------------------------
        // À appeler une fois au démarrage : crée le fichier et les tables si besoin
        // ------------------------------------------------------------------
        public static void Initialiser()
        {
            Directory.CreateDirectory(Dossier);
            Directory.CreateDirectory(DossierImages);

            using (SqliteConnection cn = new SqliteConnection(ChaineConnexion))
            {
                cn.Open();
                Executer(cn, null, "PRAGMA journal_mode = WAL;");   // plus rapide et plus sûr en cas de coupure

                int version = Convert.ToInt32(Scalaire(cn, "PRAGMA user_version;"));
                if (version < 1)
                {
                    using (SqliteTransaction tr = cn.BeginTransaction())
                    {
                        CreerTables(cn, tr);
                        tr.Commit();
                    }
                    Executer(cn, null, "PRAGMA user_version = 1;");
                    version = 1;
                }
                if (version < 2)
                {
                    // Version 2 : catégories modifiables par le client
                    using (SqliteTransaction tr = cn.BeginTransaction())
                    {
                        CreerCategories(cn, tr);
                        tr.Commit();
                    }
                    Executer(cn, null, "PRAGMA user_version = 2;");
                }
                // Les futures versions ajouteront ici : if (version < 3) { ... }
            }
        }

        private static void CreerTables(SqliteConnection cn, SqliteTransaction tr)
        {
            Executer(cn, tr, @"
CREATE TABLE IF NOT EXISTS Produits (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    CodeBarre     TEXT    NOT NULL DEFAULT '',
    Nom           TEXT    NOT NULL,
    Prix          NUMERIC NOT NULL DEFAULT 0,
    Stock         INTEGER NOT NULL DEFAULT 9999,     -- 9999 = illimité
    QuantitePromo INTEGER NULL,
    PrixPromo     NUMERIC NULL,
    Taxable       INTEGER NOT NULL DEFAULT 1,
    Categorie     TEXT    NULL,
    Image         TEXT    NULL
);

CREATE TABLE IF NOT EXISTS Clients (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Prenom          TEXT    NOT NULL,
    Nom             TEXT    NULL,
    Telephone       TEXT    NOT NULL UNIQUE,
    Tampons         INTEGER NOT NULL DEFAULT 0,      -- ancienne carte unique (non utilisé)
    CafesGratuits   INTEGER NOT NULL DEFAULT 0,      -- nombre de gratuits reçus
    DateInscription TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS Paiements (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    Montant           NUMERIC NOT NULL,
    DatePaiement      TEXT    NOT NULL,              -- aaaa-mm-jj hh:mm:ss
    Statut            TEXT    NOT NULL,
    NumeroTransaction TEXT    NULL,
    ClientId          INTEGER NULL REFERENCES Clients (Id),
    ModePaiement      TEXT    NULL,
    MarqueCarte       TEXT    NULL,
    TypeCarte         TEXT    NULL,
    Derniers4         TEXT    NULL
);
CREATE INDEX IF NOT EXISTS IX_Paiements_Date ON Paiements (DatePaiement);

CREATE TABLE IF NOT EXISTS DetailsVente (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    PaiementId   INTEGER NOT NULL REFERENCES Paiements (Id),
    ProduitId    INTEGER NOT NULL,                   -- pas de clé étrangère : on peut supprimer un produit sans perdre l'historique
    NomProduit   TEXT    NOT NULL,
    Quantite     INTEGER NOT NULL,
    PrixUnitaire NUMERIC NOT NULL,
    Total        NUMERIC NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_DetailsVente_Paiement ON DetailsVente (PaiementId);

CREATE TABLE IF NOT EXISTS CartesFidelite (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nom           TEXT    NOT NULL,
    Categorie     TEXT    NOT NULL,
    TamponsRequis INTEGER NOT NULL DEFAULT 9,
    Active        INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS TamponsClients (
    ClientId INTEGER NOT NULL REFERENCES Clients (Id),
    CarteId  INTEGER NOT NULL REFERENCES CartesFidelite (Id),
    Tampons  INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (ClientId, CarteId)
);

CREATE TABLE IF NOT EXISTS Parametres (
    Cle    TEXT NOT NULL PRIMARY KEY,
    Valeur TEXT NULL
);

-- Valeurs de départ
INSERT OR IGNORE INTO Parametres (Cle, Valeur) VALUES
    ('NomCommerce', 'Mon commerce'),
    ('MessageTicket', 'Merci et à bientôt !'),
    ('TauxTPS', '5'),
    ('TauxTVQ', '9.975'),
    ('ArrondiComptant', '1'),
    ('FideliteActive', '1'),
    ('CafeOffert', 'PlusCher'),
    ('ReaderId', '');

INSERT INTO CartesFidelite (Nom, Categorie, TamponsRequis)
SELECT 'Café', 'Cafés', 9 WHERE NOT EXISTS (SELECT 1 FROM CartesFidelite);
");
        }

        private static void CreerCategories(SqliteConnection cn, SqliteTransaction tr)
        {
            Executer(cn, tr, @"
CREATE TABLE IF NOT EXISTS Categories (
    Id    INTEGER PRIMARY KEY AUTOINCREMENT,
    Nom   TEXT    NOT NULL UNIQUE COLLATE NOCASE,
    Ordre INTEGER NOT NULL DEFAULT 0,
    Emoji TEXT    NULL                -- affiché pour les produits sans photo
);

INSERT OR IGNORE INTO Categories (Nom, Ordre, Emoji) VALUES
    ('Cafés', 1, '☕'),
    ('Boissons', 2, '🥤'),
    ('Sandwichs', 3, '🥪'),
    ('Pâtisseries', 4, '🧁'),
    ('Autres', 99, '🛒');

-- Les catégories déjà utilisées par des produits ou des cartes fidélité
INSERT OR IGNORE INTO Categories (Nom, Ordre)
SELECT DISTINCT Categorie, 50 FROM Produits WHERE Categorie IS NOT NULL AND trim(Categorie) <> '';
INSERT OR IGNORE INTO Categories (Nom, Ordre)
SELECT DISTINCT Categorie, 50 FROM CartesFidelite WHERE trim(Categorie) <> '';
");
        }

        // ------------------------------------------------------------------
        // Sauvegarde : copie cohérente de la base, même pendant l'utilisation
        // ------------------------------------------------------------------
        public static void Sauvegarder(string destination)
        {
            if (File.Exists(destination)) File.Delete(destination);
            using (SqliteConnection source = new SqliteConnection(ChaineConnexion))
            using (SqliteConnection copie = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination }.ToString()))
            {
                source.Open();
                copie.Open();
                source.BackupDatabase(copie);
            }
            SqliteConnection.ClearAllPools();   // libère le fichier copié
        }

        // ------------------------------------------------------------------
        // Outils
        // ------------------------------------------------------------------
        // Les dates sont enregistrées en texte triable : 2026-09-27 14:05:00
        public static string Date(DateTime d)
        {
            return d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static DateTime LireDate(object valeur)
        {
            if (valeur is DateTime) return (DateTime)valeur;
            return DateTime.Parse(Convert.ToString(valeur, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        // SQLite calcule en nombres à virgule : on arrondit au cent en relisant
        public static decimal Argent(object valeur)
        {
            if (valeur == null || valeur == DBNull.Value) return 0m;
            return Math.Round(Convert.ToDecimal(valeur, CultureInfo.InvariantCulture), 2, MidpointRounding.AwayFromZero);
        }

        private static void Executer(SqliteConnection cn, SqliteTransaction tr, string sql)
        {
            using (SqliteCommand cmd = new SqliteCommand(sql, cn, tr)) cmd.ExecuteNonQuery();
        }

        private static object Scalaire(SqliteConnection cn, string sql)
        {
            using (SqliteCommand cmd = new SqliteCommand(sql, cn)) return cmd.ExecuteScalar();
        }
    }
}
