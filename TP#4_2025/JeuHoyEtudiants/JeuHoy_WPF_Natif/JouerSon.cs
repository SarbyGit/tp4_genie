using System.ComponentModel;
using System.Media;

namespace JeuHoy_WPF
{
    /// <summary>
    /// Auteur: Hugo St-Louis
    /// Description: Permet de faire jouer un son asynchrone.
    /// Date: 26/04/2014
    /// </summary>
    public class JouerSon
    {
        /// <summary>
        /// Permet de faire charger un son et de le faire jouer de manière asynchrone.
        /// </summary>
        /// <param name="FichierSon"></param>
        public void JouerSonAsync(string FichierSon)
        {
            SoundPlayer wavPlayer = new SoundPlayer();
            wavPlayer.SoundLocation = FichierSon;
            wavPlayer.LoadCompleted += new AsyncCompletedEventHandler(wavPlayer_LoadCompleted);
            wavPlayer.LoadAsync();
        }

        /// <summary>
        /// Fait jouer le son.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wavPlayer_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            ((System.Media.SoundPlayer)sender).Play();
        }
    }
}
