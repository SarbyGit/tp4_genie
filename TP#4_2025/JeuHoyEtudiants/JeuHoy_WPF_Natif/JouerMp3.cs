using System;
using System.Text;

using System.Runtime.InteropServices;

namespace JeuHoy_WPF
{
    /// <summary>
    /// Auteur: Hugo St-Louis
    /// Description: Permet de faire jouer un mp3
    /// Date: 26/04/2014
    /// </summary>
    public class JouerMp3
    {
        private string _command;
        private bool isOpen = false;
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        /// <summary>
        /// Ferme et stop un mp3 qui joue
        /// </summary>
        public void Close()
        {
            _command = "close MediaFile";
            mciSendString(_command, null, 0, IntPtr.Zero);
            isOpen = false;
        }

        /// <summary>
        /// Ouvre un mp3
        /// </summary>
        /// <param name="sFileName"></param>
        public void Open(string sFileName)
        {
            _command = "open \"" + sFileName + "\" type mpegvideo alias MediaFile";
            mciSendString(_command, null, 0, IntPtr.Zero); 
           isOpen = true;
        }

        /// <summary>
        /// Fait jouer un mp3
        /// </summary>
        /// <param name="loop">Détermine si le mp3 doit jouer en boucle.</param>
        public void Play(bool loop)
        {
            if (isOpen)
            {
                _command = "play MediaFile";
                if (loop)
                    _command += " REPEAT";
                mciSendString(_command, null, 0, IntPtr.Zero);
            }
        }

    }
}
