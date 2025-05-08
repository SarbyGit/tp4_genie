using JeuHoy_WPF_Natif.présentation;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    public partial class wEntrainement : Window
    {

        #region Constants
        private const DisplayFrameType DEFAULT_DISPLAYFRAMETYPE = DisplayFrameType.Color;
        #endregion

        private KinectSensor _kinectSensor = null;
        private MultiSourceFrameReader _multisourceFrameReader = null;
        private EntrainementPresenteur _presenteur;

        /// <summary>
        /// ctor
        /// </summary>
        public wEntrainement()
        {
            InitializeComponent();

            // Initialiser le présenteur
            _presenteur = new EntrainementPresenteur();
            _presenteur.ActiverReconnaissanceVocale();
            _presenteur.CommandeVocaleDetectee += Presenteur_CommandeVocaleDetectee;

            // Affichage initial de la figure
            lblFigureEnCours.Content = _presenteur.PositionEnCours.ToString();
            picPositionAFaire.Source = _presenteur.GetImageFigureEnCours();

            _kinectSensor = KinectSensor.GetDefault();
            if (_kinectSensor != null)
            {
                // Passer le KinectSensor au présenteur
                _presenteur.InitKinect(_kinectSensor, pDessinSquelette, DEFAULT_DISPLAYFRAMETYPE);

                // Vérifie si la Kinect est fonctionnelle.
                _kinectSensor.Open();
                _kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;

                //Lecture des 3 types d'image
                _multisourceFrameReader = _kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Infrared | FrameSourceTypes.Depth);
                _presenteur.SetupCurrentDisplay(DEFAULT_DISPLAYFRAMETYPE);
                _multisourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                // Pour la lecture de squelette
                BodyFrameReader bodyframe = _kinectSensor.BodyFrameSource.OpenReader();
                bodyframe.FrameArrived += Bodyframe_FrameArrived;
            }
        }

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
                    txtConsole.Text = _presenteur.TraiterDonneesSquelette(squelettes);
                }
            }
        }

        /// <summary>
        /// Événement lancé lorsqu'une image est créée par la Kinect
        /// </summary>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame == null)
                return;

            using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                    picKinect.Source = _presenteur.ShowColorFrame(colorFrame);
            }
        }

        /// <summary>
        /// Événement lancé lorsque le statut de la Kinect change
        /// </summary>
        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.Title = "kinect 2.0 : " + (this._kinectSensor.IsAvailable ? "Connecté" : "Non connecté");
        }

        private void picRetour_MouseLeave(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void picRetour_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void picRetour_MouseHover(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Apprentissage avec la position obtenue à partir de la Kinect versus l'image affichée
        /// </summary>
        private void btnApprendre_Click(object sender, RoutedEventArgs e)
        {
            if (_presenteur.CurrentBody != null && _presenteur.CurrentBody.IsTracked)
            {
                _presenteur.ApprendrePosition(_presenteur.CurrentBody.Joints);
                txtConsole.Text = "Position " + _presenteur.PositionEnCours + " apprise !";
            }
            else
            {
                txtConsole.Text = "Aucun squelette détecté. Placez-vous devant la Kinect.";
            }
        }

        /// <summary>
        /// Lorsqu'on appuie sur le bouton suivant ou précédent, modifier la figure en conséquence
        /// </summary>
        private void btnClickChangerFigure_Click(object sender, RoutedEventArgs e)
        {
            Control bouton = (Control)sender;

            if (bouton.Name == "btnSuivant")
                _presenteur.FigureSuivante();
            else if (bouton.Name == "btnPrecedent")
                _presenteur.FigurePrecedente();

            // Mise à jour de l'affichage
            lblFigureEnCours.Content = _presenteur.PositionEnCours.ToString();
            picPositionAFaire.Source = _presenteur.GetImageFigureEnCours();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _presenteur.DesactiverReconnaissanceVocale();

            if (_kinectSensor.IsAvailable)
                _kinectSensor.Close();
        }

        private void Presenteur_CommandeVocaleDetectee(object sender, EventArgs e)
        {
            // Feedback visuel
            System.Windows.Media.Animation.Storyboard animation = (System.Windows.Media.Animation.Storyboard)FindResource("HoyDetectedAnimation");
            animation.Begin();

            if (_presenteur.CurrentBody != null && _presenteur.CurrentBody.IsTracked)
            {
                _presenteur.ApprendrePosition(_presenteur.CurrentBody.Joints);
                txtConsole.Text = "Position " + _presenteur.PositionEnCours + " validée par commande vocale !";
            }
        }

        private void btnRecoVocale_Checked(object sender, RoutedEventArgs e)
        {
            _presenteur.ActiverReconnaissanceVocale();

            VoiceRecognitionStatus.Fill = Brushes.Green;
            txtInstructionVocale.Visibility = Visibility.Visible;
        }

        private void btnRecoVocale_Unchecked(object sender, RoutedEventArgs e)
        {
            _presenteur.DesactiverReconnaissanceVocale();

            VoiceRecognitionStatus.Fill = Brushes.Gray;
            txtInstructionVocale.Visibility = Visibility.Collapsed;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_kinectSensor.IsAvailable)
                _kinectSensor.Close();
        }
    }
}