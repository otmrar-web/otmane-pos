namespace StripePayementTest
{
    partial class FormBoissons
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
            btnCafeRegulier = new Button();
            btnLatte = new Button();
            btnCappuccino = new Button();
            btnMoccachino = new Button();
            btnChocoChaud = new Button();
            btnPolarPop = new Button();
            btnPetit = new Button();
            btnMoyen = new Button();
            btnGrand = new Button();
            lblCategorieChoisie = new Label();
            SuspendLayout();
            // 
            // btnCafeRegulier
            // 
            btnCafeRegulier.Location = new Point(12, 38);
            btnCafeRegulier.Name = "btnCafeRegulier";
            btnCafeRegulier.Size = new Size(132, 29);
            btnCafeRegulier.TabIndex = 0;
            btnCafeRegulier.Text = "btnCafeRegulier";
            btnCafeRegulier.UseVisualStyleBackColor = true;
            btnCafeRegulier.Click += btnCafeRegulier_Click;
            // 
            // btnLatte
            // 
            btnLatte.Location = new Point(150, 38);
            btnLatte.Name = "btnLatte";
            btnLatte.Size = new Size(118, 29);
            btnLatte.TabIndex = 1;
            btnLatte.Text = "btnLatte";
            btnLatte.UseVisualStyleBackColor = true;
            btnLatte.Click += btnLatte_Click;
            // 
            // btnCappuccino
            // 
            btnCappuccino.Location = new Point(274, 38);
            btnCappuccino.Name = "btnCappuccino";
            btnCappuccino.Size = new Size(133, 29);
            btnCappuccino.TabIndex = 2;
            btnCappuccino.Text = "btnCappuccino";
            btnCappuccino.UseVisualStyleBackColor = true;
            btnCappuccino.Click += btnCappuccino_Click;
            // 
            // btnMoccachino
            // 
            btnMoccachino.Location = new Point(413, 38);
            btnMoccachino.Name = "btnMoccachino";
            btnMoccachino.Size = new Size(131, 29);
            btnMoccachino.TabIndex = 3;
            btnMoccachino.Text = "btnMoccachino";
            btnMoccachino.UseVisualStyleBackColor = true;
            btnMoccachino.Click += btnMoccachino_Click;
            // 
            // btnChocoChaud
            // 
            btnChocoChaud.Location = new Point(550, 38);
            btnChocoChaud.Name = "btnChocoChaud";
            btnChocoChaud.Size = new Size(131, 29);
            btnChocoChaud.TabIndex = 4;
            btnChocoChaud.Text = "btnChocoChaud";
            btnChocoChaud.UseVisualStyleBackColor = true;
            btnChocoChaud.Click += btnChocoChaud_Click;
            // 
            // btnPolarPop
            // 
            btnPolarPop.Location = new Point(687, 39);
            btnPolarPop.Name = "btnPolarPop";
            btnPolarPop.Size = new Size(114, 28);
            btnPolarPop.TabIndex = 5;
            btnPolarPop.Text = "btnPolarPop";
            btnPolarPop.UseVisualStyleBackColor = true;
            btnPolarPop.Click += btnPolarPop_Click;
            // 
            // btnPetit
            // 
            btnPetit.Location = new Point(174, 131);
            btnPetit.Name = "btnPetit";
            btnPetit.Size = new Size(94, 29);
            btnPetit.TabIndex = 6;
            btnPetit.Text = "btnPetit";
            btnPetit.UseVisualStyleBackColor = true;
            btnPetit.Visible = false;
            btnPetit.Click += btnPetit_Click;
            // 
            // btnMoyen
            // 
            btnMoyen.Location = new Point(313, 131);
            btnMoyen.Name = "btnMoyen";
            btnMoyen.Size = new Size(94, 29);
            btnMoyen.TabIndex = 7;
            btnMoyen.Text = "btnMoyen";
            btnMoyen.UseVisualStyleBackColor = true;
            btnMoyen.Visible = false;
            btnMoyen.Click += btnMoyen_Click;
            // 
            // btnGrand
            // 
            btnGrand.Location = new Point(450, 131);
            btnGrand.Name = "btnGrand";
            btnGrand.Size = new Size(94, 29);
            btnGrand.TabIndex = 8;
            btnGrand.Text = "btnGrand";
            btnGrand.UseVisualStyleBackColor = true;
            btnGrand.Visible = false;
            btnGrand.Click += btnGrand_Click;
            // 
            // lblCategorieChoisie
            // 
            lblCategorieChoisie.AutoSize = true;
            lblCategorieChoisie.Location = new Point(320, 270);
            lblCategorieChoisie.Name = "lblCategorieChoisie";
            lblCategorieChoisie.Size = new Size(50, 20);
            lblCategorieChoisie.TabIndex = 9;
            lblCategorieChoisie.Text = "label1";
            // 
            // FormBoissons
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(907, 450);
            Controls.Add(lblCategorieChoisie);
            Controls.Add(btnGrand);
            Controls.Add(btnMoyen);
            Controls.Add(btnPetit);
            Controls.Add(btnPolarPop);
            Controls.Add(btnChocoChaud);
            Controls.Add(btnMoccachino);
            Controls.Add(btnCappuccino);
            Controls.Add(btnLatte);
            Controls.Add(btnCafeRegulier);
            Name = "FormBoissons";
            Text = "FormBoissons";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnCafeRegulier;
        private Button btnLatte;
        private Button btnCappuccino;
        private Button btnMoccachino;
        private Button btnChocoChaud;
        private Button btnPolarPop;
        private Button btnPetit;
        private Button btnMoyen;
        private Button btnGrand;
        private Label lblCategorieChoisie;
    }
}