using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OtmanePOS
{
    // Une ligne de la table Produits (ou une carte qui regroupe les tailles)
    public class Produit
    {
        public int Id { get; set; }
        public string CodeBarre { get; set; }
        public string Nom { get; set; }
        public decimal Prix { get; set; }
        public int Stock { get; set; }
        public int? QuantitePromo { get; set; }
        public decimal? PrixPromo { get; set; }
        public bool Taxable { get; set; }

        // Affichage
        public string Categorie { get; set; }
        public string FichierImage { get; set; }   // ex. "latte.png" dans le dossier Images
        public string Emoji { get; set; }          // affiché si l'image est absente

        // Tailles : "Latte - Petit", "Latte - Moyen"... regroupées sur une seule carte "Latte"
        public string Taille { get; set; }
        public List<Produit> Variantes { get; set; }

        public bool ATailles
        {
            get { return Variantes != null && Variantes.Count > 0; }
        }

        // 9999 dans la colonne Stock = stock illimité
        public bool StockIllimite
        {
            get { return Stock >= 9999; }
        }

        public string PrixAffiche
        {
            get { return ATailles ? "dès " + Theme.Argent(Variantes.Min(v => v.Prix)) : Theme.Argent(Prix); }
        }
    }

    public class LignePanier
    {
        public Produit Produit { get; set; }
        public int Quantite { get; set; }
        public Image Image { get; set; }
        public string Emoji { get; set; }

        public string Nom { get { return Produit.Nom; } }
        public decimal Prix { get { return Produit.Prix; } }
        public bool Taxable { get { return Produit.Taxable; } }

        // Promo : chaque groupe de QuantitePromo articles coûte PrixPromo
        // (ex. QuantitePromo = 3, PrixPromo = 5,00 → 3 pour 5,00 $)
        public decimal Total
        {
            get
            {
                if (Produit.QuantitePromo.HasValue && Produit.PrixPromo.HasValue && Produit.QuantitePromo.Value > 1)
                {
                    int q = Produit.QuantitePromo.Value;
                    return (Quantite / q) * Produit.PrixPromo.Value + (Quantite % q) * Prix;
                }
                return Prix * Quantite;
            }
        }
    }

    // Une vente terminée (ou en attente de paiement Stripe)
    public class Vente
    {
        public int Id { get; set; }                  // Id dans Paiements, après l'enregistrement
        public List<LignePanier> Lignes { get; set; }
        public decimal SousTotal { get; set; }
        public decimal TPS { get; set; }
        public decimal TVQ { get; set; }
        public decimal Total { get; set; }
        public string ModePaiement { get; set; }
        public string NumeroTransaction { get; set; }
        public System.DateTime Date { get; set; }

        // Montant réellement payé (au comptant : total arrondi aux 5 cents)
        public decimal MontantPaye { get; set; }
        public decimal Recu { get; set; }    // comptant seulement
        public decimal Rendu { get; set; }   // comptant seulement
        public bool EstComptant { get { return ModePaiement == "Comptant"; } }

        // Fidélité
        public Client Client { get; set; }
        public decimal Rabais { get; set; }          // total des produits offerts
        public List<ResultatCarte> ResultatsFidelite { get; set; }

        // Carte utilisée (Stripe / TPE), null pour le comptant
        public InfoCarte Carte { get; set; }
    }

    public class Client
    {
        public int Id { get; set; }
        public string Prenom { get; set; }
        public string Nom { get; set; }
        public string Telephone { get; set; }

        // Tampons par carte : clé = Id de la carte
        public Dictionary<int, int> TamponsParCarte { get; set; } = new Dictionary<int, int>();

        public int TamponsSur(int carteId)
        {
            int n;
            return TamponsParCarte.TryGetValue(carteId, out n) ? n : 0;
        }

        public string NomComplet
        {
            get { return string.IsNullOrWhiteSpace(Nom) ? Prenom : Prenom + " " + Nom; }
        }
    }

    // Règles de la carte de tampons : modifie-les ici
    // Une carte fidélité : ex. « Café », catégorie « Cafés », 9 tampons
    public class CarteFidelite
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Categorie { get; set; }
        public int TamponsRequis { get; set; }
        public bool Active { get; set; }

        public bool Accepte(Produit p)
        {
            return p != null && Active && string.Equals(p.Categorie, Categorie, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    // Le calcul d'une carte pour le panier en cours
    public class ResultatCarte
    {
        public CarteFidelite Carte { get; set; }
        public int Avant { get; set; }                  // tampons avant la vente
        public int Gagnes { get; set; }
        public int Utilises { get; set; }
        public Produit ProduitOffert { get; set; }
        public decimal Rabais { get; set; }
        public bool RabaisTaxable { get; set; }
        public bool RecompenseDisponible { get; set; }  // assez de tampons pour un gratuit
        public bool ProduitDansPanier { get; set; }

        public int Apres { get { return Avant + Gagnes - Utilises; } }
    }

    public static class Fidelite
    {
        public static List<CarteFidelite> Cartes { get; private set; } = new List<CarteFidelite>();
        public static bool TableTrouvee { get; private set; }

        public static IEnumerable<CarteFidelite> CartesActives
        {
            get { return Cartes.Where(c => c.Active); }
        }

        // Programme activé dans Paramètres ET au moins une carte active
        public static bool Actif
        {
            get { return Parametres.FideliteActive && CartesActives.Any(); }
        }

        public static void Charger()
        {
            try
            {
                Cartes = Donnees.ChargerCartes();
                TableTrouvee = true;
            }
            catch
            {
                Cartes = new List<CarteFidelite>();
                TableTrouvee = false;
            }
        }

        // ●●●●○○○○○
        public static string Pastilles(int tampons, int requis)
        {
            int pleins = System.Math.Max(0, System.Math.Min(tampons, requis));
            return new string('●', pleins) + new string('○', requis - pleins);
        }

        public static string NormaliserTelephone(string texte)
        {
            string chiffres = new string((texte ?? "").Where(char.IsDigit).ToArray());
            if (chiffres.Length == 11 && chiffres.StartsWith("1")) chiffres = chiffres.Substring(1);
            return chiffres;
        }

        public static string FormaterTelephone(string t)
        {
            if (t == null || t.Length != 10) return t;
            return "(" + t.Substring(0, 3) + ") " + t.Substring(3, 3) + "-" + t.Substring(6);
        }

    }

    // Marque et type de la carte, lus chez Stripe après le paiement
    public class InfoCarte
    {
        public string Marque { get; set; }        // visa, mastercard, amex, interac...
        public string Financement { get; set; }   // credit, debit, prepaid
        public string Derniers4 { get; set; }

        public static string NomMarque(string marque)
        {
            switch ((marque ?? "").ToLowerInvariant())
            {
                case "visa": return "Visa";
                case "mastercard": return "Mastercard";
                case "amex": return "American Express";
                case "interac": return "Interac";
                case "discover": return "Discover";
                case "": return "Carte";
                default: return char.ToUpper(marque[0]) + marque.Substring(1);
            }
        }

        // « Visa », « Visa Débit », « Débit (Interac) »...
        public string Libelle
        {
            get
            {
                string m = (Marque ?? "").ToLowerInvariant();
                if (m == "interac") return "Débit (Interac)";
                string nom = NomMarque(Marque);
                if (Financement == "debit") return nom + " Débit";
                if (Financement == "prepaid") return nom + " prépayée";
                return nom;
            }
        }

        public string LibelleComplet
        {
            get { return Libelle + (string.IsNullOrEmpty(Derniers4) ? "" : " **** " + Derniers4); }
        }
    }

    // Une catégorie de produits (bouton dans la caisse)
    public class CategorieProduit
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public int Ordre { get; set; }
        public string Emoji { get; set; }
    }

    // Les catégories chargées au démarrage (table Categories)
    public static class Catalogue
    {
        public static List<CategorieProduit> Categories { get; private set; } = new List<CategorieProduit>();

        public static IEnumerable<string> Noms
        {
            get { return Categories.Select(c => c.Nom); }
        }

        public static void Charger()
        {
            try { Categories = Donnees.ChargerCategories(); }
            catch { Categories = new List<CategorieProduit>(); }
        }

        public static CategorieProduit Trouver(string nom)
        {
            return Categories.FirstOrDefault(c => string.Equals(c.Nom, nom, System.StringComparison.OrdinalIgnoreCase));
        }

        public static bool Existe(string nom)
        {
            return Trouver(nom) != null;
        }

        public static string EmojiDe(string nom)
        {
            CategorieProduit c = Trouver(nom);
            return c != null && !string.IsNullOrWhiteSpace(c.Emoji) ? c.Emoji : null;
        }
    }
}
