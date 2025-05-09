// IEntrainementVue.cs
using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JeuHoy_WPF.vue
{
    public interface IEntrainementVue
    {
        // Méthodes d'affichage
        void AfficherFigureEnCours(int position, BitmapImage image);
        void MettreAJourConsole(string message);
        void AfficherImage(WriteableBitmap image);

        // Méthodes pour le dessin du squelette
        void DessinerJoint(Point position, Color couleur, int taille);
        void DessinerLigne(Point debut, Point fin, Color couleur, double epaisseur);
        void EffacerSquelette();

        // Méthodes pour la reconnaissance vocale
        void ChangerEtatReconnaissanceVocale(bool actif, SolidColorBrush couleur, Visibility visibiliteInstructions);
        void DemarrerAnimationReconnaissanceVocale();

        // Propriétés pour les dimensions du canvas
        double LargeurCanvasSquelette { get; }
        double HauteurCanvasSquelette { get; }

        // Méthodes pour le changement de mode d'affichage
        void ChangerModeAffichage(DisplayFrameType mode);
    }
}