﻿using NAudio.Wave;

namespace StarmaidIntegrationComputer.Thalassa.VoiceToText
{
    public class VoiceToTextManager
    {
        private readonly TranscriptionSender transcriptionSender;
        private readonly VoiceListener voiceListener;

        public VoiceToTextManager(TranscriptionSender transcriptionSender, VoiceListener voiceListener)
        {
            this.transcriptionSender = transcriptionSender;
            this.voiceListener = voiceListener;
        }

        public async Task<string> StartListeningAndInterpret(string context = "")
        {
            var heardAudio = await voiceListener.StartListening();
            WaveIn wi = new WaveIn();
            using (WaveFileWriter writer = new WaveFileWriter(@"D:\temp\heardAudio\out.wav", new WaveFormat(16000, 16, 1)))
            {
                //TODO: Unsafe conversion, I should maybe do something about this

                await writer.WriteAsync(heardAudio.ToArray(), 0, heardAudio.Length);
            }

            var interpretedText = await transcriptionSender.Interpret(context, heardAudio);

            //Any further processing on the interpreted text will go here
            //  unless that processing is dependant on the use.
            return interpretedText;
        }
    }
}
