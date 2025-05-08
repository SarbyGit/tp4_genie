using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeuHoy_WPF_Natif.modèle
{
    internal class position
    {
        public int NumeroPosition { get; set; }
        public Dictionary<JointType, double[]> JointsPosition { get; set; }

        public position(int numero)
        {
            NumeroPosition = numero;
            JointsPosition = new Dictionary<JointType, double[]>();
        }
    }
}
