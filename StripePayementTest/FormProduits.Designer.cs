namespace StripePayementTest
{
    partial class FormProduits
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dgvProduits = new DataGridView();
            label1 = new Label();
            txtCodeBarre = new TextBox();
            label2 = new Label();
            txtNom = new TextBox();
            label3 = new Label();
            txtPrix = new TextBox();
            label4 = new Label();
            txtStock = new TextBox();
            btnAjouter = new Button();
            btnModifier = new Button();
            btnSupprimer = new Button();
            lblStockFaible = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvProduits).BeginInit();
            SuspendLayout();
            // 
            // dgvProduits
            // 
            dgvProduits.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProduits.Location = new Point(46, 34);
            dgvProduits.Name = "dgvProduits";
            dgvProduits.RowHeadersWidth = 51;
            dgvProduits.Size = new Size(1104, 404);
            dgvProduits.TabIndex = 0;
            dgvProduits.CellClick += dgvProduits_CellClick;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(235, 489);
            label1.Name = "label1";
            label1.Size = new Size(98, 20);
            label1.TabIndex = 1;
            label1.Text = "Code-barres :";
            // 
            // txtCodeBarre
            // 
            txtCodeBarre.Location = new Point(339, 489);
            txtCodeBarre.Name = "txtCodeBarre";
            txtCodeBarre.Size = new Size(125, 27);
            txtCodeBarre.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(210, 538);
            label2.Name = "label2";
            label2.Size = new Size(123, 20);
            label2.TabIndex = 3;
            label2.Text = "Nom du produit :";
            // 
            // txtNom
            // 
            txtNom.Location = new Point(339, 538);
            txtNom.Name = "txtNom";
            txtNom.Size = new Size(125, 27);
            txtNom.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(293, 587);
            label3.Name = "label3";
            label3.Size = new Size(40, 20);
            label3.TabIndex = 5;
            label3.Text = "Prix :";
            // 
            // txtPrix
            // 
            txtPrix.Location = new Point(339, 587);
            txtPrix.Name = "txtPrix";
            txtPrix.Size = new Size(125, 27);
            txtPrix.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(283, 633);
            label4.Name = "label4";
            label4.Size = new Size(52, 20);
            label4.TabIndex = 7;
            label4.Text = "Stock :";
            // 
            // txtStock
            // 
            txtStock.Location = new Point(339, 633);
            txtStock.Name = "txtStock";
            txtStock.Size = new Size(125, 27);
            txtStock.TabIndex = 8;
            // 
            // btnAjouter
            // 
            btnAjouter.Location = new Point(583, 480);
            btnAjouter.Name = "btnAjouter";
            btnAjouter.Size = new Size(177, 29);
            btnAjouter.TabIndex = 9;
            btnAjouter.Text = "Ajouter le produit";
            btnAjouter.UseVisualStyleBackColor = true;
            btnAjouter.Click += btnAjouter_Click;
            // 
            // btnModifier
            // 
            btnModifier.Location = new Point(583, 550);
            btnModifier.Name = "btnModifier";
            btnModifier.Size = new Size(177, 29);
            btnModifier.TabIndex = 10;
            btnModifier.Text = "Modifier le produit";
            btnModifier.UseVisualStyleBackColor = true;
            btnModifier.Click += btnModifier_Click;
            // 
            // btnSupprimer
            // 
            btnSupprimer.Location = new Point(583, 624);
            btnSupprimer.Name = "btnSupprimer";
            btnSupprimer.Size = new Size(177, 29);
            btnSupprimer.TabIndex = 11;
            btnSupprimer.Text = "Supprimer le produit";
            btnSupprimer.UseVisualStyleBackColor = true;
            btnSupprimer.Click += btnSupprimer_Click;
            // 
            // lblStockFaible
            // 
            lblStockFaible.AutoSize = true;
            lblStockFaible.Location = new Point(934, 480);
            lblStockFaible.Name = "lblStockFaible";
            lblStockFaible.Size = new Size(216, 20);
            lblStockFaible.TabIndex = 12;
            lblStockFaible.Text = "PRODUITS EN STOCK FAIBLE : 0";
            // 
            // FormProduits
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1246, 737);
            Controls.Add(lblStockFaible);
            Controls.Add(btnSupprimer);
            Controls.Add(btnModifier);
            Controls.Add(btnAjouter);
            Controls.Add(txtStock);
            Controls.Add(label4);
            Controls.Add(txtPrix);
            Controls.Add(label3);
            Controls.Add(txtNom);
            Controls.Add(label2);
            Controls.Add(txtCodeBarre);
            Controls.Add(label1);
            Controls.Add(dgvProduits);
            Name = "FormProduits";
            Text = "Gestion des produits";
            Load += FormProduits_Load;
            ((System.ComponentModel.ISupportInitialize)dgvProduits).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvProduits;
        private Label label1;
        private TextBox txtCodeBarre;
        private Label label2;
        private TextBox txtNom;
        private Label label3;
        private TextBox txtPrix;
        private Label label4;
        private TextBox txtStock;
        private Button btnAjouter;
        private Button btnModifier;
        private Button btnSupprimer;
        private Label lblStockFaible;
    }
}