using System.Text.Json;
using System.Text.RegularExpressions;

using StarmaidIntegrationComputer.Thalassa.OpenAiCommon.JsonParsing;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.VoiceToText.Exceptions;
using StarmaidIntegrationComputer.Thalassa.VoiceToText.JsonParsing;

namespace StarmaidIntegrationComputer.Thalassa.VoiceToText
{
    //TODO: Consider adding logging?
    public class TranscriptionSender
    {
        private HttpClient httpClient;
        private readonly OpenAISensitiveSettings settings;

        const string transcriptionsUri = "https://api.openai.com/v1/audio/transcriptions";

        public TranscriptionSender(HttpClient httpClient, OpenAISensitiveSettings settings)
        {
            this.httpClient = httpClient;
            this.settings = settings;
        }

        public async Task<string> Interpret(string context, MemoryStream audio)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Post;
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.OpenAIBearerToken);
            var multipartContent = new MultipartFormDataContent
            {
                {new StreamContent(audio), "file", "source.wav" },
                { new StringContent("whisper-1"), "model"},
                { new StringContent($"{context}"), "prompt"},
            };
            message.Content = multipartContent;
            message.RequestUri = new Uri(transcriptionsUri);

            var httpResponseMessage = await httpClient.SendAsync(message);

            var result = await httpResponseMessage.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<string> Interpret(string context, byte[] audio)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Post;
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.OpenAIBearerToken);


            var multipartContent = new MultipartFormDataContent
            {
                {new ByteArrayContent(audio), "file", "source.wav" },
                { new StringContent("whisper-1"), "model"},
                { new StringContent($"{context}"), "prompt"},
            };

            message.Content = multipartContent;
            message.RequestUri = new Uri(transcriptionsUri);

            var httpResponseMessage = await httpClient.SendAsync(message);

            var interpretingResponse = await httpResponseMessage.Content.ReadAsStringAsync();


            var interpretingResponseWithoutWhitespace = Regex.Replace(interpretingResponse, "s*", "");

            //This is the good response!
            if (interpretingResponseWithoutWhitespace.StartsWith("{\"text\":\""))
            {
                try
                {
                    ParsedTranscriptionText parsedText = JsonSerializer.Deserialize<ParsedTranscriptionText>(interpretingResponse);

                    return parsedText.text;
                }
                catch
                {
                    //TODO: Consider logging this?  But we'll catch that something weird happened when we fall through and we log the state, tbf.
                }
            }

            //That wasn't the expected good response, maybe it was an error in the known error format?
            if (interpretingResponseWithoutWhitespace.StartsWith("{\"error\":{\"message\":\""))
            {
                try
                {
                    ParsedOpenAiError error = JsonSerializer.Deserialize<ParsedOpenAiError>(interpretingResponse);

                    throw new TranscriptionSenderException($"Error interpreting speech - {error.error.message}", interpretingResponse);
                }
                catch
                {
                    //TODO: Consider logging this?  But we'll catch that something weird happened when we fall through and we log the state, tbf.
                }
            }

            //What we got was neither an error in the known error format, nor the expected good response - report what we got.
            throw new TranscriptionSenderException("Error interpreting speech, and unable to parse error as message! See log!", interpretingResponse);
        }
    }
}
