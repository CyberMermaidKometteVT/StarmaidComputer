using NAudio.Wave;

namespace Thalassa.VoiceToText
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
#error Leaving off here!  Writing to a wav file for test purposes, and maybe I'll want to keep doing that?  Not sure yet.  But either way, I need to implement the TranscriptionSender class!
            var heardAudio = await voiceListener.StartListening();
            using (WaveFileWriter writer = new WaveFileWriter(@"D:\temp\heardAudio\out.wav", new WaveFormat(48000, 1)))
            {
                writer.Write(heardAudio, 0, heardAudio.Length);
            }
            //var interpretedText = await transcriptionSender.Interpret(context, heardAudio);

            //Any further processing on the interpreted text will go here
            //  unless that processing is dependant on the use.
            throw new NotImplementedException();
            //return interpretedText;
        }
    }
}
