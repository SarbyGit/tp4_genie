using JeuHoy_WPF;
using JeuHoy_WPF.modèle;
using JeuHoy_WPF.vue;
using JeuHoy_WPF_Natif.modèle;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JeuHoy_WPF_Natif.présentation
{
    internal class EntrainementPresenteur
    {
        private GestionnairePerceptron _gestionnairePerceptrons;
        private Dictionary<string, BitmapImage> _imagesFigures;
        private int _positionEnCours;
        private ReconnaissanceVocale _reconnaissanceVocale;
        private KinectSensor _kinectSensor;
        private Canvas _canvasSquelette;
        private DisplayFrameType _currentDisplayFrameType;
        private Body _currentBody = null;
        private WriteableBitmap _bitmap = null;
        private ushort[] _picFrameData = null;
        private byte[] _picPixels = null;

        public event EventHandler CommandeVocaleDetectee;

        public EntrainementPresenteur()
        {
            _gestionnairePerceptrons = new GestionnairePerceptron();
            _imagesFigures = new Dictionary<string, BitmapImage>();
            _positionEnCours = 1;
            //Reconnaissance vocale
            _reconnaissanceVocale = new ReconnaissanceVocale();
            _reconnaissanceVocale.CommandeDetectee += ReconnaissanceVocale_CommandeDetectee;

            // Charger les images des figures
            ChargerImagesFigures();
        }

        // Méthode pour initialiser les ressources Kinect
        public void InitKinect(KinectSensor kinectSensor, Canvas canvasSquelette, DisplayFrameType displayFrameType)
        {
            _kinectSensor = kinectSensor;
            _canvasSquelette = canvasSquelette;
            _currentDisplayFrameType = displayFrameType;
        }

        // Méthode pour traiter les données du squelette
        public string TraiterDonneesSquelette(Body[] squelettes)
        {
            _canvasSquelette.Children.Clear();
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
                    return IdentifierPosition(squelette.Joints);
                }
            }

            return "Aucun squelette détecté";
        }

        // Méthodes pour dessiner le squelette
        public void DrawJoint(Joint joint, Color color, int size)
        {
            if (joint.Position.X != 0 && joint.Position.Y != 0 && joint.Position.Z != 0)
            {
                // Convertir la position du joint en coordonnées d'écran
                Point point = GetPoint(joint.Position);

                // Créer un cercle à la position du joint
                Ellipse ellipse = new Ellipse();
                ellipse.Fill = new SolidColorBrush(color);
                ellipse.Width = size;
                ellipse.Height = size;

                // Positionner le cercle sur l'élément de dessin Canvas
                Canvas.SetLeft(ellipse, point.X - size / 2);
                Canvas.SetTop(ellipse, point.Y - size / 2);

                // Ajouter le cercle à l'élément de dessin Canvas
                _canvasSquelette.Children.Add(ellipse);
            }
        }

        public void DrawBone(Joint joint1, Joint joint2)
        {
            if (joint1.TrackingState == TrackingState.NotTracked ||
                joint2.TrackingState == TrackingState.NotTracked)
                return;

            Point point1 = GetPoint(joint1.Position);
            Point point2 = GetPoint(joint2.Position);

            Line line = new Line
            {
                X1 = point1.X,
                Y1 = point1.Y,
                X2 = point2.X,
                Y2 = point2.Y,
                Stroke = Brushes.Green,
                StrokeThickness = 3
            };

            _canvasSquelette.Children.Add(line);
        }

        public void DrawBones(Body body)
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

        // Méthode pour convertir les coordonnées
        public Point GetPoint(CameraSpacePoint position)
        {
            Point point = new Point();

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
            double scaleX = _canvasSquelette.Width / sourceWidth;
            double scaleY = _canvasSquelette.Height / sourceHeight;

            point.X = point.X * scaleX;
            point.Y = point.Y * scaleY;

            return point;
        }

        // Méthodes pour les frames
        public void SetupCurrentDisplay(DisplayFrameType newDisplayFrameType)
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

        // Méthodes pour traiter les images
        public WriteableBitmap ShowColorFrame(ColorFrame colorFrame)
        {
            if (colorFrame != null)
            {
                FrameDescription frameDescription = colorFrame.FrameDescription;
                if (_bitmap == null)
                    _bitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                colorFrame.CopyConvertedFrameDataToArray(_picPixels, ColorImageFormat.Bgra);

                RenderPixelArray(_picPixels, frameDescription);
            }
            return _bitmap;
        }

        public WriteableBitmap ShowDepthFrame(DepthFrame depthFrame)
        {
            FrameDescription frameDescription = null;
            ushort depth;
            byte r = 0;
            byte g = 0;
            byte b = 0;
            if (depthFrame != null)
            {
                frameDescription = depthFrame.FrameDescription;
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
            return _bitmap;
        }

        private void RenderPixelArray(byte[] pixels, FrameDescription currentFrameDescription)
        {
            _bitmap.Lock();
            _bitmap.WritePixels(new Int32Rect(0, 0, currentFrameDescription.Width, currentFrameDescription.Height), pixels, currentFrameDescription.Width * 4, 0);
            _bitmap.Unlock();
        }

        // Méthodes existantes
        private void ChargerImagesFigures()
        {
            for (int i = 1; i <= CstApplication.NBFIGURE; i++)
            {
                string chemin = $"../Resources/fig{i}.png";
                BitmapImage image = new BitmapImage(new System.Uri(chemin, System.UriKind.Relative));
                _imagesFigures[$"fig{i}"] = image;
            }
        }

        public void FigureSuivante()
        {
            _positionEnCours++;
            if (_positionEnCours > CstApplication.NBFIGURE)
                _positionEnCours = 1;
        }

        public void FigurePrecedente()
        {
            _positionEnCours--;
            if (_positionEnCours < 1)
                _positionEnCours = CstApplication.NBFIGURE;
        }

        public void ApprendrePosition(IReadOnlyDictionary<JointType, Joint> joints)
        {
            _gestionnairePerceptrons.ApprendrePosition(joints, _positionEnCours);
        }

        public string IdentifierPosition(IReadOnlyDictionary<JointType, Joint> joints)
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

        public int PositionEnCours => _positionEnCours;

        public BitmapImage GetImageFigureEnCours()
        {
            string cle = $"fig{_positionEnCours}";
            if (_imagesFigures.ContainsKey(cle))
                return _imagesFigures[cle];

            return null;
        }

        private void ReconnaissanceVocale_CommandeDetectee(object sender, EventArgs e)
        {
            JouerSon joueurSon = new JouerSon();
            joueurSon.JouerSonAsync("./Resources/HoyContent/hooy.wav");

            // Déclencher un événement pour notifier la vue
            CommandeVocaleDetectee?.Invoke(this, EventArgs.Empty);
        }

        public void ActiverReconnaissanceVocale()
        {
            _reconnaissanceVocale.DemarrerEcoute();
        }

        public void DesactiverReconnaissanceVocale()
        {
            _reconnaissanceVocale.ArreterEcoute();
        }

        public Body CurrentBody => _currentBody;

        public void Dispose()
        {
            if (_reconnaissanceVocale != null)
            {
                _reconnaissanceVocale.Dispose();
            }
        }
    }
}