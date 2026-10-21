using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;
using Stripe.Terminal;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;

namespace StripePayementTest
{
    public partial class Form1 : Form
    {
        private string readerId = "tmr_GqlWDge6Okh7ub"; // ✅ corrigé : grand "O", pas un zéro

        private const decimal TAUX_TPS = 0.05m;
        private const decimal TAUX_TVQ = 0.09975m;
        private PrintDocument ticketDocument = new PrintDocument();
        private List<string> lignesDernierTicket = new List<string>();
        private decimal totalDernierTicket = 0;
        private string numeroDerniereTransaction = "";
        private static readonly HttpClient httpClient = new HttpClient();

        private FilterInfoCollection videoDevicesEmbed;
        private VideoCaptureDevice videoSourceEmbed;
        private readonly Reader zxingReaderEmbed = new Reader();
        private readonly object frameLockEmbed = new object();
        private Bitmap dernierFrameEmbed;
        private System.Windows.Forms.Timer minuteurEmbed;
        private bool scannerActif = false;

        public Form1()
        {
            InitializeComponent();
            ApplyModernStyle();
            ticketDocument.PrintPage += TicketDocument_PrintPage;
            StartLocalServer();
            StripeConfiguration.ApiKey =
    Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

            if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
            {
                MessageBox.Show(
                    "Clé API Stripe introuvable. Vérifie que la variable d'environnement STRIPE_SECRET_KEY est bien configurée.",
                    "Erreur de configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

        }
        private void ApplyModernStyle()
        {

            lblSousTotal.AutoSize = true;
            lblTPS.AutoSize = true;
            lblTVQ.AutoSize = true;
            lblTotalPanier.AutoSize = true;
            // Couleurs de la palette
            Color couleurFond = Color.FromArgb(245, 246, 248);
            Color couleurPrincipale = Color.FromArgb(37, 99, 235);   // bleu (actions principales)
            Color couleurSucces = Color.FromArgb(22, 163, 74);       // vert (paiement)
            Color couleurDanger = Color.FromArgb(220, 38, 38);       // rouge (suppression)
            Color couleurNeutre = Color.FromArgb(71, 85, 105);       // gris-bleu (navigation)
            Color couleurTexte = Color.White;

            this.BackColor = couleurFond;
            this.Font = new Font("Segoe UI", 9F);

            // Style de base pour tous les boutons
            void StyleBouton(Button btn, Color couleur)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = couleur;
                btn.ForeColor = couleurTexte;
                btn.Font = new Font("Segoe UI Semibold", 9.5F);
                btn.Cursor = Cursors.Hand;
                btn.Height = Math.Max(btn.Height, 36);
            }

            // Boutons d'action principale (paiement)
            StyleBouton(btnPayer, couleurSucces);
            StyleBouton(btnPayerTPE, couleurSucces);

            // Boutons destructifs
            StyleBouton(btnSupprimerPanier, couleurDanger);
            StyleBouton(btnViderPanier, couleurDanger);

            // Boutons de navigation/neutres
            StyleBouton(btnGestionProduits, couleurNeutre);
            StyleBouton(btnHistoriqueVentes, couleurNeutre);
            StyleBouton(btnImprimerTicket, couleurPrincipale);

            // Mettre en évidence le total à payer
            lblTotalPanier.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTotalPanier.ForeColor = couleurPrincipale;

            // Sous-total / taxes en plus discret
            lblSousTotal.ForeColor = couleurNeutre;
            lblTPS.ForeColor = couleurNeutre;
            lblTVQ.ForeColor = couleurNeutre;
            lblSousTotal.Font = new Font("Segoe UI", 9F);
            lblTPS.Font = new Font("Segoe UI", 9F);
            lblTVQ.Font = new Font("Segoe UI", 9F);

            // Zone de statut (résultat)
            lblResultat.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            // Style du tableau (panier)
            dgvPanier.BackgroundColor = Color.White;
            dgvPanier.BorderStyle = BorderStyle.None;
            dgvPanier.GridColor = Color.FromArgb(226, 232, 240);
            dgvPanier.RowHeadersVisible = false;
            dgvPanier.EnableHeadersVisualStyles = false;
            dgvPanier.ColumnHeadersDefaultCellStyle.BackColor = couleurPrincipale;
            dgvPanier.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPanier.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
            dgvPanier.ColumnHeadersHeight = 34;
            dgvPanier.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvPanier.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvPanier.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvPanier.RowTemplate.Height = 30;

            // Champ code-barres
            txtCodeBarre.Font = new Font("Segoe UI", 10F);
            txtCodeBarre.BorderStyle = BorderStyle.FixedSingle;
        }

        private void btnPayer_Click(object sender, EventArgs e)
        {
            var (sousTotal, tps, tvq, montant) = CalculerMontants();

            if (montant <= 0)
            {
                MessageBox.Show("Le panier est vide.");
                return;
            }

            long montantEnCents = (long)(montant * 100);
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = "http://localhost:5050/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "http://localhost:5050/cancel",
                LineItems = new List<SessionLineItemOptions>
    {
        new SessionLineItemOptions
        {
            Quantity = 1,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "cad",
                UnitAmount = montantEnCents,
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = "Paiement test (taxes incluses)"
                }
            }
        }
    }
            };
            var service = new SessionService();
            var session = service.Create(options);
            Process.Start(new ProcessStartInfo
            {
                FileName = session.Url,
                UseShellExecute = true
            });
        }
        private async void StartLocalServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();
            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                string path = context.Request.Url.AbsolutePath;
                string sessionId = context.Request.QueryString["session_id"];
                string message = "";

                if (path == "/success")
                {
                    message = "Paiement réussi !";
                    var sessionService = new SessionService();
                    var session = sessionService.Get(sessionId);
                    decimal montantPaye = (session.AmountTotal ?? 0) / 100m;
                    string numeroTransaction = session.PaymentIntentId;
                    numeroDerniereTransaction = numeroTransaction;
                    int paiementId = EnregistrerPaiement(montantPaye, "Réussi", numeroTransaction);
                    this.Invoke(() => EnregistrerDetailsVente(paiementId));
                    lblResultat.Invoke(() => lblResultat.Text = "Paiement réussi !");
                    lignesDernierTicket.Clear();
                    totalDernierTicket = 0;
                    this.Invoke(() =>
                    {
                        foreach (DataGridViewRow row in dgvPanier.Rows)
                        {
                            if (row.Cells["colProduit"].Value != null)
                            {
                                string produit = row.Cells["colProduit"].Value.ToString();
                                int quantite = Convert.ToInt32(row.Cells["colQuantite"].Value);
                                decimal totalProduit = Convert.ToDecimal(row.Cells["colTotal"].Value);
                                lignesDernierTicket.Add($"{produit} x{quantite} - {totalProduit:C}");
                                totalDernierTicket += totalProduit;
                            }
                        }
                    });
                    this.Invoke(() => DiminuerStockApresPaiement());
                    this.Invoke(() => dgvPanier.Rows.Clear());



                    this.Invoke(() => lblTotalPanier.Text = "TOTAL À PAYER : 0,00 $");

                }
                else if (path == "/cancel")
                {
                    message = "Paiement annulé.";
                    lblResultat.Invoke(() => lblResultat.Text = "Paiement annulé.");
                }

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
        }
        private int EnregistrerPaiement(decimal montant, string statut, string numeroTransaction)
        {
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            string sql = @"
        INSERT INTO Paiements
        (Montant, Statut, NumeroTransaction)
        VALUES (@Montant, @Statut, @NumeroTransaction);

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using SqlCommand command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Montant", montant);
            command.Parameters.AddWithValue("@Statut", statut);
            command.Parameters.AddWithValue("@NumeroTransaction", numeroTransaction);

            int paiementId = Convert.ToInt32(command.ExecuteScalar());

            return paiementId;
        }

        private void EnregistrerDetailsVente(int paiementId)
        {
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            foreach (DataGridViewRow row in dgvPanier.Rows)
            {
                if (row.Cells["colProduit"].Value != null)
                {
                    string nomProduit = row.Cells["colProduit"].Value.ToString();
                    int quantite = Convert.ToInt32(row.Cells["colQuantite"].Value);
                    decimal prixUnitaire = Convert.ToDecimal(row.Cells["colPrix"].Value);
                    decimal total = Convert.ToDecimal(row.Cells["colTotal"].Value);

                    string sql = @"
                INSERT INTO DetailsVente
                (PaiementId, ProduitId, NomProduit, Quantite, PrixUnitaire, Total)
                SELECT @PaiementId, Id, @NomProduit, @Quantite, @PrixUnitaire, @Total
                FROM Produits
                WHERE Nom = @NomProduit";

                    using SqlCommand command = new SqlCommand(sql, connection);

                    command.Parameters.AddWithValue("@PaiementId", paiementId);
                    command.Parameters.AddWithValue("@NomProduit", nomProduit);
                    command.Parameters.AddWithValue("@Quantite", quantite);
                    command.Parameters.AddWithValue("@PrixUnitaire", prixUnitaire);
                    command.Parameters.AddWithValue("@Total", total);

                    command.ExecuteNonQuery();
                }
            }
        }

        private void txtCodeBarre_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string codeBarre = txtCodeBarre.Text.Trim();
                RechercherProduit(codeBarre);
            }
        }

        private decimal CalculerPrixAvecPromo(decimal prixUnitaire, int quantite, int? quantitePromo, decimal? prixPromo)
        {
            if (quantitePromo.HasValue && prixPromo.HasValue && quantitePromo.Value > 0)
            {
                int nombreLots = quantite / quantitePromo.Value;
                int reste = quantite % quantitePromo.Value;
                return (nombreLots * prixPromo.Value) + (reste * prixUnitaire);
            }
            return prixUnitaire * quantite;
        }


        private void RechercherProduit(string codeBarre)
        {
            string connectionString =
        @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";
            using SqlConnection connection = new SqlConnection(connectionString);
            string sql = @"
SELECT Nom, Prix, Stock, QuantitePromo, PrixPromo, Taxable
FROM Produits
WHERE CodeBarre = @CodeBarre";
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CodeBarre", codeBarre);
            connection.Open();
            using SqlDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                string nom = reader["Nom"].ToString();
                decimal prix = Convert.ToDecimal(reader["Prix"]);
                int stock = Convert.ToInt32(reader["Stock"]);
                int? quantitePromo = reader["QuantitePromo"] == DBNull.Value ? null : Convert.ToInt32(reader["QuantitePromo"]);
                decimal? prixPromo = reader["PrixPromo"] == DBNull.Value ? null : Convert.ToDecimal(reader["PrixPromo"]);
                bool taxable = Convert.ToBoolean(reader["Taxable"]);

                string messagePromo = quantitePromo.HasValue
                    ? $"\nPromo : {quantitePromo} pour {prixPromo:C}"
                    : "";
                string messageTaxe = taxable ? "" : "\n(Produit non taxable)";
                MessageBox.Show($"{nom} - {prix:C} - Stock : {stock}{messagePromo}{messageTaxe}");

                bool produitExiste = false;
                foreach (DataGridViewRow row in dgvPanier.Rows)
                {
                    if (row.Cells["colProduit"].Value?.ToString() == nom)
                    {
                        int quantite = Convert.ToInt32(row.Cells["colQuantite"].Value);
                        if (quantite >= stock)
                        {
                            MessageBox.Show($"Stock insuffisant. Il reste seulement {stock} unité(s).");
                            return;
                        }
                        quantite++;
                        row.Cells["colQuantite"].Value = quantite;
                        row.Cells["colTotal"].Value = CalculerPrixAvecPromo(prix, quantite, quantitePromo, prixPromo);
                        produitExiste = true;
                        break;
                    }
                }
                if (!produitExiste)
                {
                    int nouvelleQuantite = 1;
                    decimal totalLigne = CalculerPrixAvecPromo(prix, nouvelleQuantite, quantitePromo, prixPromo);

                    int rowIndex = dgvPanier.Rows.Add();
                    dgvPanier.Rows[rowIndex].Cells["colProduit"].Value = nom;
                    dgvPanier.Rows[rowIndex].Cells["colPrix"].Value = prix;
                    dgvPanier.Rows[rowIndex].Cells["colQuantite"].Value = nouvelleQuantite;
                    dgvPanier.Rows[rowIndex].Cells["colTotal"].Value = totalLigne;
                    dgvPanier.Rows[rowIndex].Cells["colQuantitePromo"].Value = quantitePromo;
                    dgvPanier.Rows[rowIndex].Cells["colPrixPromo"].Value = prixPromo;
                    dgvPanier.Rows[rowIndex].Cells["colTaxable"].Value = taxable;
                }
                CalculerTotalPanier();
            }
        }

        private (decimal totalTaxable, decimal totalNonTaxable) CalculerSousTotaux()
        {
            decimal totalTaxable = 0;
            decimal totalNonTaxable = 0;

            foreach (DataGridViewRow row in dgvPanier.Rows)
            {
                if (row.Cells["colTotal"].Value != null)
                {
                    decimal ligneTotal = Convert.ToDecimal(row.Cells["colTotal"].Value);
                    bool taxable = row.Cells["colTaxable"].Value == null || Convert.ToBoolean(row.Cells["colTaxable"].Value);

                    if (taxable)
                        totalTaxable += ligneTotal;
                    else
                        totalNonTaxable += ligneTotal;
                }
            }
            return (totalTaxable, totalNonTaxable);
        }

        private (decimal sousTotal, decimal tps, decimal tvq, decimal total) CalculerMontants()
        {
            var (totalTaxable, totalNonTaxable) = CalculerSousTotaux();
            decimal tps = Math.Round(totalTaxable * TAUX_TPS, 2);
            decimal tvq = Math.Round(totalTaxable * TAUX_TVQ, 2);
            decimal sousTotal = totalTaxable + totalNonTaxable;
            decimal total = sousTotal + tps + tvq;
            return (sousTotal, tps, tvq, total);
        }

        private void CalculerTotalPanier()
        {
            var (sousTotal, tps, tvq, total) = CalculerMontants();

            lblSousTotal.Text = $"Sous-total : {sousTotal:C}";
            lblTPS.Text = $"TPS (5%) : {tps:C}";
            lblTVQ.Text = $"TVQ (9,975%) : {tvq:C}";
            lblTotalPanier.Text = $"TOTAL À PAYER : {total:C}";
        }

        private void btnImprimerTicket_Click(object sender, EventArgs e)
        {
            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = ticketDocument;
            preview.ShowDialog();

        }
        private void TicketDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font police = new Font("Arial", 12);
            float y = 20;
            e.Graphics.DrawString("TICKET DE CAISSE", police, Brushes.Black, 20, y);
            y += 30;
            e.Graphics.DrawString($"Date : {DateTime.Now:dd/MM/yyyy HH:mm}", police, Brushes.Black, 20, y);
            y += 25;
            e.Graphics.DrawString($"Transaction : {numeroDerniereTransaction}", police, Brushes.Black, 20, y);
            y += 30;

            foreach (string ligne in lignesDernierTicket)
            {
                e.Graphics.DrawString(
                    ligne,
                    police,
                    Brushes.Black,
                    20,
                    y);

                y += 25;
            }

            decimal sousTotalTicket = totalDernierTicket;
            decimal tpsTicket = Math.Round(sousTotalTicket * TAUX_TPS, 2);
            decimal tvqTicket = Math.Round(sousTotalTicket * TAUX_TVQ, 2);
            decimal totalAvecTaxesTicket = sousTotalTicket + tpsTicket + tvqTicket;

            y += 15;
            e.Graphics.DrawString($"Sous-total : {sousTotalTicket:C}", police, Brushes.Black, 20, y);
            y += 20;
            e.Graphics.DrawString($"TPS (5%) : {tpsTicket:C}", police, Brushes.Black, 20, y);
            y += 20;
            e.Graphics.DrawString($"TVQ (9,975%) : {tvqTicket:C}", police, Brushes.Black, 20, y);
            y += 25;
            e.Graphics.DrawString(
    $"TOTAL : {totalAvecTaxesTicket:C}",
    new Font("Arial", 14, FontStyle.Bold),
    Brushes.Black,
    20,
    y);
        }
        private void DiminuerStockApresPaiement()
        {
            string connectionString =
    @"Server=.\SQLEXPRESS;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True;";
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            foreach (DataGridViewRow row in dgvPanier.Rows)
            {
                if (row.Cells["colProduit"].Value != null)
                {
                    string produit = row.Cells["colProduit"].Value.ToString();
                    int quantite = Convert.ToInt32(row.Cells["colQuantite"].Value);
                    string sql = @"
    UPDATE Produits
    SET Stock = Stock - @Quantite
    WHERE Nom = @Produit";
                    using SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@Quantite", quantite);
                    command.Parameters.AddWithValue("@Produit", produit);
                    command.ExecuteNonQuery();
                }
            }
        }



        private void btnSupprimerPanier_Click(object sender, EventArgs e)
        {
            if (dgvPanier.CurrentRow != null)
            {
                dgvPanier.Rows.Remove(dgvPanier.CurrentRow);
                CalculerTotalPanier();
            }
        }

        private void btnViderPanier_Click(object sender, EventArgs e)
        {
            dgvPanier.Rows.Clear();
            CalculerTotalPanier();
        }

        private void btnGestionProduits_Click(object sender, EventArgs e)
        {
            FormProduits formProduits = new FormProduits();
            formProduits.ShowDialog();
        }

        private void btnHistoriqueVentes_Click(object sender, EventArgs e)
        {
            FormVentes formVentes = new FormVentes();
            formVentes.ShowDialog();
        }

        // Appelle directement l'API REST de Stripe pour présenter une carte test
        // avec un numéro précis, sans dépendre des classes du SDK Stripe.net
        // (qui ne les expose pas encore dans cette version).
        private async Task<JObject> PresenterCarteTestAsync(string readerId, string numeroCarte)
        {
            string url = $"https://api.stripe.com/v1/test_helpers/terminal/readers/{readerId}/present_payment_method";

            var parametres = new Dictionary<string, string>
            {
                { "type", "card_present" },
                { "card_present[number]", numeroCarte }
            };

            using var requete = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(parametres)
            };
            requete.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", StripeConfiguration.ApiKey);

            HttpResponseMessage reponse = await httpClient.SendAsync(requete);
            string contenu = await reponse.Content.ReadAsStringAsync();

            if (!reponse.IsSuccessStatusCode)
            {
                throw new Exception($"Erreur API Stripe ({(int)reponse.StatusCode}) : {contenu}");
            }

            return JObject.Parse(contenu);
        }

        private async void btnPayerTPE_Click(object sender, EventArgs e)
        {
            try
            {
                var (sousTotal, tps, tvq, montant) = CalculerMontants();

                if (montant <= 0)
                {
                    MessageBox.Show("Le panier est vide.");
                    return;
                }

                // 1. Ouvrir la fenêtre pour saisir le numéro de carte test
                FormCarteTest formCarte = new FormCarteTest();
                DialogResult resultatDialog = formCarte.ShowDialog();

                if (resultatDialog != DialogResult.OK || !formCarte.Confirme)
                {
                    lblResultat.Text = "Paiement annulé.";
                    return;
                }

                string numeroCarteTest = formCarte.NumeroCarte;

                long montantEnCents = (long)(montant * 100);

                // 2. Créer le PaymentIntent
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = montantEnCents,
                    Currency = "cad",
                    PaymentMethodTypes = new List<string> { "card_present" },
                    CaptureMethod = "automatic"
                };

                var paymentIntentService = new PaymentIntentService();
                PaymentIntent paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

                lblResultat.Text = $"Envoi de {montant:C} au lecteur...";

                // 3. Envoyer le PaymentIntent au lecteur
                var readerService = new Stripe.Terminal.ReaderService();
                var processOptions = new Stripe.Terminal.ReaderProcessPaymentIntentOptions
                {
                    PaymentIntent = paymentIntent.Id
                };

                await readerService.ProcessPaymentIntentAsync(readerId, processOptions);

                lblResultat.Text = $"⏳ Simulation de la carte {numeroCarteTest}...";

                // 4. Présenter LE NUMÉRO DE CARTE TAPÉ, via un appel HTTP direct
                JObject resultatPresent = await PresenterCarteTestAsync(readerId, numeroCarteTest);
                string statutAction = resultatPresent["action"]?["status"]?.ToString();
                string messageEchec = resultatPresent["action"]?["failure_message"]?.ToString();

                // 5. Vérifier le résultat
                if (statutAction == "succeeded")
                {
                    PaymentIntent confirmedIntent = await paymentIntentService.GetAsync(paymentIntent.Id);

                    if (confirmedIntent.Status == "succeeded")
                    {
                        lblResultat.Text = "✅ Paiement réussi !";

                        // --- mêmes étapes que pour Stripe Checkout ---

                        // a) Enregistrer le paiement en base
                        string numeroTransaction = confirmedIntent.Id;
                        numeroDerniereTransaction = numeroTransaction;
                        int paiementId = EnregistrerPaiement(montant, "Réussi", numeroTransaction);
                        EnregistrerDetailsVente(paiementId);

                        // b) Préparer le ticket
                        lignesDernierTicket.Clear();
                        totalDernierTicket = 0;
                        foreach (DataGridViewRow row in dgvPanier.Rows)
                        {
                            if (row.Cells["colProduit"].Value != null)
                            {
                                string produit = row.Cells["colProduit"].Value.ToString();
                                int quantite = Convert.ToInt32(row.Cells["colQuantite"].Value);
                                decimal totalProduit = Convert.ToDecimal(row.Cells["colTotal"].Value);
                                lignesDernierTicket.Add($"{produit} x{quantite} - {totalProduit:C}");
                                totalDernierTicket += totalProduit;
                            }
                        }

                        // c) Diminuer le stock
                        DiminuerStockApresPaiement();

                        // d) Vider le panier
                        dgvPanier.Rows.Clear();
                        CalculerTotalPanier();

                        MessageBox.Show($"✅ Paiement de {montant:C} RÉUSSI !\nCarte : {numeroCarteTest}\nPaymentIntent : {confirmedIntent.Id}");
                    }
                    else
                    {
                        lblResultat.Text = $"Statut : {confirmedIntent.Status}";
                        MessageBox.Show($"Statut du paiement : {confirmedIntent.Status}");
                    }
                }
                else
                {
                    lblResultat.Text = "❌ Échec du paiement";
                    MessageBox.Show($"❌ Échec : {messageEchec}");
                }
            }
            catch (StripeException stripeEx)
            {
                lblResultat.Text = "❌ Erreur Stripe";
                MessageBox.Show($"Erreur Stripe : {stripeEx.StripeError?.Message ?? stripeEx.Message}");
            }
            catch (Exception ex)
            {
                lblResultat.Text = "❌ Erreur";
                MessageBox.Show($"Erreur : {ex.Message}");
            }
        }

        // ✅ CORRIGÉ : utilise maintenant le scanner intégré directement dans Form1,
        // au lieu d'ouvrir FormScannerCamera dans une fenêtre séparée.
        private void btnScannerWebcam_Click(object sender, EventArgs e)
        {
            if (scannerActif)
            {
                ArreterScannerEmbed();
            }
            else
            {
                DemarrerScannerEmbed();
            }
        }

        private void DemarrerScannerEmbed()
        {
            videoDevicesEmbed = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevicesEmbed.Count == 0)
            {
                MessageBox.Show("Aucune webcam détectée.");
                return;
            }

            FilterInfo appareilChoisi = null;
            foreach (FilterInfo appareil in videoDevicesEmbed)
            {
                if (!appareil.Name.Contains("OBS", StringComparison.OrdinalIgnoreCase) &&
                    !appareil.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                {
                    appareilChoisi = appareil;
                    break;
                }
            }
            appareilChoisi ??= videoDevicesEmbed[0];

            videoSourceEmbed = new VideoCaptureDevice(appareilChoisi.MonikerString);
            videoSourceEmbed.NewFrame += VideoSourceEmbed_NewFrame;
            videoSourceEmbed.Start();

            pbCameraEmbed.Visible = true;
            scannerActif = true;

            minuteurEmbed = new System.Windows.Forms.Timer { Interval = 150 };
            minuteurEmbed.Tick += MinuteurEmbed_Tick;
            minuteurEmbed.Start();
        }

        private void ArreterScannerEmbed()
        {
            minuteurEmbed?.Stop();
            scannerActif = false;

            if (videoSourceEmbed != null && videoSourceEmbed.IsRunning)
            {
                videoSourceEmbed.NewFrame -= VideoSourceEmbed_NewFrame;
                videoSourceEmbed.SignalToStop();
            }

            pbCameraEmbed.Visible = false;
            pbCameraEmbed.Image?.Dispose();
            pbCameraEmbed.Image = null;
        }

        private void VideoSourceEmbed_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap copie = (Bitmap)eventArgs.Frame.Clone();
                lock (frameLockEmbed)
                {
                    dernierFrameEmbed?.Dispose();
                    dernierFrameEmbed = copie;
                }
            }
            catch
            {
                // image ratée, on ignore
            }
        }

        private void MinuteurEmbed_Tick(object sender, EventArgs e)
        {
            if (!scannerActif) return;

            Bitmap frameATraiter;
            lock (frameLockEmbed)
            {
                if (dernierFrameEmbed == null) return;
                frameATraiter = dernierFrameEmbed;
                dernierFrameEmbed = null;
            }

            try
            {
                var ancienneImage = pbCameraEmbed.Image;
                pbCameraEmbed.Image = frameATraiter;
                ancienneImage?.Dispose();

                string resultat = zxingReaderEmbed.Decoder(frameATraiter);

                if (!string.IsNullOrEmpty(resultat))
                {
                    string codeTrouve = resultat;
                    ArreterScannerEmbed();

                    txtCodeBarre.Text = codeTrouve;
                    RechercherProduit(codeTrouve);
                }
            }
            catch
            {
                // image ratée, on continue
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            ArreterScannerEmbed();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnBoissons_Click(object sender, EventArgs e)
        {
            using FormBoissons formBoissons = new FormBoissons();
            DialogResult resultat = formBoissons.ShowDialog();

            if (resultat == DialogResult.OK && formBoissons.Confirme)
            {
                RechercherProduit(formBoissons.CodeSelectionne);
            }
        }
    }


}