using JeuHoy_WPF;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeuHoy_WPF_Natif.modèle
{
    internal class GestionnairePerceptron
    {
        private const string DOSSIER_PERCEPTRONS = "Perceptrons";
        private List<Perceptron> _perceptrons;
        private int _tailleEntree;

        public GestionnairePerceptron()
        {
            _perceptrons = new List<Perceptron>();
            _tailleEntree = JointDataConverter.JointsImportants.Length * 3; // 3 coordonnées par joint

            // Créer le dossier des perceptrons s'il n'existe pas
            if (!Directory.Exists(DOSSIER_PERCEPTRONS))
                Directory.CreateDirectory(DOSSIER_PERCEPTRONS);

            // Charger ou créer les perceptrons pour chaque position
            for (int i = 1; i <= CstApplication.NBFIGURE; i++)
            {
                Perceptron p = new Perceptron(i, _tailleEntree);
                string fichier = Path.Combine(DOSSIER_PERCEPTRONS, $"perceptron_{i}.dat");

                if (File.Exists(fichier))
                    p.Charger(fichier);

                _perceptrons.Add(p);
            }
        }

        /// <summary>
        /// Entraîne les perceptrons pour une position spécifique
        /// </summary>
        public void ApprendrePosition(IReadOnlyDictionary<JointType, Joint> joints, int positionCible)
        {
            double[] entrees = JointDataConverter.ConvertirJointsEnEntrees(joints);

            foreach (Perceptron p in _perceptrons)
            {
                bool estPositionCible = (p.PositionCible == positionCible);
                p.Apprendre(entrees, estPositionCible);

                // Sauvegarder le perceptron après apprentissage
                string fichier = Path.Combine(DOSSIER_PERCEPTRONS, $"perceptron_{p.PositionCible}.dat");
                p.Sauvegarder(fichier);
            }
        }

        /// <summary>
        /// Identifie la position actuelle à partir des joints
        /// </summary>
        public List<int> IdentifierPosition(IReadOnlyDictionary<JointType, Joint> joints)
        {
            double[] entrees = JointDataConverter.ConvertirJointsEnEntrees(joints);
            List<int> positionsReconnues = new List<int>();

            foreach (Perceptron p in _perceptrons)
            {
                int resultat = p.Calculer(entrees);
                if (resultat == CstApplication.VRAI)
                {
                    positionsReconnues.Add(p.PositionCible);
                }
            }

            return positionsReconnues;
        }
    }
}
