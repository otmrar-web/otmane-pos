namespace StripePayementTest
{
    partial class FormScannerCamera
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
            pbCamera = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pbCamera).BeginInit();
            SuspendLayout();
            // 
            // pbCamera
            // 
            pbCamera.Dock = DockStyle.Fill;
            pbCamera.Location = new Point(259, 130);
            pbCamera.Name = "pbCamera";
            pbCamera.Size = new Size(258, 193);
            pbCamera.SizeMode = PictureBoxSizeMode.StretchImage;
            pbCamera.TabIndex = 0;
            pbCamera.TabStop = false;
            // 
            // FormScannerCamera
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pbCamera);
            Name = "FormScannerCamera";
            Text = "FormScannerCamera";
            Load += FormScannerCamera_Load;
            FormClosing += FormScannerCamera_FormClosing;
            ((System.ComponentModel.ISupportInitialize)pbCamera).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pbCamera;
    }
}