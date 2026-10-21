namespace StripePayementTest
{
    partial class FormVentes
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
            dgvVentes = new DataGridView();
            dgvDetailsVente = new DataGridView();
            lblVentesAujourdhui = new Label();
            lblNombreVentes = new Label();
            dtpDate = new DateTimePicker();
            btnRechercherDate = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvVentes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvDetailsVente).BeginInit();
            SuspendLayout();
            // 
            // dgvVentes
            // 
            dgvVentes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvVentes.Location = new Point(12, 12);
            dgvVentes.Name = "dgvVentes";
            dgvVentes.RowHeadersWidth = 51;
            dgvVentes.Size = new Size(750, 313);
            dgvVentes.TabIndex = 0;
            dgvVentes.CellClick += dgvVentes_CellClick;
            // 
            // dgvDetailsVente
            // 
            dgvDetailsVente.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDetailsVente.Location = new Point(12, 433);
            dgvDetailsVente.Name = "dgvDetailsVente";
            dgvDetailsVente.RowHeadersWidth = 51;
            dgvDetailsVente.Size = new Size(750, 262);
            dgvDetailsVente.TabIndex = 1;
            // 
            // lblVentesAujourdhui
            // 
            lblVentesAujourdhui.AutoSize = true;
            lblVentesAujourdhui.Location = new Point(553, 346);
            lblVentesAujourdhui.Name = "lblVentesAujourdhui";
            lblVentesAujourdhui.Size = new Size(209, 20);
            lblVentesAujourdhui.TabIndex = 2;
            lblVentesAujourdhui.Text = "VENTES AUJOURD'HUI : 0,00 $";
            // 
            // lblNombreVentes
            // 
            lblNombreVentes.AutoSize = true;
            lblNombreVentes.Location = new Point(496, 386);
            lblNombreVentes.Name = "lblNombreVentes";
            lblNombreVentes.Size = new Size(266, 20);
            lblNombreVentes.TabIndex = 3;
            lblNombreVentes.Text = "NOMBRE DE VENTES AUJOURD'HUI : 0";
            // 
            // dtpDate
            // 
            dtpDate.Location = new Point(12, 364);
            dtpDate.Name = "dtpDate";
            dtpDate.Size = new Size(250, 27);
            dtpDate.TabIndex = 4;
            // 
            // btnRechercherDate
            // 
            btnRechercherDate.Location = new Point(294, 365);
            btnRechercherDate.Name = "btnRechercherDate";
            btnRechercherDate.Size = new Size(94, 29);
            btnRechercherDate.TabIndex = 5;
            btnRechercherDate.Text = "Rechercher";
            btnRechercherDate.UseVisualStyleBackColor = true;
            btnRechercherDate.Click += btnRechercherDate_Click;
            // 
            // FormVentes
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 742);
            Controls.Add(btnRechercherDate);
            Controls.Add(dtpDate);
            Controls.Add(lblNombreVentes);
            Controls.Add(lblVentesAujourdhui);
            Controls.Add(dgvDetailsVente);
            Controls.Add(dgvVentes);
            Name = "FormVentes";
            Text = "Historique des ventes";
            Load += FormVentes_Load;
            ((System.ComponentModel.ISupportInitialize)dgvVentes).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvDetailsVente).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvVentes;
        private DataGridView dgvDetailsVente;
        private Label lblVentesAujourdhui;
        private Label lblNombreVentes;
        private DateTimePicker dtpDate;
        private Button btnRechercherDate;
    }
}