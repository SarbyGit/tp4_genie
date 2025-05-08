using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JeuHoy_WPF.vue
{
    /// <summary>
    /// Auteur: Hugo St-Louis
    /// Description: Fenêtre princiaple de l'application. Montre les choix à l'utilisateur
    /// Date: 2023-04-13
    /// </summary>
    public partial class wEntree : Window
    {
        private JouerMp3 _wmpIntro = new JouerMp3();

        /// <summary>
        /// Constructeur
        /// </summary>
        public wEntree()
        {
            InitializeComponent();

            _wmpIntro.Open(@"./HoyContent/intro.mp3");
            _wmpIntro.Play(true);

        }

        /// <summary>
        /// Ouverture de la fenêtre de jeu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picJouer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _wmpIntro.Close();

            wJeu f = new wJeu();
            this.Hide();
            f.ShowDialog();
            f.Close();
            this.Show();
            _wmpIntro.Open(@"./HoyContent/intro.mp3");
            _wmpIntro.Play(true);
        }

        /// <summary>
        /// Ouverture de la fenêtre d'entrainement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picEntrainement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _wmpIntro.Close();

            wEntrainement f = new wEntrainement();
            this.Hide();
            f.ShowDialog();
            f.Close();
            this.Show();
            _wmpIntro.Open(@"./HoyContent/intro.mp3");
            _wmpIntro.Play(true);
        }

        /// <summary>
        /// Fermeture de la form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picQuitter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
            _wmpIntro.Close();
        }

        /// <summary>
        /// Ouverture de la fenêtre d'aide.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picAide_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            wAide f = new wAide();
            this.Hide();
            f.ShowDialog();
            f.Close();
            this.Show();
        }

        /// <summary>
        /// Comportement lorsque curseur est au dessus d'une image(modifier le curseur)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pic_MouseHover(object sender, MouseEventArgs e)
        {

            this.Cursor = Cursors.Hand;
            Image p = (Image)sender;
            if (p.Name == "picJouer")
            {
                Uri uriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resources/JouerDessus.png", UriKind.Absolute);
                picJouer.Source = new BitmapImage(uriSource);
            }
            else if (p.Name == "picEntrainement")
            {
                Uri uriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Resources\EntrainementDessus.png", UriKind.Absolute);
                picEntrainement.Source = new BitmapImage(uriSource);
            }
            else if (p.Name == "picAide")
            {
                Uri uriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory +  @"./Resources/AideDessus.png", UriKind.Absolute);
                picAide.Source = new BitmapImage(uriSource);
            }
        }

        /// <summary>
        /// Comportement lorsque curseur quitte l'image(modifier le curseur)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pic_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            Image p = (Image)sender;
            if (p.Name == "picJouer")
            {
                Uri uriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resources/JouerHoy.png", UriKind.Absolute);
                picJouer.Source = new BitmapImage(uriSource);
            }
            else if (p.Name == "picEntrainement")
            {
                Uri uriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Resources\Entrainement.png", UriKind.Absolute);
                picEntrainement.Source = new BitmapImage(uriSource);
            }
            else if (p.Name == "picAide")
            {
                Uri uriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"./Resources/Aide.png", UriKind.Absolute);
                picAide.Source = new BitmapImage(uriSource);
            }
        }

        /// <summary>
        /// Fermeture de la fenêtre
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _wmpIntro.Close();
        }

    }
}
