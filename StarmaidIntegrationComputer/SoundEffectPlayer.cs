using System;

using NAudio.Wave;

using StarmaidIntegrationComputer.StarmaidSettings;

namespace StarmaidIntegrationComputer
{
    public class SoundEffectPlayer : IDisposable
    {
        AudioFileReader startingListeningReader;
        AudioFileReader stoppingListeningReader;

        WaveOutEvent startingListening = new WaveOutEvent();
        WaveOutEvent stoppingListening = new WaveOutEvent();

        public SoundEffectPlayer(SoundPathSettings soundPathSettings)
        {
            startingListeningReader = new AudioFileReader(soundPathSettings.StartingListeningSoundPath);
            startingListening.Init(startingListeningReader);

            stoppingListeningReader = new AudioFileReader(soundPathSettings.StoppingListeningSoundPath);
            stoppingListening.Init(stoppingListeningReader);
        }

        /// <summary>
        /// Note that this must be called on the UI Dispatcher thread in order to work (I think?)
        /// </summary>
        public void PlayStartingListeningFile()
        {

            startingListeningReader.Position = 0;
            startingListening.Play();
        }

        /// <summary>
        /// Note that this must be called on the UI Dispatcher thread in order to work (I think?)
        /// </summary>
        public void PlayStoppingListeningFile()
        {

            stoppingListeningReader.Position = 0;
            stoppingListening.Play();

        }

        public void Dispose()
        {
            if (startingListeningReader != null)
            {
                startingListeningReader.Dispose();
            }

            if (stoppingListeningReader != null)
            {
                stoppingListeningReader.Dispose();
            }

            if (startingListening != null)
            {
                startingListening.Dispose();
            }

            if (stoppingListening != null)
            {
                stoppingListening.Dispose();
            }
        }
    }
}
