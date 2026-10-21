using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;

namespace StripePayementTest
{
    public partial class FormScannerCamera : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private readonly Reader zxingReader = new Reader();

        private readonly object frameLock = new object();
        private Bitmap dernierFrame;

        private System.Windows.Forms.Timer minuteur;

        public string CodeScanne { get; private set; }
        public bool Trouve { get; private set; } = false;

        public FormScannerCamera()
        {
            InitializeComponent();
        }

        private void FormScannerCamera_Load(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Aucune webcam détectée.");
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            FilterInfo appareilChoisi = null;
            foreach (FilterInfo appareil in videoDevices)
            {
                if (!appareil.Name.Contains("OBS", StringComparison.OrdinalIgnoreCase) &&
                    !appareil.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                {
                    appareilChoisi = appareil;
                    break;
                }
            }
            appareilChoisi ??= videoDevices[0];

            videoSource = new VideoCaptureDevice(appareilChoisi.MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();

            // Le minuteur s'exécute uniquement sur le thread principal (UI),
            // donc aucune manipulation de Bitmap ne se fait plus depuis le thread caméra.
            minuteur = new System.Windows.Forms.Timer();
            minuteur.Interval = 150;
            minuteur.Tick += Minuteur_Tick;
            minuteur.Start();
        }

        // S'exécute sur le thread caméra : ne fait QUE stocker la dernière image, rien de plus.
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap copie = (Bitmap)eventArgs.Frame.Clone();

                lock (frameLock)
                {
                    dernierFrame?.Dispose();
                    dernierFrame = copie;
                }
            }
            catch
            {
                // on ignore une image ratée, la prochaine arrivera vite
            }
        }

        // S'exécute sur le thread principal (UI) : affichage ET décodage se font ici, en toute sécurité.
        private void Minuteur_Tick(object sender, EventArgs e)
        {
            if (Trouve) return;

            Bitmap frameATraiter;
            lock (frameLock)
            {
                if (dernierFrame == null) return;
                frameATraiter = dernierFrame;
                dernierFrame = null;
            }

            try
            {
                var ancienneImage = pbCamera.Image;
                pbCamera.Image = frameATraiter;
                ancienneImage?.Dispose();

                string resultat = zxingReader.Decoder(frameATraiter);

                if (!string.IsNullOrEmpty(resultat))
                {
                    CodeScanne = resultat;
                    Trouve = true;

                    minuteur.Stop();
                    ArreterCamera();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch
            {
                // image ratée, on continue avec la suivante
            }
        }

        private void ArreterCamera()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource.SignalToStop();
            }
        }

        private void FormScannerCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            minuteur?.Stop();
            ArreterCamera();
        }
    }

    internal class Reader
    {
        private readonly BarcodeReaderGeneric barcodeReader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions { TryHarder = true }
        };

        public string Decoder(Bitmap bitmap)
        {
            LuminanceSource source = ConvertirEnLuminanceSource(bitmap);
            var resultat = barcodeReader.Decode(source);
            return resultat?.Text;
        }

        private LuminanceSource ConvertirEnLuminanceSource(Bitmap bitmap)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                int largeur = bitmap.Width;
                int hauteur = bitmap.Height;
                byte[] rgb = new byte[largeur * hauteur * 3];

                for (int y = 0; y < hauteur; y++)
                {
                    IntPtr ligneSource = data.Scan0 + y * data.Stride;
                    System.Runtime.InteropServices.Marshal.Copy(ligneSource, rgb, y * largeur * 3, largeur * 3);
                }

                return new RGBLuminanceSource(rgb, largeur, hauteur, RGBLuminanceSource.BitmapFormat.BGR24);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}