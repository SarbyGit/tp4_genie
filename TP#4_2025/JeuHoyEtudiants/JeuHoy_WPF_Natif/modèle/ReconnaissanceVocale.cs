using System;
using System.Speech.Recognition;
using System.Globalization;

namespace JeuHoy_WPF.modèle
{
    /// <summary>
    /// Gère la reconnaissance vocale pour le jeu
    /// </summary>
    public class ReconnaissanceVocale : IDisposable
    {
        private SpeechRecognitionEngine _recognizer;
        private bool _isListening;

        // Événement déclenché lorsque le mot "HOY" est reconnu
        public event EventHandler CommandeDetectee;

        /// <summary>
        /// Constructeur
        /// </summary>
        public ReconnaissanceVocale()
        {
            // Initialiser le moteur de reconnaissance vocale
            try
            {
                // Spécifiez la culture française pour une meilleure reconnaissance en français
                _recognizer = new SpeechRecognitionEngine(new CultureInfo("fr-FR"));

                // Créer les commandes vocales
                var choices = new Choices();
                choices.Add(new string[] { "HOY" });

                // Créer la grammaire
                var grammar = new Grammar(new GrammarBuilder(choices));
                _recognizer.LoadGrammar(grammar);

                // Définir les événements
                _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                _recognizer.SpeechDetected += Recognizer_SpeechDetected;
                _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;

                // Configurer l'entrée audio
                _recognizer.SetInputToDefaultAudioDevice();

                _isListening = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur d'initialisation de la reconnaissance vocale: {ex.Message}");
            }
        }

        /// <summary>
        /// Commence l'écoute
        /// </summary>
        public void DemarrerEcoute()
        {
            if (_recognizer != null && !_isListening)
            {
                try
                {
                    _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    _isListening = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur au démarrage de l'écoute: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Arrête l'écoute
        /// </summary>
        public void ArreterEcoute()
        {
            if (_recognizer != null && _isListening)
            {
                try
                {
                    _recognizer.RecognizeAsyncStop();
                    _isListening = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur à l'arrêt de l'écoute: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Se déclenche lorsqu'une parole est reconnue
        /// </summary>
        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Vérifier si la confiance est suffisante
            if (e.Result.Confidence > 0.7)
            {
                if (e.Result.Text.ToUpper() == "HOY")
                {
                    // Déclencher l'événement
                    CommandeDetectee?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Se déclenche lorsqu'une parole est détectée (mais pas encore reconnue)
        /// </summary>
        private void Recognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            // Ici vous pourriez ajouter du feedback visuel indiquant que la voix est détectée
        }

        /// <summary>
        /// Se déclenche lorsque la reconnaissance est terminée
        /// </summary>
        private void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                _isListening = false;
            }
        }

        /// <summary>
        /// Libère les ressources
        /// </summary>
        public void Dispose()
        {
            if (_recognizer != null)
            {
                ArreterEcoute();
                _recognizer.Dispose();
                _recognizer = null;
            }
        }
    }
}
