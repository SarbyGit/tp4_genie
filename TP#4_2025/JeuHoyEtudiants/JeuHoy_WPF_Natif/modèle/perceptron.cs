using JeuHoy_WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeuHoy_WPF_Natif.modèle
{
    internal class Perceptron
    {
        private double[] _poids;
        private double _biais;
        private double _tauxApprentissage;
        private int _positionCible;
        private int _nbEntrees;

        public Perceptron(int positionCible, int nbEntrees)
        {
            _positionCible = positionCible;
            _nbEntrees = nbEntrees;
            _poids = new double[nbEntrees];
            _biais = 0;
            _tauxApprentissage = CstApplication.CONSTANTEAPPRENTISSAGE;

            // Initialisation des poids avec des valeurs aléatoires entre -0.5 et 0.5
            Random rand = new Random();
            for (int i = 0; i < nbEntrees; i++)
            {
                _poids[i] = rand.NextDouble() - 0.5;
            }
        }

        /// <summary>
        /// Calcule la somme pondérée des entrées
        /// </summary>
        private double CalculerSomme(double[] entrees)
        {
            double somme = _biais;
            for (int i = 0; i < _nbEntrees; i++)
            {
                somme += entrees[i] * _poids[i];
            }
            return somme;
        }

        /// <summary>
        /// Fonction d'activation (sigmoïde)
        /// </summary>
        private int Activation(double somme)
        {
            return somme >= 0 ? CstApplication.VRAI : CstApplication.FAUX;
        }

        /// <summary>
        /// Calcule la sortie du perceptron pour des entrées données
        /// </summary>
        public int Calculer(double[] entrees)
        {
            if (entrees.Length != _nbEntrees)
                throw new ArgumentException("Le nombre d'entrées doit correspondre au nombre de poids");

            double somme = CalculerSomme(entrees);
            return Activation(somme);
        }

        /// <summary>
        /// Entraîne le perceptron avec un exemple
        /// </summary>
        public void Apprendre(double[] entrees, bool estPositionCible)
        {
            int sortieAttendue = estPositionCible ? CstApplication.VRAI : CstApplication.FAUX;
            int sortieCalculee = Calculer(entrees);

            // Si la sortie est correcte, pas besoin d'ajuster les poids
            if (sortieCalculee == sortieAttendue)
                return;

            // Ajustement des poids
            int erreur = sortieAttendue - sortieCalculee;
            for (int i = 0; i < _nbEntrees; i++)
            {
                _poids[i] += _tauxApprentissage * erreur * entrees[i];
            }

            // Ajustement du biais
            _biais += _tauxApprentissage * erreur;
        }

        /// <summary>
        /// Sauvegarde le perceptron dans un fichier
        /// </summary>
        public void Sauvegarder(string nomFichier)
        {
            using (StreamWriter writer = new StreamWriter(nomFichier))
            {
                writer.WriteLine(_positionCible);
                writer.WriteLine(_biais);
                for (int i = 0; i < _nbEntrees; i++)
                {
                    writer.WriteLine(_poids[i]);
                }
            }
        }

        /// <summary>
        /// Charge le perceptron depuis un fichier
        /// </summary>
        public void Charger(string nomFichier)
        {
            if (!File.Exists(nomFichier))
                return;

            using (StreamReader reader = new StreamReader(nomFichier))
            {
                string line = reader.ReadLine();
                if (line != null)
                    _positionCible = int.Parse(line);

                line = reader.ReadLine();
                if (line != null)
                    _biais = double.Parse(line);

                for (int i = 0; i < _nbEntrees && !reader.EndOfStream; i++)
                {
                    line = reader.ReadLine();
                    if (line != null)
                        _poids[i] = double.Parse(line);
                }
            }
        }

        public int PositionCible => _positionCible;
    }
}
