// wEntrainement.cs
using JeuHoy_WPF_Natif.présentation;
using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JeuHoy_WPF.vue
{
    /// <summary>
    /// Auteur:      Hugo St-Louis
    /// Description: Permet de faire l'entrainement des différentes figures de danse.
    /// Date:        2023-04-17
    /// </summary>
    public enum DisplayFrameType
    {
        Infrared,
        Color,
        Depth
    }

    /// <summary>
    /// Auteur :      Hugo St-Louis
    /// Description : Expérimentation avec la Kinect XBox One.
    /// Date :        2024-04-18
    /// </summary>
    public partial class wEntrainement : Window, IEntrainementVue
    {
        #region Constants
        private const DisplayFrameType DEFAULT_DISPLAYFRAMETYPE = DisplayFrameType.Color;
        public static readonly double DPI = 96.0;
        public static readonly PixelFormat FORMAT = PixelFormats.Bgra32;
        #endregion

        private KinectSensor _kinectSensor = null;
        private EntrainementPresenteur _presenteur;
        private MultiSourceFrameReader _multisourceFrameReader = null;

        /// <summary>
        /// Constructeur
        /// </summary>
        public wEntrainement()
        {
            InitializeComponent();

            // Initialiser le présenteur
            _presenteur = new EntrainementPresenteur(this);

            // Initialiser Kinect
            _kinectSensor = KinectSensor.GetDefault();
            if (_kinectSensor != null)
            {
                // Vérifie si la Kinect est fonctionnelle.
                _kinectSensor.Open();
                _kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;

                // Initialiser le présenteur avec Kinect
                _presenteur.InitialiserKinect(_kinectSensor, DEFAULT_DISPLAYFRAMETYPE);

                // Lecture des 3 types d'image
                _multisourceFrameReader = _kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Infrared | FrameSourceTypes.Depth);
                _multisourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                // Pour la lecture de squelette
                BodyFrameReader bodyframe = _kinectSensor.BodyFrameSource.OpenReader();
                bodyframe.FrameArrived += Bodyframe_FrameArrived;

                // Activer la reconnaissance vocale
                _presenteur.ActiverReconnaissanceVocale();
            }
        }

        #region Implémentation IEntrainementVue

        public void AfficherFigureEnCours(int position, BitmapImage image)
        {
            lblFigureEnCours.Content = position.ToString();
            picPositionAFaire.Source = image;
        }

        public void MettreAJourConsole(string message)
        {
            txtConsole.Text = message;
        }

        public void DessinerJoint(Point position, Color couleur, int taille)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = new SolidColorBrush(couleur);
            ellipse.Width = taille;
            ellipse.Height = taille;

            Canvas.SetLeft(ellipse, position.X - taille / 2);
            Canvas.SetTop(ellipse, position.Y - taille / 2);

            pDessinSquelette.Children.Add(ellipse);
        }

        public void DessinerLigne(Point debut, Point fin, Color couleur, double epaisseur)
        {
            Line line = new Line
            {
                X1 = debut.X,
                Y1 = debut.Y,
                X2 = fin.X,
                Y2 = fin.Y,
                Stroke = new SolidColorBrush(couleur),
                StrokeThickness = epaisseur
            };

            pDessinSquelette.Children.Add(line);
        }

        public void AfficherImage(WriteableBitmap image)
        {
            picKinect.Source = image;
        }

        public void EffacerSquelette()
        {
            pDessinSquelette.Children.Clear();
        }

        public void ChangerEtatReconnaissanceVocale(bool actif, SolidColorBrush couleur, Visibility visibiliteInstructions)
        {
            VoiceRecognitionStatus.Fill = couleur;
            txtInstructionVocale.Visibility = visibiliteInstructions;
        }

        public void DemarrerAnimationReconnaissanceVocale()
        {
            System.Windows.Media.Animation.Storyboard animation =
                (System.Windows.Media.Animation.Storyboard)FindResource("HoyDetectedAnimation");
            animation.Begin();
        }

        public void ChangerModeAffichage(DisplayFrameType mode)
        {
            // Mise à jour des contrôles UI si nécessaire
            // Suite de la méthode ChangerModeAffichage
            // Cette méthode peut mettre à jour l'interface utilisateur en fonction du mode
            // Par exemple, activer/désactiver certains boutons, changer le titre, etc.
        }

        public double LargeurCanvasSquelette => pDessinSquelette.ActualWidth > 0 ? pDessinSquelette.ActualWidth : pDessinSquelette.Width;

        public double HauteurCanvasSquelette => pDessinSquelette.ActualHeight > 0 ? pDessinSquelette.ActualHeight : pDessinSquelette.Height;

        #endregion

        /// <summary>
        /// Événement lorsqu'un squelette est détecté
        /// </summary>
        private void Bodyframe_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    Body[] squelettes = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(squelettes);

                    // Déléguer le traitement au présenteur
                    _presenteur.TraiterDonneesSquelette(squelettes);
                }
            }
        }

        /// <summary>
        /// Événement lancé lorsque la Kinect change d'état
        /// </summary>
        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.Title = "kinect 2.0 : " + (this._kinectSensor.IsAvailable ? "Connecté" : "Non connecté");
        }

        /// <summary>
        /// Événement lancé lorsqu'une image est créée par la Kinect
        /// </summary>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame != null)
            {
                // Déléguer le traitement au présenteur
                _presenteur.TraiterMultiSourceFrame(multiSourceFrame);
            }
        }

        /// <summary>
        /// Fermer la connexion à la Kinect si on ferme l'écran.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_kinectSensor.IsAvailable)
                _kinectSensor.Close();

            _presenteur.Dispose();
        }

        private void picRetour_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void picRetour_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Change le curseur lorsque le curseur est sur l'image
        /// </summary>
        private void picRetour_MouseHover(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Apprentissage avec la position obtenue à partir de la Kinect
        /// </summary>
        private void btnApprendre_Click(object sender, RoutedEventArgs e)
        {
            _presenteur.ApprendrePosition();
        }

        /// <summary>
        /// Lorsqu'on appuie sur le bouton suivant ou précédent
        /// </summary>
        private void btnClickChangerFigure_Click(object sender, RoutedEventArgs e)
        {
            Control bouton = (Control)sender;

            if (bouton.Name == "btnSuivant")
                _presenteur.FigureSuivante();
            else if (bouton.Name == "btnPrecedent")
                _presenteur.FigurePrecedente();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _presenteur.DesactiverReconnaissanceVocale();
        }

        private void btnRecoVocale_Checked(object sender, RoutedEventArgs e)
        {
            _presenteur.ActiverReconnaissanceVocale();
        }

        private void btnRecoVocale_Unchecked(object sender, RoutedEventArgs e)
        {
            _presenteur.DesactiverReconnaissanceVocale();
        }

        /// <summary>
        /// Modifier le type d'affichage pour infrarouge
        /// </summary>
        private void InfraredButton_Click(object sender, RoutedEventArgs e)
        {
            _presenteur.ConfigurerAffichage(DisplayFrameType.Infrared);
        }

        /// <summary>
        /// Modifier le type d'affichage pour couleur
        /// </summary>
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            _presenteur.ConfigurerAffichage(DisplayFrameType.Color);
        }

        /// <summary>
        /// Modifier le type d'affichage pour profondeur
        /// </summary>
        private void DepthButton_Click(object sender, RoutedEventArgs e)
        {
            _presenteur.ConfigurerAffichage(DisplayFrameType.Depth);
        }
    }
}