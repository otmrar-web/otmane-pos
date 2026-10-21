using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;

namespace OtmanePOS
{
    // Tout le code Stripe est ici (repris de ton ancien Form1)
    public static class ServiceStripe
    {
        public static string ReaderId { get { return Parametres.ReaderId; } }   // réglable dans Paramètres
        private const string UrlLocale = "http://localhost:5050/";

        private static readonly HttpClient http = new HttpClient();
        private static HttpListener listener;

        // Clé du commerce (Paramètres → Stripe). La variable STRIPE_SECRET_KEY sert seulement en développement.
        public static bool Initialiser()
        {
            string cle = Parametres.StripeCle;
            if (string.IsNullOrWhiteSpace(cle)) cle = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
            StripeConfiguration.ApiKey = string.IsNullOrWhiteSpace(cle) ? null : cle.Trim();
            return EstConfigure;
        }

        public static bool EstConfigure
        {
            get { return !string.IsNullOrEmpty(StripeConfiguration.ApiKey); }
        }

        public static bool EstModeTest(string cle)
        {
            cle = cle ?? "";
            return cle.StartsWith("sk_test_") || cle.StartsWith("rk_test_");
        }

        public static bool ModeTest
        {
            get { return EstModeTest(StripeConfiguration.ApiKey); }
        }

        // =====================================================================
        // CONNEXION : vérifie une clé et liste les lecteurs du compte
        // =====================================================================
        public static async Task<InfoCompteStripe> TesterConnexionAsync(string cle)
        {
            cle = (cle ?? "").Trim();
            if (!(cle.StartsWith("sk_") || cle.StartsWith("rk_")))
                throw new Exception("Ce n'est pas une clé secrète Stripe : elle doit commencer par sk_ (ou rk_ pour une clé restreinte).");

            RequestOptions options = new RequestOptions { ApiKey = cle };
            InfoCompteStripe info = new InfoCompteStripe { ModeTest = EstModeTest(cle) };

            // Nom du compte (une clé restreinte peut ne pas y avoir droit : ce n'est pas grave)
            try
            {
                Account compte = await new AccountService().GetSelfAsync(options);
                info.Nom = compte.BusinessProfile != null && !string.IsNullOrEmpty(compte.BusinessProfile.Name)
                    ? compte.BusinessProfile.Name
                    : compte.Settings != null && compte.Settings.Dashboard != null && !string.IsNullOrEmpty(compte.Settings.Dashboard.DisplayName)
                        ? compte.Settings.Dashboard.DisplayName
                        : (compte.Email ?? compte.Id);
                info.Pays = compte.Country;
            }
            catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)   // 403 = clé restreinte sans ce droit
            {
                info.Nom = "(clé restreinte : nom du compte non disponible)";
            }

            // Lecteurs Stripe Terminal
            try
            {
                StripeList<Stripe.Terminal.Reader> lecteurs = await new Stripe.Terminal.ReaderService()
                    .ListAsync(new Stripe.Terminal.ReaderListOptions { Limit = 100 }, options);
                info.Lecteurs = lecteurs.Data.Select(r => new LecteurStripe
                {
                    Id = r.Id,
                    Nom = string.IsNullOrEmpty(r.Label) ? r.Id : r.Label,
                    Modele = r.DeviceType,
                    EnLigne = r.Status == "online"
                }).ToList();
            }
            catch (StripeException ex)
            {
                info.Lecteurs = new List<LecteurStripe>();
                info.ErreurLecteurs = ex.StripeError != null ? ex.StripeError.Message : ex.Message;
            }
            return info;
        }

        // =====================================================================
        // STRIPE CHECKOUT : page de paiement dans le navigateur
        // =====================================================================
        public static void OuvrirCheckout(decimal montant)
        {
            long cents = (long)Math.Round(montant * 100);
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = UrlLocale + "success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = UrlLocale + "cancel",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "cad",
                            UnitAmount = cents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Achat Otmane POS (taxes incluses)"
                            }
                        }
                    }
                }
            };

            Session session = new SessionService().Create(options);
            Process.Start(new ProcessStartInfo { FileName = session.Url, UseShellExecute = true });
        }

        // Petit serveur qui reçoit la réponse de Stripe après le paiement.
        // Démarré depuis le formulaire : les rappels s'exécutent sur le thread de l'interface.
        public static async void DemarrerServeurLocal(Action<string, decimal> surSucces, Action surAnnulation, Action<string> surErreur)
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(UrlLocale);
                listener.Start();
            }
            catch (Exception ex)
            {
                surErreur("Impossible de démarrer le serveur local Stripe (port 5050) : " + ex.Message);
                return;
            }

            while (listener != null && listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync(); }
                catch { break; }   // serveur arrêté

                string chemin = ctx.Request.Url.AbsolutePath;
                string message = "";
                string numeroTransaction = null;
                decimal montantPaye = 0;
                bool succes = false, annule = false;

                try
                {
                    if (chemin == "/success")
                    {
                        string sessionId = ctx.Request.QueryString["session_id"];
                        Session session = await new SessionService().GetAsync(sessionId);
                        numeroTransaction = session.PaymentIntentId;
                        montantPaye = (session.AmountTotal ?? 0) / 100m;
                        message = "Paiement réussi ! Tu peux fermer cette page et revenir à la caisse.";
                        succes = true;
                    }
                    else if (chemin == "/cancel")
                    {
                        message = "Paiement annulé. Tu peux fermer cette page.";
                        annule = true;
                    }
                }
                catch (Exception ex)
                {
                    message = "Erreur : " + ex.Message;
                }

                // On répond d'abord au navigateur...
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    ctx.Response.ContentType = "text/plain; charset=utf-8";
                    ctx.Response.ContentLength64 = buffer.Length;
                    await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    ctx.Response.OutputStream.Close();
                }
                catch { }

                // ... puis on prévient la caisse
                if (succes) surSucces(numeroTransaction, montantPaye);
                else if (annule) surAnnulation();
            }
        }

        public static void ArreterServeur()
        {
            try { if (listener != null) listener.Stop(); } catch { }
            listener = null;
        }

        // =====================================================================
        // TPE : Stripe Terminal (lecteur simulé)
        // Retourne le numéro de transaction (PaymentIntent) si le paiement réussit,
        // sinon lance une exception avec la raison.
        // =====================================================================
        // Retourne le numéro de transaction (PaymentIntent) si le paiement réussit,
        // sinon lance une exception avec la raison (OperationCanceledException si annulé).
        //  - Lecteur réel : on attend que le client présente sa carte.
        //  - Lecteur simulé (mode test) : demanderCarteTest() fournit un numéro de carte de test.
        public static async Task<string> PayerParTPEAsync(decimal montant, Func<string> demanderCarteTest,
                                                         Action<string> statut, CancellationToken annulation)
        {
            var lecteurs = new Stripe.Terminal.ReaderService();
            var paymentIntentService = new PaymentIntentService();

            // 0. Le lecteur : simulé ou réel, en ligne ?
            statut("Connexion au lecteur...");
            Stripe.Terminal.Reader lecteur = await lecteurs.GetAsync(ReaderId);
            bool simule = (lecteur.DeviceType ?? "").StartsWith("simulated");
            string nomLecteur = string.IsNullOrEmpty(lecteur.Label) ? lecteur.Id : lecteur.Label;

            string carteTest = null;
            if (simule)
            {
                carteTest = demanderCarteTest();
                if (string.IsNullOrEmpty(carteTest)) throw new OperationCanceledException("Paiement annulé.");
            }
            else if (lecteur.Status == "offline")
            {
                throw new Exception("Le lecteur « " + nomLecteur + " » est hors ligne.\nVérifie qu'il est allumé et connecté à Internet.");
            }

            // 1. Créer le PaymentIntent
            long cents = (long)Math.Round(montant * 100);
            PaymentIntent paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = cents,
                Currency = "cad",
                PaymentMethodTypes = new List<string> { "card_present", "interac_present" },   // interac = débit au Canada
                CaptureMethod = "automatic"
            });

            // 2. Envoyer le montant au lecteur
            statut("Envoi de " + Theme.Argent(montant) + " au lecteur...");
            await lecteurs.ProcessPaymentIntentAsync(ReaderId,
                new Stripe.Terminal.ReaderProcessPaymentIntentOptions { PaymentIntent = paymentIntent.Id });

            if (simule)
            {
                // 3a. Lecteur simulé : on présente la carte de test
                statut("Simulation de la carte " + carteTest + "...");
                JObject resultat = await PresenterCarteTestAsync(ReaderId, carteTest);
                string statutAction = resultat["action"]?["status"]?.ToString();
                string messageEchec = resultat["action"]?["failure_message"]?.ToString();
                if (statutAction == "failed")
                    throw new Exception("Paiement refusé : " + (messageEchec ?? "raison inconnue"));
            }
            else
            {
                // 3b. Lecteur réel : on attend la carte du client (3 minutes au maximum)
                statut("Présentez la carte sur le lecteur « " + nomLecteur + " »");
                DateTime limite = DateTime.Now.AddMinutes(3);
                while (true)
                {
                    if (annulation.IsCancellationRequested || DateTime.Now > limite)
                    {
                        bool delai = !annulation.IsCancellationRequested;
                        try { await lecteurs.CancelActionAsync(ReaderId); } catch { }
                        try { await paymentIntentService.CancelAsync(paymentIntent.Id); } catch { }
                        if (delai) throw new Exception("Délai dépassé : aucune carte n'a été présentée.");
                        throw new OperationCanceledException("Paiement annulé.");
                    }

                    try { await Task.Delay(1000, annulation); } catch (TaskCanceledException) { continue; }

                    Stripe.Terminal.Reader etat = await lecteurs.GetAsync(ReaderId);
                    if (etat.Action == null || etat.Action.Status == "succeeded") break;
                    if (etat.Action.Status == "failed")
                        throw new Exception("Paiement refusé : " + (etat.Action.FailureMessage ?? "raison inconnue"));
                }
            }

            // 4. Vérifier le statut final
            PaymentIntent confirme = await paymentIntentService.GetAsync(paymentIntent.Id);
            if (confirme.Status != "succeeded")
                throw new Exception("Le paiement n'a pas abouti (statut : " + confirme.Status + ").");

            return confirme.Id;
        }

        // =====================================================================
        // Après un paiement : marque, type (crédit/débit) et 4 derniers chiffres
        // Retourne null si Stripe ne donne pas l'information (le paiement reste valide)
        // =====================================================================
        public static async Task<InfoCarte> LireCarteAsync(string paymentIntentId)
        {
            if (string.IsNullOrEmpty(paymentIntentId)) return null;
            try
            {
                PaymentIntent pi = await new PaymentIntentService().GetAsync(paymentIntentId);
                if (string.IsNullOrEmpty(pi.LatestChargeId)) return null;

                Charge charge = await new ChargeService().GetAsync(pi.LatestChargeId);
                var details = charge.PaymentMethodDetails;
                if (details == null) return null;

                if (details.InteracPresent != null)       // débit Interac au TPE
                    return new InfoCarte { Marque = "interac", Financement = "debit", Derniers4 = details.InteracPresent.Last4 };
                if (details.CardPresent != null)          // carte au TPE
                    return new InfoCarte { Marque = details.CardPresent.Brand, Financement = details.CardPresent.Funding, Derniers4 = details.CardPresent.Last4 };
                if (details.Card != null)                 // carte en ligne (Checkout)
                    return new InfoCarte { Marque = details.Card.Brand, Financement = details.Card.Funding, Derniers4 = details.Card.Last4 };
            }
            catch
            {
                // Pas grave : la vente est enregistrée sans le type de carte
            }
            return null;
        }

        // Appel direct à l'API REST de Stripe pour présenter une carte test précise
        private static async Task<JObject> PresenterCarteTestAsync(string readerId, string numeroCarte)
        {
            string url = "https://api.stripe.com/v1/test_helpers/terminal/readers/" + readerId + "/present_payment_method";
            var parametres = new Dictionary<string, string>
            {
                { "type", "card_present" },
                { "card_present[number]", numeroCarte }
            };

            using (var requete = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(parametres) })
            {
                requete.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", StripeConfiguration.ApiKey);

                HttpResponseMessage reponse = await http.SendAsync(requete);
                string contenu = await reponse.Content.ReadAsStringAsync();
                if (!reponse.IsSuccessStatusCode)
                    throw new Exception("Erreur API Stripe (" + (int)reponse.StatusCode + ") : " + contenu);

                return JObject.Parse(contenu);
            }
        }
    }

    public class LecteurStripe
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public string Modele { get; set; }
        public bool EnLigne { get; set; }

        public bool Simule { get { return (Modele ?? "").StartsWith("simulated"); } }

        public override string ToString()
        {
            return Nom + (Simule ? "  (simulé)" : "") + (EnLigne ? "" : "  (hors ligne)");
        }
    }

    public class InfoCompteStripe
    {
        public string Nom { get; set; }
        public string Pays { get; set; }
        public bool ModeTest { get; set; }
        public List<LecteurStripe> Lecteurs { get; set; } = new List<LecteurStripe>();
        public string ErreurLecteurs { get; set; }
    }
}
