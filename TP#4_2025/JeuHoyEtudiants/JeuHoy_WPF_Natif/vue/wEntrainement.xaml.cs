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
        public static readonly double DPI = 96.0;
        public static readonly PixelFormat FORMAT = PixelFormats.Bgra32;

        #endregion
        private Dictionary<string, BitmapImage> _dicImgFigure = new Dictionary<string, BitmapImage>();

        private const float _fInfraredSourceValueMaximum = (float)ushort.MaxValue;
        private const float _fInfraredOutputValueMinimum = 0.01f;
        private const float _fInfraredOutputValueMaximum = 1.0f;
        private const float _fInfraredSceneValueAverage = 0.08f;
        private const float _fInfraredSceneStandardDeviations = 3.0f;
        private int _positionEnCours = 1;
        private DisplayFrameType _currentDisplayFrameType;
        private KinectSensor _kinectSensor = null;
        private WriteableBitmap _bitmap = null;

        private ushort[] _picFrameData = null;
        private byte[] _picPixels = null;
        private EntrainementPresenteur _presenteur;
        private Body _currentBody = null; // Le corps actuellement suivi

        private MultiSourceFrameReader _multisourceFrameReader = null;

        /// <summary>
        /// ctor
        /// </summary>
        public wEntrainement()
        {

            InitializeComponent();
            // Initialiser le présenteur
            _presenteur = new EntrainementPresenteur();
            _presenteur.ActiverReconnaissanceVocale();

            // Affichage initial de la figure
            lblFigureEnCours.Content = _presenteur.PositionEnCours.ToString();
            picPositionAFaire.Source = _presenteur.GetImageFigureEnCours();

            _kinectSensor = KinectSensor.GetDefault();
            if (_kinectSensor != null)
            {
                // Vérifie si la Kinect est fonctionnelle.
                _kinectSensor.Open();
                _kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;
                //Lecture des 3 types d'image
                _multisourceFrameReader = _kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Infrared | FrameSourceTypes.Depth);
                SetupCurrentDisplay(DEFAULT_DISPLAYFRAMETYPE);
                _multisourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                // Pour la lecture de squelette
                BodyFrameReader bodyframe = _kinectSensor.BodyFrameSource.OpenReader();
                bodyframe.FrameArrived += Bodyframe_FrameArrived;
                _presenteur.CommandeVocaleDetectee += Presenteur_CommandeVocaleDetectee;
            }
        }
        /// <summary>
        /// Événement lorsqu'un squelette est détecté
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Bodyframe_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    Body[] squelettes = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(squelettes);
                    pDessinSquelette.Children.Clear();

                    // Recherche du premier squelette suivi
                    _currentBody = null;
                    foreach (Body squelette in squelettes)
                    {
                        if (squelette.IsTracked)
                        {
                            _currentBody = squelette;

                            // Dessiner les joints
                            foreach (Joint j in squelette.Joints.Values)
                            {
                                DrawJoint(j, Colors.BlueViolet, 10);
                            }

                            // Dessiner les os
                            DrawBones(squelette);

                            // Identifier la position en temps réel
                            txtConsole.Text = _presenteur.IdentifierPosition(squelette.Joints);

                            break; // On ne traite que le premier squelette
                        }
                    }
                }
            }
        }
        private void DrawBones(Body body)
        {
            // Liste des paires de joints qui forment les os
            var bones = new Tuple<JointType, JointType>[]
            {
                // Colonne vertébrale
                Tuple.Create(JointType.Head, JointType.Neck),
                Tuple.Create(JointType.Neck, JointType.SpineShoulder),
                Tuple.Create(JointType.SpineShoulder, JointType.SpineMid),
                Tuple.Create(JointType.SpineMid, JointType.SpineBase),
                
                // Bras gauche
                Tuple.Create(JointType.SpineShoulder, JointType.ShoulderLeft),
                Tuple.Create(JointType.ShoulderLeft, JointType.ElbowLeft),
                Tuple.Create(JointType.ElbowLeft, JointType.WristLeft),
                Tuple.Create(JointType.WristLeft, JointType.HandLeft),
                
                // Bras droit
                Tuple.Create(JointType.SpineShoulder, JointType.ShoulderRight),
                Tuple.Create(JointType.ShoulderRight, JointType.ElbowRight),
                Tuple.Create(JointType.ElbowRight, JointType.WristRight),
                Tuple.Create(JointType.WristRight, JointType.HandRight),
                
                // Jambe gauche
                Tuple.Create(JointType.SpineBase, JointType.HipLeft),
                Tuple.Create(JointType.HipLeft, JointType.KneeLeft),
                Tuple.Create(JointType.KneeLeft, JointType.AnkleLeft),
                Tuple.Create(JointType.AnkleLeft, JointType.FootLeft),
                
                // Jambe droite
                Tuple.Create(JointType.SpineBase, JointType.HipRight),
                Tuple.Create(JointType.HipRight, JointType.KneeRight),
                Tuple.Create(JointType.KneeRight, JointType.AnkleRight),
                Tuple.Create(JointType.AnkleRight, JointType.FootRight)
            };

            foreach (var bone in bones)
            {
                DrawBone(body.Joints[bone.Item1], body.Joints[bone.Item2]);
            }
        }
        private void DrawBone(Joint joint1, Joint joint2)
        {
            if (joint1.TrackingState == TrackingState.NotTracked ||
                joint2.TrackingState == TrackingState.NotTracked)
                return;

            System.Windows.Point point1 = GetPoint(joint1.Position);
            System.Windows.Point point2 = GetPoint(joint2.Position);

            Line line = new Line
            {
                X1 = point1.X,
                Y1 = point1.Y,
                X2 = point2.X,
                Y2 = point2.Y,
                Stroke = Brushes.Green,
                StrokeThickness = 3
            };

            pDessinSquelette.Children.Add(line);
        }
        /// <summary>
        /// Événement lancé lorsqu'une image(Couleur, infrarouge ou profondeur) est créer par la Kinect et qu'elle est prête à être utilisée.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _multisourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame == null)
                return;
            switch (_currentDisplayFrameType)
            {
                case DisplayFrameType.Infrared:

                    break;
                case DisplayFrameType.Color:
                    using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                            ShowColorFrame(colorFrame);
                    }
                    break;
                case DisplayFrameType.Depth:
                    using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        if (depthFrame != null)
                            ShowColorFrame(depthFrame);
                    }
                    break;
                default:
                    break;
            }
        }

        private void ShowColorFrame(DepthFrame depthFrame)
        {
            FrameDescription frameDescription = null;
            ushort depth;
            byte r = 0;
            byte g = 0;
            byte b = 0;
            if (depthFrame != null)
            {
                frameDescription = depthFrame.FrameDescription;
                //Copier les pixels de données dans un image
                depthFrame.CopyFrameDataToArray(_picFrameData);
                int iIndexPicPixel = 0;
                for (int i = 0; i < _picFrameData.Length; i++)
                {
                    depth = _picFrameData[i];
                    if (depth < 1000)
                    {
                        r = (byte)(depth % 255);
                        g = 0;
                        b = 0;
                    }
                    else if (depth >= 1000 && depth < 2000)
                    {
                        r = 0;
                        g = (byte)(depth % 255);
                        b = 0;
                    }
                    else if (depth >= 2000 && depth < 3000)
                    {
                        r = 0;
                        g = 0;
                        b = (byte)(depth % 255);
                    }
                    else
                        r = b = g = 255;
                    _picPixels[iIndexPicPixel++] = b; // bleu
                    _picPixels[iIndexPicPixel++] = g; // vert
                    _picPixels[iIndexPicPixel++] = r; // rouge
                    _picPixels[iIndexPicPixel++] = 255; // alpga
                }
                RenderPixelArray(_picPixels, frameDescription);
            }

        }

        /// <summary>
        /// Événement lancé lorsque
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.Title = "kinect 2.0 : " + (this._kinectSensor.IsAvailable ? "Connecté" : "Non connecté");
        }



        /// <summary>
        /// Dessine une élispe sur le joint du squelette
        /// </summary>
        /// <param name="joint"></param>
        /// <param name="color"></param>
        /// <param name="size"></param>
        private void DrawJoint(Joint joint, System.Windows.Media.Color color, int size)
        {
            if (joint.Position.X != 0 && joint.Position.Y != 0 && joint.Position.Z != 0)
            {
                // Convertir la position du joint en coordonnées d'écran
                System.Windows.Point point = GetPoint(joint.Position);

                // Créer un cercle à la position du joint
                Ellipse ellipse = new Ellipse();
                ellipse.Fill = new SolidColorBrush(color);
                ellipse.Width = size;
                ellipse.Height = size;

                // Positionner le cercle sur l'élément de dessin Canvas
                Canvas.SetLeft(ellipse, point.X - size / 2);
                Canvas.SetTop(ellipse, point.Y - size / 2);

                // Ajouter le cercle à l'élément de dessin Canvas
                pDessinSquelette.Children.Add(ellipse);
            }
        }

        /// <summary>
        /// Convertit une position par rapport à la caméra(x,y) par rapport à une image(interface de l'utilisateur).
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public System.Windows.Point GetPoint(CameraSpacePoint position)
        {
            System.Windows.Point point = new System.Windows.Point();

            // Obtenir les coordonnées du point par rapport à la Kinect
            switch (_currentDisplayFrameType)
            {
                case DisplayFrameType.Color:
                    {
                        ColorSpacePoint colorPoint = _kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(position);
                        point.X = float.IsInfinity(colorPoint.X) ? 0.0 : colorPoint.X;
                        point.Y = float.IsInfinity(colorPoint.Y) ? 0.0 : colorPoint.Y;
                    }
                    break;
                case DisplayFrameType.Depth:
                case DisplayFrameType.Infrared:
                    {
                        DepthSpacePoint depthPoint = _kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(position);
                        point.X = float.IsInfinity(depthPoint.X) ? 0.0 : depthPoint.X;
                        point.Y = float.IsInfinity(depthPoint.Y) ? 0.0 : depthPoint.Y;
                    }
                    break;
                default:
                    break;
            }

            // Déterminer la résolution source en fonction du type d'affichage
            double sourceWidth = _currentDisplayFrameType == DisplayFrameType.Color ?
                _kinectSensor.ColorFrameSource.FrameDescription.Width :
                _kinectSensor.DepthFrameSource.FrameDescription.Width;

            double sourceHeight = _currentDisplayFrameType == DisplayFrameType.Color ?
                _kinectSensor.ColorFrameSource.FrameDescription.Height :
                _kinectSensor.DepthFrameSource.FrameDescription.Height;

            // Transformer les coordonnées pour qu'elles s'adaptent au canvas
            double scaleX = pDessinSquelette.Width / sourceWidth;
            double scaleY = pDessinSquelette.Height / sourceHeight;

            point.X = point.X * scaleX;
            point.Y = point.Y * scaleY;

            return point;
        }

        /// <summary>
        /// Configure les tableaux _picPixels et _picFrameData dans le bon format selon le type d'affichage. L'affichage Color est plus grand.
        /// </summary>
        /// <param name="newDisplayFrameType"></param>
        private void SetupCurrentDisplay(DisplayFrameType newDisplayFrameType)
        {
            _currentDisplayFrameType = newDisplayFrameType;
            FrameDescription frameDescription;

            switch (_currentDisplayFrameType)
            {
                case DisplayFrameType.Infrared:
                case DisplayFrameType.Depth:
                    frameDescription = _kinectSensor.InfraredFrameSource.FrameDescription;
                    _picPixels = new byte[frameDescription.Width * frameDescription.Height * 4];
                    _picFrameData = new ushort[frameDescription.Width * frameDescription.Height];
                    break;

                case DisplayFrameType.Color:
                    frameDescription = _kinectSensor.ColorFrameSource.FrameDescription;
                    _picPixels = new byte[frameDescription.Width * frameDescription.Height * 4];
                    _picFrameData = new ushort[frameDescription.Width * frameDescription.Height];
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Affiche une image de profondeur.
        /// </summary>
        /// <param name="depthFrame"></param>
        private void ShowDepthFrame(DepthFrame depthFrame)
        {


        }



        /// <summary>
        /// Affiche une image couleur
        /// </summary>
        /// <param name="colorFrame"></param>
        private void ShowColorFrame(ColorFrame colorFrame)
        {
            if (colorFrame != null)
            {
                FrameDescription frameDescription = colorFrame.FrameDescription;
                if (_bitmap == null)
                    _bitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, DPI, DPI, FORMAT, null);
                colorFrame.CopyConvertedFrameDataToArray(_picPixels, ColorImageFormat.Bgra);
                //Fonction utilitaire pour remplacer le contenu en mémoire plutot que de Créer un nouveau à chaque fois
                RenderPixelArray(_picPixels, frameDescription);

            }
        }

        /// <summary>
        /// Affiche une image infrarouge
        /// </summary>
        /// <param name="infraredFrame"></param>
        private void ShowInfraredFrame(InfraredFrame infraredFrame)
        {

        }

        /// <summary>
        /// Convertit un tableau de données en une tableau de pixel
        /// </summary>
        private void ConvertInfraredDataToPixels()
        {
            // Convert the infrared to RGB
            int colorPixelIndex = 0;
            for (int i = 0; i < this._picFrameData.Length; ++i)
            {
                // normalize the incoming infrared data (ushort) to a float ranging from 
                // [InfraredOutputValueMinimum, InfraredOutputValueMaximum] by
                // 1. dividing the incoming value by the source maximum value
                float intensityRatio = (float)this._picFrameData[i] / _fInfraredSourceValueMaximum;

                // 2. dividing by the (average scene value * standard deviations)
                intensityRatio /= _fInfraredSceneValueAverage * _fInfraredSceneStandardDeviations;

                // 3. limiting the value to InfraredOutputValueMaximum
                intensityRatio = Math.Min(_fInfraredOutputValueMaximum, intensityRatio);

                // 4. limiting the lower value InfraredOutputValueMinimum
                intensityRatio = Math.Max(_fInfraredOutputValueMinimum, intensityRatio);

                // 5. converting the normalized value to a byte and using the result
                // as the RGB components required by the image
                byte intensity = (byte)(intensityRatio * 255.0f);
                _picPixels[colorPixelIndex++] = intensity; //Blue
                _picPixels[colorPixelIndex++] = intensity; //Green
                _picPixels[colorPixelIndex++] = intensity; //Red
                _picPixels[colorPixelIndex++] = 255;       //Alpha
            }
        }

        /// <summary>
        /// Optimisation utilisé pour remplacer en mémoire l'image de pixel plutot que de recréer une nouvelle image et de l'assigner à imgCameraKinet
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="currentFrameDescription"></param>
        private void RenderPixelArray(byte[] pixels, FrameDescription currentFrameDescription)
        {
            _bitmap.Lock();
            _bitmap.WritePixels(new Int32Rect(0, 0, currentFrameDescription.Width, currentFrameDescription.Height), pixels, currentFrameDescription.Width * 4, 0);
            _bitmap.Unlock();
            picKinect.Source = _bitmap;
        }


        /// <summary>
        /// Modifier le type d'affichage pour infrarouge.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InfraredButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Infrared);
        }

        /// <summary>
        /// Modifier le type d'affichage pour couleur.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Color);
        }

        /// <summary>
        /// Modifier le type d'affichage pour profondeur.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepthButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Depth);
        }

        /// <summary>
        /// Fermer la connexion à la Kinect si on ferme l'écran.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_kinectSensor.IsAvailable)
                _kinectSensor.Close();
        }
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame == null)
                return;
            using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                    ShowColorFrame(colorFrame);
            }

        }

        private void picRetour_MouseLeave(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }
        private void picRetour_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// Change le curseur lorsque le curseur est sur l'image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picRetour_MouseHover(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }
        /// <summary>
        /// Apprentissage avec la position obtenu à partir de la Kinect versus l'image affichée.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApprendre_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBody != null && _currentBody.IsTracked)
            {
                _presenteur.ApprendrePosition(_currentBody.Joints);
                txtConsole.Text = "Position " + _presenteur.PositionEnCours + " apprise !";
            }
            else
            {
                txtConsole.Text = "Aucun squelette détecté. Placez-vous devant la Kinect.";
            }

        }
        /// <summary>
        /// Lorsqu'on appuie sur le bouton suivant ou précédent, modifier la figure en conséquence.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// Charger la figure de danse en cours.
        /// </summary>
        private void ChargerFigure()
        {
            BitmapImage imgValue;
            bool bResultat;

            if (_positionEnCours > CstApplication.NBFIGURE)
                _positionEnCours = 1;

            if (_positionEnCours < 1)
                _positionEnCours = CstApplication.NBFIGURE;

            lblFigureEnCours.Content = _positionEnCours.ToString();

            bResultat = _dicImgFigure.TryGetValue("fig" + _positionEnCours, out imgValue);
            if (bResultat == true)
                picPositionAFaire.Source = imgValue;

        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _presenteur.DesactiverReconnaissanceVocale();
        }

        private void Presenteur_CommandeVocaleDetectee(object sender, EventArgs e)
        {
            // Feedback visuel
            System.Windows.Media.Animation.Storyboard animation = (System.Windows.Media.Animation.Storyboard)FindResource("HoyDetectedAnimation");
            animation.Begin();

            if (_currentBody != null && _currentBody.IsTracked)
            {
                _presenteur.ApprendrePosition(_currentBody.Joints);
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
    }
}



