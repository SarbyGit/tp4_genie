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
using System.Windows.Shapes;

namespace JeuHoy_WPF.vue
{
    /// <summary>
    /// Auteur:      Hugo St-Louis
    /// Description: Montre les règles du jeu.
    /// Date:        2023-04-13
    /// </summary>
    public partial class wAide : Window
    {

        /// <summary>
        /// ctor
        /// </summary>
        public wAide()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Change le curseur lorsque le curseur est sur l'image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picRetour_MouseHover(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Change le curseur lorsque le curseur nest plus sur l'image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picRetour_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Ferme la fenêtre lorsqu'on appuie sur le bouton retour.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picRetour_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
