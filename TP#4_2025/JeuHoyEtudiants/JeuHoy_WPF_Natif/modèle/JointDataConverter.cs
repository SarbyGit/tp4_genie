using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeuHoy_WPF_Natif.modèle
{
    internal class JointDataConverter
    {
        /// <summary>
        /// Liste des joints importants pour la reconnaissance
        /// </summary>
        public static JointType[] JointsImportants = new JointType[]
        {
            JointType.Head,
            JointType.Neck,
            JointType.SpineShoulder,
            JointType.SpineMid,
            JointType.SpineBase,
            JointType.ShoulderLeft,
            JointType.ShoulderRight,
            JointType.ElbowLeft,
            JointType.ElbowRight,
            JointType.WristLeft,
            JointType.WristRight,
            JointType.HandLeft,
            JointType.HandRight,
            JointType.HipLeft,
            JointType.HipRight,
            JointType.KneeLeft,
            JointType.KneeRight,
            JointType.AnkleLeft,
            JointType.AnkleRight,
            JointType.FootLeft,
            JointType.FootRight
        };

        /// <summary>
        /// Convertit les positions des joints en vecteur d'entrée pour le perceptron
        /// </summary>
        public static double[] ConvertirJointsEnEntrees(IReadOnlyDictionary<JointType, Joint> joints)
        {
            List<double> entrees = new List<double>();

            foreach (JointType type in JointsImportants)
            {
                if (joints.ContainsKey(type))
                {
                    // Ajouter les coordonnées X, Y, Z du joint
                    entrees.Add(joints[type].Position.X);
                    entrees.Add(joints[type].Position.Y);
                    entrees.Add(joints[type].Position.Z);
                }
            }

            // Normaliser les entrées
            return NormaliserEntrees(entrees.ToArray());
        }

        /// <summary>
        /// Normalise les valeurs d'entrée pour éviter les problèmes de convergence
        /// </summary>
        private static double[] NormaliserEntrees(double[] entrees)
        {
            double valeurMax = entrees.Select(Math.Abs).Max();
            if (valeurMax == 0)
                return entrees;

            for (int i = 0; i < entrees.Length; i++)
            {
                entrees[i] = entrees[i] / valeurMax;
            }

            return entrees;
        }
    }
}
