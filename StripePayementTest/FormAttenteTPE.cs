using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace OtmanePOS
{
    // « Présentez la carte sur le lecteur » + bouton Annuler, pendant un paiement au TPE
    public class FormAttenteTPE : Form
    {
        private readonly Label lblMessage;
        private readonly BoutonArrondi btnAnnuler;
        private readonly CancellationTokenSource annulation;
        private bool peutFermer;

        public FormAttenteTPE(decimal montant, CancellationTokenSource annulation)
        {
            this.annulation = annulation;

            Text = "Paiement par carte";
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = false;          // on ferme seulement quand le paiement est terminé
            ShowInTaskbar = false;

            Label icone = Theme.Etiquette(Theme.IcTPE, Theme.Icone(34f), Theme.Bleu, ContentAlignment.MiddleCenter);
            icone.SetBounds(0, 20, 460, 60);

            Label lblMontant = Theme.Etiquette(Theme.Argent(montant), Theme.Police(24f, FontStyle.Bold), Theme.Texte, ContentAlignment.MiddleCenter);
            lblMontant.SetBounds(0, 84, 460, 46);

            lblMessage = Theme.Etiquette("Connexion au lecteur...", Theme.Police(12f), Theme.TexteDoux, ContentAlignment.MiddleCenter);
            lblMessage.SetBounds(20, 134, 420, 60);

            btnAnnuler = new BoutonArrondi
            {
                Text = "Annuler",
                BackColor = Theme.RougeClair,
                ForeColor = Theme.Rouge,
                Font = Theme.Police(11f, FontStyle.Bold),
                Bounds = new Rectangle(160, 212, 140, 42)
            };
            btnAnnuler.Click += (s, e) =>
            {
                btnAnnuler.Enabled = false;
                lblMessage.Text = "Annulation en cours...";
                annulation.Cancel();
            };

            Controls.AddRange(new Control[] { icone, lblMontant, lblMessage, btnAnnuler });
        }

        public void Message(string texte)
        {
            if (IsDisposed) return;
            lblMessage.Text = texte;
        }

        // Appelé quand le paiement est terminé (réussi, refusé ou annulé)
        public void Terminer()
        {
            peutFermer = true;
            if (!IsDisposed) Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!peutFermer) e.Cancel = true;   // pas de fermeture par Alt+F4 pendant le paiement
            base.OnFormClosing(e);
        }
    }
}
