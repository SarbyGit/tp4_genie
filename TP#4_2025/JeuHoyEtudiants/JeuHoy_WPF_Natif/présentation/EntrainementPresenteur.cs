// EntrainementPresenteur.cs
using JeuHoy_WPF;
using JeuHoy_WPF.modèle;
using JeuHoy_WPF.vue;
using JeuHoy_WPF_Natif.modèle;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JeuHoy_WPF_Natif.présentation
{
    internal class EntrainementPresenteur
    {
        // Référence à l'interface de la vue
        private readonly IEntrainementVue _vue;

        // Objets du modèle
        private GestionnairePerceptron _gestionnairePerceptrons;
        private ReconnaissanceVocale _reconnaissanceVocale;
        private Dictionary<string, BitmapImage> _imagesFigures;

        // État de l'application
        private int _positionEnCours;
        private Body _currentBody = null;

        // Objets liés à Kinect
        private KinectSensor _kinectSensor;
        private DisplayFrameType _currentDisplayFrameType;
        private WriteableBitmap _bitmap = null;
        private ushort[] _picFrameData = null;
        private byte[] _picPixels = null;

        public EntrainementPresenteur(IEntrainementVue vue)
        {
            _vue = vue ?? throw new ArgumentNullException(nameof(vue));

            // Initialisation des composants
            _gestionnairePerceptrons = new GestionnairePerceptron();
            _imagesFigures = new Dictionary<string, BitmapImage>();
            _positionEnCours = 1;

            // Initialisation de la reconnaissance vocale
            _reconnaissanceVocale = new ReconnaissanceVocale();
            _reconnaissanceVocale.CommandeDetectee += ReconnaissanceVocale_CommandeDetectee;

            // Chargement des ressources
            ChargerImagesFigures();

            // Mise à jour initiale de l'affichage
            MettreAJourAffichageFigure();
        }

        /// <summary>
        /// Initialise la Kinect et configure le type d'affichage
        /// </summary>
        public void InitialiserKinect(KinectSensor kinectSensor, DisplayFrameType displayFrameType)
        {
            _kinectSensor = kinectSensor ?? throw new ArgumentNullException(nameof(kinectSensor));
            _currentDisplayFrameType = displayFrameType;
            ConfigurerAffichage(displayFrameType);
        }

        /// <summary>
        /// Configure les buffers pour le type d'affichage spécifié
        /// </summary>
        public void ConfigurerAffichage(DisplayFrameType displayFrameType)
        {
            if (_kinectSensor == null)
                return;

            _currentDisplayFrameType = displayFrameType;
            _vue.ChangerModeAffichage(displayFrameType);

            FrameDescription frameDescription;

            switch (_currentDisplayFrameType)
            {
                case DisplayFrameType.Infrared:
                case DisplayFrameType.Depth:
                    frameDescription = _kinectSensor.InfraredFrameSource.FrameDescription;
                    break;
                case DisplayFrameType.Color:
                default:
                    frameDescription = _kinectSensor.ColorFrameSource.FrameDescription;
                    break;
            }

            // Redimensionner les buffers
            _picPixels = new byte[frameDescription.Width * frameDescription.Height * 4];
            _picFrameData = new ushort[frameDescription.Width * frameDescription.Height];

            // Réinitialiser bitmap
            _bitmap = null;
        }

        /// <summary>
        /// Traite les données du squelette et met à jour l'interface
        /// </summary>
        public void TraiterDonneesSquelette(Body[] squelettes)
        {
            if (squelettes == null)
                return;

            _vue.EffacerSquelette();
            _currentBody = null;

            // Recherche du premier squelette suivi
            foreach (Body squelette in squelettes)
            {
                if (squelette.IsTracked)
                {
                    _currentBody = squelette;

                    // Dessiner les joints
                    foreach (Joint joint in squelette.Joints.Values)
                    {
                        if (joint.TrackingState != TrackingState.NotTracked)
                        {
                            Point point = ConvertirPositionEnPoint(joint.Position);
                            _vue.DessinerJoint(point, Colors.BlueViolet, 10);
                        }
                    }

                    // Dessiner les os
                    DessinerOs(squelette);

                    // Mettre à jour la console avec l'identification de position
                    string message = IdentifierPosition(squelette.Joints);
                    _vue.MettreAJourConsole(message);

                    break; // On ne traite que le premier squelette
                }
            }

            if (_currentBody == null)
            {
                _vue.MettreAJourConsole("Aucun squelette détecté");
            }
        }

        /// <summary>
        /// Dessine les os du squelette
        /// </summary>
        private void DessinerOs(Body corps)
        {
            if (corps == null)
                return;

            // Liste des paires de joints qui forment les os
            var os = new Tuple<JointType, JointType>[]
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

            // Dessiner chaque os
            foreach (var paire in os)
            {
                DessinerOs(corps.Joints[paire.Item1], corps.Joints[paire.Item2]);
            }
        }

        /// <summary>
        /// Dessine un os entre deux joints
        /// </summary>
        private void DessinerOs(Joint joint1, Joint joint2)
        {
            // Vérifier que les deux joints sont suivis
            if (joint1.TrackingState == TrackingState.NotTracked ||
                joint2.TrackingState == TrackingState.NotTracked)
                return;

            // Convertir les coordonnées
            Point point1 = ConvertirPositionEnPoint(joint1.Position);
            Point point2 = ConvertirPositionEnPoint(joint2.Position);

            // Déléguer le dessin à la vue
            _vue.DessinerLigne(point1, point2, Colors.Green, 3);
        }

        /// <summary>
        /// Convertit une position 3D de la Kinect en position 2D pour l'affichage
        /// </summary>
        private Point ConvertirPositionEnPoint(CameraSpacePoint position)
        {
            if (_kinectSensor == null)
                return new Point();

            Point point = new Point();

            // Obtenir les coordonnées du point selon le mode d'affichage
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
                default:
                    {
                        DepthSpacePoint depthPoint = _kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(position);
                        point.X = float.IsInfinity(depthPoint.X) ? 0.0 : depthPoint.X;
                        point.Y = float.IsInfinity(depthPoint.Y) ? 0.0 : depthPoint.Y;
                    }
                    break;
            }

            // Déterminer la résolution source
            double sourceWidth = _currentDisplayFrameType == DisplayFrameType.Color ?
                _kinectSensor.ColorFrameSource.FrameDescription.Width :
                _kinectSensor.DepthFrameSource.FrameDescription.Width;

            double sourceHeight = _currentDisplayFrameType == DisplayFrameType.Color ?
                _kinectSensor.ColorFrameSource.FrameDescription.Height :
                _kinectSensor.DepthFrameSource.FrameDescription.Height;

            // Adapter les coordonnées au canvas
            double scaleX = _vue.LargeurCanvasSquelette / sourceWidth;
            double scaleY = _vue.HauteurCanvasSquelette / sourceHeight;

            point.X = point.X * scaleX;
            point.Y = point.Y * scaleY;

            return point;
        }

        /// <summary>
        /// Traite une frame de couleur et met à jour l'affichage
        /// </summary>
        public void TraiterFrameCouleur(ColorFrame frame)
        {
            if (frame == null)
                return;

            FrameDescription description = frame.FrameDescription;

            // Initialiser le bitmap si nécessaire
            if (_bitmap == null)
            {
                _bitmap = new WriteableBitmap(
                    description.Width,
                    description.Height,
                    96.0, 96.0,
                    PixelFormats.Bgra32,
                    null);
            }

            // Copier les données de la frame
            frame.CopyConvertedFrameDataToArray(_picPixels, ColorImageFormat.Bgra);

            // Mettre à jour le bitmap
            _bitmap.Lock();
            _bitmap.WritePixels(
                new Int32Rect(0, 0, description.Width, description.Height),
                _picPixels,
                description.Width * 4,
                0);
            _bitmap.Unlock();

            // Afficher l'image
            _vue.AfficherImage(_bitmap);
        }

        /// <summary>
        /// Traite une frame de profondeur et met à jour l'affichage
        /// </summary>
        public void TraiterFrameProfondeur(DepthFrame frame)
        {
            if (frame == null)
                return;

            FrameDescription description = frame.FrameDescription;

            // Initialiser le bitmap si nécessaire
            if (_bitmap == null)
            {
                _bitmap = new WriteableBitmap(
                    description.Width,
                    description.Height,
                    96.0, 96.0,
                    PixelFormats.Bgra32,
                    null);
            }

            // Copier les données de la frame
            frame.CopyFrameDataToArray(_picFrameData);

            // Convertir les données de profondeur en pixels BGRA
            int pixelIndex = 0;
            for (int i = 0; i < _picFrameData.Length; i++)
            {
                ushort profondeur = _picFrameData[i];
                byte r = 0, g = 0, b = 0;

                // Coder la profondeur par couleur
                if (profondeur < 1000)
                {
                    r = (byte)(profondeur % 255);
                }
                else if (profondeur < 2000)
                {
                    g = (byte)(profondeur % 255);
                }
                else if (profondeur < 3000)
                {
                    b = (byte)(profondeur % 255);
                }
                else
                {
                    r = g = b = 255;
                }

                _picPixels[pixelIndex++] = b;  // Bleu
                _picPixels[pixelIndex++] = g;  // Vert
                _picPixels[pixelIndex++] = r;  // Rouge
                _picPixels[pixelIndex++] = 255; // Alpha
            }

            // Mettre à jour le bitmap
            _bitmap.Lock();
            _bitmap.WritePixels(
                new Int32Rect(0, 0, description.Width, description.Height),
                _picPixels,
                description.Width * 4,
                0);
            _bitmap.Unlock();

            // Afficher l'image
            _vue.AfficherImage(_bitmap);
        }

        /// <summary>
        /// Traite une frame infrarouge et met à jour l'affichage
        /// </summary>
        public void TraiterFrameInfrarouge(InfraredFrame frame)
        {
            if (frame == null)
                return;

            FrameDescription description = frame.FrameDescription;

            // Initialiser le bitmap si nécessaire
            if (_bitmap == null)
            {
                _bitmap = new WriteableBitmap(
                    description.Width,
                    description.Height,
                    96.0, 96.0,
                    PixelFormats.Bgra32,
                    null);
            }

            // Copier les données de la frame
            frame.CopyFrameDataToArray(_picFrameData);

            // Constantes pour normalisation
            const float InfraredSourceValueMaximum = ushort.MaxValue;
            const float InfraredOutputValueMinimum = 0.01f;
            const float InfraredOutputValueMaximum = 1.0f;
            const float InfraredSceneValueAverage = 0.08f;
            const float InfraredSceneValueStandardDeviations = 3.0f;

            // Convertir en pixels BGRA
            int pixelIndex = 0;
            for (int i = 0; i < _picFrameData.Length; i++)
            {
                // Normaliser la valeur infrarouge
                float intensityRatio = (float)_picFrameData[i] / InfraredSourceValueMaximum;
                intensityRatio /= InfraredSceneValueAverage * InfraredSceneValueStandardDeviations;
                intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);
                intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

                // Convertir en valeur de pixel
                byte intensity = (byte)(intensityRatio * 255.0f);

                _picPixels[pixelIndex++] = intensity; // Bleu
                _picPixels[pixelIndex++] = intensity; // Vert
                _picPixels[pixelIndex++] = intensity; // Rouge
                _picPixels[pixelIndex++] = 255;       // Alpha
            }

            // Mettre à jour le bitmap
            _bitmap.Lock();
            _bitmap.WritePixels(
                new Int32Rect(0, 0, description.Width, description.Height),
                _picPixels,
                description.Width * 4,
                0);
            _bitmap.Unlock();

            // Afficher l'image
            _vue.AfficherImage(_bitmap);
        }

        /// <summary>
        /// Traite une frame multi-source
        /// </summary>
        public void TraiterMultiSourceFrame(MultiSourceFrame frame)
        {
            if (frame == null)
                return;

            switch (_currentDisplayFrameType)
            {
                case DisplayFrameType.Color:
                    using (ColorFrame colorFrame = frame.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                            TraiterFrameCouleur(colorFrame);
                    }
                    break;

                case DisplayFrameType.Depth:
                    using (DepthFrame depthFrame = frame.DepthFrameReference.AcquireFrame())
                    {
                        if (depthFrame != null)
                            TraiterFrameProfondeur(depthFrame);
                    }
                    break;

                case DisplayFrameType.Infrared:
                    using (InfraredFrame infraFrame = frame.InfraredFrameReference.AcquireFrame())
                    {
                        if (infraFrame != null)
                            TraiterFrameInfrarouge(infraFrame);
                    }
                    break;
            }
        }

        /// <summary>
        /// Charge les images des figures depuis les ressources
        /// </summary>
        private void ChargerImagesFigures()
        {
            for (int i = 1; i <= CstApplication.NBFIGURE; i++)
            {
                string chemin = $"../Resources/fig{i}.png";
                BitmapImage image = new BitmapImage(new Uri(chemin, UriKind.Relative));
                _imagesFigures[$"fig{i}"] = image;
            }
        }

        /// <summary>
        /// Passe à la figure suivante
        /// </summary>
        public void FigureSuivante()
        {
            _positionEnCours++;
            if (_positionEnCours > CstApplication.NBFIGURE)
                _positionEnCours = 1;

            MettreAJourAffichageFigure();
        }

        /// <summary>
        /// Passe à la figure précédente
        /// </summary>
        public void FigurePrecedente()
        {
            _positionEnCours--;
            if (_positionEnCours < 1)
                _positionEnCours = CstApplication.NBFIGURE;

            MettreAJourAffichageFigure();
        }

        /// <summary>
        /// Met à jour l'affichage de la figure courante
        /// </summary>
        private void MettreAJourAffichageFigure()
        {
            BitmapImage image = null;
            string cle = $"fig{_positionEnCours}";

            if (_imagesFigures.ContainsKey(cle))
                image = _imagesFigures[cle];

            _vue.AfficherFigureEnCours(_positionEnCours, image);
        }

        /// <summary>
        /// Apprend la position actuelle avec les joints détectés
        /// </summary>
        public void ApprendrePosition()
        {
            if (_currentBody == null || !_currentBody.IsTracked)
            {
                _vue.MettreAJourConsole("Aucun squelette détecté. Placez-vous devant la Kinect.");
                return;
            }

            _gestionnairePerceptrons.ApprendrePosition(_currentBody.Joints, _positionEnCours);
            _vue.MettreAJourConsole($"Position {_positionEnCours} apprise !");
        }

        /// <summary>
        /// Identifie la position à partir des joints détectés
        /// </summary>
        private string IdentifierPosition(IReadOnlyDictionary<JointType, Joint> joints)
        {
            List<int> positionsReconnues = _gestionnairePerceptrons.IdentifierPosition(joints);

            if (positionsReconnues.Count == 0)
                return "Aucune position reconnue";

            string resultat = "Position détectée : ";
            foreach (int position in positionsReconnues)
            {
                resultat += position + " ";
            }

            return resultat;
        }

        /// <summary>
        /// Gère l'événement de détection d'une commande vocale
        /// </summary>
        private void ReconnaissanceVocale_CommandeDetectee(object sender, EventArgs e)
        {
            // Jouer un son
            JouerSon joueurSon = new JouerSon();
            joueurSon.JouerSonAsync("./Resources/HoyContent/hooy.wav");

            // Animation
            _vue.DemarrerAnimationReconnaissanceVocale();

            // Apprentissage de position
            if (_currentBody != null && _currentBody.IsTracked)
            {
                _gestionnairePerceptrons.ApprendrePosition(_currentBody.Joints, _positionEnCours);
                _vue.MettreAJourConsole($"Position {_positionEnCours} validée par commande vocale !");
            }
            else
            {
                _vue.MettreAJourConsole("Commande vocale détectée, mais aucun squelette n'est suivi.");
            }

            // Propager l'événement
            CommandeVocaleDetectee?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Active la reconnaissance vocale
        /// </summary>
        public void ActiverReconnaissanceVocale()
        {
            _reconnaissanceVocale.DemarrerEcoute();
            _vue.ChangerEtatReconnaissanceVocale(true, new SolidColorBrush(Colors.Green), Visibility.Visible);
        }

        /// <summary>
        /// Désactive la reconnaissance vocale
        /// </summary>
        public void DesactiverReconnaissanceVocale()
        {
            _reconnaissanceVocale.ArreterEcoute();
            _vue.ChangerEtatReconnaissanceVocale(false, new SolidColorBrush(Colors.Gray), Visibility.Collapsed);
        }

        /// <summary>
        /// Libère les ressources
        /// </summary>
        public void Dispose()
        {
            _reconnaissanceVocale?.Dispose();
        }

        /// <summary>
        /// Événement déclenché lorsqu'une commande vocale est détectée
        /// </summary>
        public event EventHandler CommandeVocaleDetectee;
    }
}