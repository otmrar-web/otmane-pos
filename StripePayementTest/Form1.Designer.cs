namespace StripePayementTest
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            txtMontant = new TextBox();
            btnPayer = new Button();
            lblResultat = new Label();
            label2 = new Label();
            txtCodeBarre = new TextBox();
            dgvPanier = new DataGridView();
            colProduit = new DataGridViewTextBoxColumn();
            colTaxable = new DataGridViewCheckBoxColumn();
            colQuantitePromo = new DataGridViewTextBoxColumn();
            colPrixPromo = new DataGridViewTextBoxColumn();
            colPrix = new DataGridViewTextBoxColumn();
            colQuantite = new DataGridViewTextBoxColumn();
            colTotal = new DataGridViewTextBoxColumn();
            lblTotalPanier = new Label();
            btnImprimerTicket = new Button();
            btnSupprimerPanier = new Button();
            btnViderPanier = new Button();
            btnGestionProduits = new Button();
            btnHistoriqueVentes = new Button();
            btnPayerTPE = new Button();
            lblSousTotal = new Label();
            lblTPS = new Label();
            lblTVQ = new Label();
            btnScannerWebcam = new Button();
            pbCameraEmbed = new PictureBox();
            btnBoissons = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvPanier).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbCameraEmbed).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(374, 186);
            label1.Name = "label1";
            label1.Size = new Size(65, 20);
            label1.TabIndex = 0;
            label1.Text = "Montant";
            // 
            // txtMontant
            // 
            txtMontant.Location = new Point(492, 183);
            txtMontant.Name = "txtMontant";
            txtMontant.Size = new Size(125, 27);
            txtMontant.TabIndex = 1;
            // 
            // btnPayer
            // 
            btnPayer.Location = new Point(412, 75);
            btnPayer.Name = "btnPayer";
            btnPayer.Size = new Size(148, 29);
            btnPayer.TabIndex = 2;
            btnPayer.Text = "Payer avec Stripe";
            btnPayer.UseVisualStyleBackColor = true;
            btnPayer.Click += btnPayer_Click;
            // 
            // lblResultat
            // 
            lblResultat.AutoSize = true;
            lblResultat.Location = new Point(239, 153);
            lblResultat.Name = "lblResultat";
            lblResultat.Size = new Size(15, 20);
            lblResultat.TabIndex = 3;
            lblResultat.Text = "*";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(359, 269);
            label2.Name = "label2";
            label2.Size = new Size(91, 20);
            label2.TabIndex = 4;
            label2.Text = "Code-barres";
            // 
            // txtCodeBarre
            // 
            txtCodeBarre.Location = new Point(492, 269);
            txtCodeBarre.Name = "txtCodeBarre";
            txtCodeBarre.Size = new Size(125, 27);
            txtCodeBarre.TabIndex = 5;
            txtCodeBarre.KeyDown += txtCodeBarre_KeyDown;
            // 
            // dgvPanier
            // 
            dgvPanier.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPanier.Columns.AddRange(new DataGridViewColumn[] { colProduit, colTaxable, colQuantitePromo, colPrixPromo, colPrix, colQuantite, colTotal });
            dgvPanier.Location = new Point(728, 35);
            dgvPanier.Name = "dgvPanier";
            dgvPanier.RowHeadersWidth = 51;
            dgvPanier.Size = new Size(550, 369);
            dgvPanier.TabIndex = 6;
            // 
            // colProduit
            // 
            colProduit.HeaderText = "Produit";
            colProduit.MinimumWidth = 6;
            colProduit.Name = "colProduit";
            colProduit.Width = 125;
            // 
            // colTaxable
            // 
            colTaxable.HeaderText = "Column1";
            colTaxable.MinimumWidth = 6;
            colTaxable.Name = "colTaxable";
            colTaxable.Visible = false;
            colTaxable.Width = 125;
            // 
            // colQuantitePromo
            // 
            colQuantitePromo.HeaderText = "QuantitePromo";
            colQuantitePromo.MinimumWidth = 6;
            colQuantitePromo.Name = "colQuantitePromo";
            colQuantitePromo.Visible = false;
            colQuantitePromo.Width = 125;
            // 
            // colPrixPromo
            // 
            colPrixPromo.HeaderText = "colPrixPromo";
            colPrixPromo.MinimumWidth = 6;
            colPrixPromo.Name = "colPrixPromo";
            colPrixPromo.Visible = false;
            colPrixPromo.Width = 125;
            // 
            // colPrix
            // 
            colPrix.HeaderText = "Prix";
            colPrix.MinimumWidth = 6;
            colPrix.Name = "colPrix";
            colPrix.Width = 125;
            // 
            // colQuantite
            // 
            colQuantite.HeaderText = "Quantité";
            colQuantite.MinimumWidth = 6;
            colQuantite.Name = "colQuantite";
            colQuantite.Width = 125;
            // 
            // colTotal
            // 
            colTotal.HeaderText = "Total";
            colTotal.MinimumWidth = 6;
            colTotal.Name = "colTotal";
            colTotal.Width = 125;
            // 
            // lblTotalPanier
            // 
            lblTotalPanier.AutoSize = true;
            lblTotalPanier.Location = new Point(1119, 541);
            lblTotalPanier.Name = "lblTotalPanier";
            lblTotalPanier.Size = new Size(159, 20);
            lblTotalPanier.TabIndex = 7;
            lblTotalPanier.Text = "TOTAL À PAYER : 0,00 $";
            // 
            // btnImprimerTicket
            // 
            btnImprimerTicket.Location = new Point(865, 421);
            btnImprimerTicket.Name = "btnImprimerTicket";
            btnImprimerTicket.Size = new Size(168, 29);
            btnImprimerTicket.TabIndex = 8;
            btnImprimerTicket.Text = "Imprimer ticket";
            btnImprimerTicket.UseVisualStyleBackColor = true;
            btnImprimerTicket.Click += btnImprimerTicket_Click;
            // 
            // btnSupprimerPanier
            // 
            btnSupprimerPanier.Location = new Point(865, 512);
            btnSupprimerPanier.Name = "btnSupprimerPanier";
            btnSupprimerPanier.Size = new Size(179, 29);
            btnSupprimerPanier.TabIndex = 19;
            btnSupprimerPanier.Text = "Supprimer du panier";
            btnSupprimerPanier.UseVisualStyleBackColor = true;
            btnSupprimerPanier.Click += btnSupprimerPanier_Click;
            // 
            // btnViderPanier
            // 
            btnViderPanier.Location = new Point(865, 570);
            btnViderPanier.Name = "btnViderPanier";
            btnViderPanier.Size = new Size(179, 29);
            btnViderPanier.TabIndex = 20;
            btnViderPanier.Text = "Vider le panier";
            btnViderPanier.UseVisualStyleBackColor = true;
            btnViderPanier.Click += btnViderPanier_Click;
            // 
            // btnGestionProduits
            // 
            btnGestionProduits.Location = new Point(737, 664);
            btnGestionProduits.Name = "btnGestionProduits";
            btnGestionProduits.Size = new Size(168, 29);
            btnGestionProduits.TabIndex = 21;
            btnGestionProduits.Text = "Gestion des produits";
            btnGestionProduits.UseVisualStyleBackColor = true;
            btnGestionProduits.Click += btnGestionProduits_Click;
            // 
            // btnHistoriqueVentes
            // 
            btnHistoriqueVentes.Location = new Point(737, 629);
            btnHistoriqueVentes.Name = "btnHistoriqueVentes";
            btnHistoriqueVentes.Size = new Size(168, 29);
            btnHistoriqueVentes.TabIndex = 22;
            btnHistoriqueVentes.Text = "Historique des ventes";
            btnHistoriqueVentes.UseVisualStyleBackColor = true;
            btnHistoriqueVentes.Click += btnHistoriqueVentes_Click;
            // 
            // btnPayerTPE
            // 
            btnPayerTPE.Location = new Point(412, 124);
            btnPayerTPE.Name = "btnPayerTPE";
            btnPayerTPE.Size = new Size(148, 29);
            btnPayerTPE.TabIndex = 23;
            btnPayerTPE.Text = "Payer par TPE";
            btnPayerTPE.UseVisualStyleBackColor = true;
            btnPayerTPE.Click += btnPayerTPE_Click;
            // 
            // lblSousTotal
            // 
            lblSousTotal.AutoSize = true;
            lblSousTotal.Location = new Point(1151, 421);
            lblSousTotal.Name = "lblSousTotal";
            lblSousTotal.Size = new Size(127, 20);
            lblSousTotal.TabIndex = 24;
            lblSousTotal.Text = "Sous-total : 0,00 $";
            // 
            // lblTPS
            // 
            lblTPS.AutoSize = true;
            lblTPS.Location = new Point(1161, 462);
            lblTPS.Name = "lblTPS";
            lblTPS.Size = new Size(117, 20);
            lblTPS.TabIndex = 25;
            lblTPS.Text = "TPS (5%) : 0,00 $";
            // 
            // lblTVQ
            // 
            lblTVQ.AutoSize = true;
            lblTVQ.Location = new Point(1126, 497);
            lblTVQ.Name = "lblTVQ";
            lblTVQ.Size = new Size(152, 20);
            lblTVQ.TabIndex = 26;
            lblTVQ.Text = " TVQ (9,975%) : 0,00 $";
            // 
            // btnScannerWebcam
            // 
            btnScannerWebcam.Location = new Point(397, 329);
            btnScannerWebcam.Name = "btnScannerWebcam";
            btnScannerWebcam.Size = new Size(94, 29);
            btnScannerWebcam.TabIndex = 27;
            btnScannerWebcam.Text = "📷 Scanner";
            btnScannerWebcam.UseVisualStyleBackColor = true;
            btnScannerWebcam.Click += btnScannerWebcam_Click;
            // 
            // pbCameraEmbed
            // 
            pbCameraEmbed.BorderStyle = BorderStyle.FixedSingle;
            pbCameraEmbed.Location = new Point(352, 420);
            pbCameraEmbed.Name = "pbCameraEmbed";
            pbCameraEmbed.Size = new Size(300, 220);
            pbCameraEmbed.SizeMode = PictureBoxSizeMode.StretchImage;
            pbCameraEmbed.TabIndex = 28;
            pbCameraEmbed.TabStop = false;
            pbCameraEmbed.Visible = false;
            // 
            // btnBoissons
            // 
            btnBoissons.Location = new Point(412, 234);
            btnBoissons.Name = "btnBoissons";
            btnBoissons.Size = new Size(153, 29);
            btnBoissons.TabIndex = 29;
            btnBoissons.Text = "☕ Boissons";
            btnBoissons.UseVisualStyleBackColor = true;
            btnBoissons.Click += btnBoissons_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1334, 856);
            Controls.Add(btnBoissons);
            Controls.Add(pbCameraEmbed);
            Controls.Add(btnScannerWebcam);
            Controls.Add(lblTVQ);
            Controls.Add(lblTPS);
            Controls.Add(lblSousTotal);
            Controls.Add(btnPayerTPE);
            Controls.Add(btnHistoriqueVentes);
            Controls.Add(btnGestionProduits);
            Controls.Add(btnViderPanier);
            Controls.Add(btnSupprimerPanier);
            Controls.Add(btnImprimerTicket);
            Controls.Add(lblTotalPanier);
            Controls.Add(dgvPanier);
            Controls.Add(txtCodeBarre);
            Controls.Add(label2);
            Controls.Add(lblResultat);
            Controls.Add(btnPayer);
            Controls.Add(txtMontant);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dgvPanier).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbCameraEmbed).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtMontant;
        private Button btnPayer;
        private Label lblResultat;
        private Label label2;
        private TextBox txtCodeBarre;
        private DataGridView dgvPanier;
        private Label lblTotalPanier;
        private Button btnImprimerTicket;
        private Button btnSupprimerPanier;
        private Button btnViderPanier;
        private Button btnGestionProduits;
        private Button btnHistoriqueVentes;
        private Button btnPayerTPE;
        private Label lblSousTotal;
        private Label lblTPS;
        private Label lblTVQ;
        private Button btnScannerWebcam;
        private PictureBox pbCameraEmbed;
        private DataGridViewTextBoxColumn colProduit;
        private DataGridViewCheckBoxColumn colTaxable;
        private DataGridViewTextBoxColumn colQuantitePromo;
        private DataGridViewTextBoxColumn colPrixPromo;
        private DataGridViewTextBoxColumn colPrix;
        private DataGridViewTextBoxColumn colQuantite;
        private DataGridViewTextBoxColumn colTotal;
        private Button btnBoissons;
    }
}
