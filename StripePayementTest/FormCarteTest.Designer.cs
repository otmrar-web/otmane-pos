namespace StripePayementTest
{
    partial class FormCarteTest
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
            label1 = new Label();
            txtNumeroCarte = new TextBox();
            btnConfirmer = new Button();
            btnAnnuler = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(119, 97);
            label1.Name = "label1";
            label1.Size = new Size(149, 20);
            label1.TabIndex = 0;
            label1.Text = "Numéro de carte test";
            // 
            // txtNumeroCarte
            // 
            txtNumeroCarte.Location = new Point(309, 100);
            txtNumeroCarte.Name = "txtNumeroCarte";
            txtNumeroCarte.Size = new Size(125, 27);
            txtNumeroCarte.TabIndex = 1;
            // 
            // btnConfirmer
            // 
            btnConfirmer.Location = new Point(155, 180);
            btnConfirmer.Name = "btnConfirmer";
            btnConfirmer.Size = new Size(94, 84);
            btnConfirmer.TabIndex = 2;
            btnConfirmer.Text = "Confirmer le paiement";
            btnConfirmer.UseVisualStyleBackColor = true;
            btnConfirmer.Click += btnConfirmer_Click;
            // 
            // btnAnnuler
            // 
            btnAnnuler.Location = new Point(340, 180);
            btnAnnuler.Name = "btnAnnuler";
            btnAnnuler.Size = new Size(94, 84);
            btnAnnuler.TabIndex = 3;
            btnAnnuler.Text = "Annuler";
            btnAnnuler.UseVisualStyleBackColor = true;
            btnAnnuler.Click += btnAnnuler_Click;
            // 
            // FormCarteTest
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnAnnuler);
            Controls.Add(btnConfirmer);
            Controls.Add(txtNumeroCarte);
            Controls.Add(label1);
            Name = "FormCarteTest";
            Text = "FormCarteTest";
            Load += FormCarteTest_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtNumeroCarte;
        private Button btnConfirmer;
        private Button btnAnnuler;
    }
}