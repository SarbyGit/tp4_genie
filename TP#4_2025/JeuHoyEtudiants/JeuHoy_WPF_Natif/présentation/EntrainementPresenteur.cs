using JeuHoy_WPF;
using JeuHoy_WPF.modèle;
using JeuHoy_WPF_Natif.modèle;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace JeuHoy_WPF_Natif.présentation
{
    internal class EntrainementPresenteur
    {
        private GestionnairePerceptron _gestionnairePerceptrons;
        private Dictionary<string, BitmapImage> _imagesFigures;
        private int _positionEnCours;

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

        /// <summary>
        /// Charge les images des figures depuis les ressources
        /// </summary>
        private void ChargerImagesFigures()
        {
            for (int i = 1; i <= CstApplication.NBFIGURE; i++)
            {
                string chemin = $"../Resources/fig{i}.png";
                BitmapImage image = new BitmapImage(new System.Uri(chemin, System.UriKind.Relative));
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
        }

        /// <summary>
        /// Passe à la figure précédente
        /// </summary>
        public void FigurePrecedente()
        {
            _positionEnCours--;
            if (_positionEnCours < 1)
                _positionEnCours = CstApplication.NBFIGURE;
        }

        /// <summary>
        /// Apprend la position actuelle avec les joints détectés
        /// </summary>
        public void ApprendrePosition(IReadOnlyDictionary<JointType, Joint> joints)
        {
            _gestionnairePerceptrons.ApprendrePosition(joints, _positionEnCours);
        }

        /// <summary>
        /// Identifie la position à partir des joints détectés
        /// </summary>
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
        // Ajouter ce champ privé
        private ReconnaissanceVocale _reconnaissanceVocale;
        public event EventHandler CommandeVocaleDetectee;


        // Ajouter cette méthode pour traiter la commande vocale
        private void ReconnaissanceVocale_CommandeDetectee(object sender, EventArgs e)
        {
            JouerSon joueurSon = new JouerSon();
            joueurSon.JouerSonAsync("./Resources/HoyContent/hooy.wav");

            // Déclencher un événement pour notifier la vue
            CommandeVocaleDetectee?.Invoke(this, EventArgs.Empty);
        }

        // Ajouter des méthodes pour activer/désactiver la reconnaissance
        public void ActiverReconnaissanceVocale()
        {
            _reconnaissanceVocale.DemarrerEcoute();
        }

        public void DesactiverReconnaissanceVocale()
        {
            _reconnaissanceVocale.ArreterEcoute();
        }

        // Dans la méthode Dispose ou dans une méthode appelée à la fermeture
        public void Dispose()
        {
            if (_reconnaissanceVocale != null)
            {
                _reconnaissanceVocale.Dispose();
            }
        }
    }
}
