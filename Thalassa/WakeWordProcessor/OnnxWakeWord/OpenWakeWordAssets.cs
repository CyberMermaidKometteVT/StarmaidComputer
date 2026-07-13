using System.IO;

using StarmaidIntegrationComputer.Common.Assets;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor.OnnxWakeWord
{
    /// <summary>
    /// The shared melspectrogram/embedding ONNX models that ViolaWake and openWakeWord both consume.
    /// openWakeWord publishes these as GitHub release assets but doesn't itself publish hashes for
    /// them - the hashes below were computed here after downloading and manually verifying the files.
    /// These live under a separate, app-managed directory rather than Assets\Wake Word Models, since
    /// that folder is where the user's own trained classifier files go - mixing auto-downloaded and
    /// user-provided files in one place would be confusing to sort out.
    /// </summary>
    public static class OpenWakeWordAssets
    {
        private static readonly string SharedModelDirectory = Path.Combine("Cache", "WakeWordModels");

        public static readonly string MelspectrogramLocalPath = Path.Combine(SharedModelDirectory, "melspectrogram.onnx");
        public static readonly string EmbeddingModelLocalPath = Path.Combine(SharedModelDirectory, "embedding_model.onnx");

        public static readonly ManagedAsset Melspectrogram = new(
            "https://github.com/dscripka/openWakeWord/releases/download/v0.5.1/melspectrogram.onnx",
            "ba2b0e0f8b7b875369a2c89cb13360ff53bac436f2895cced9f479fa65eb176f",
            "the melspectrogram model - the first stage of Thalassa's ViolaWake wake-word listener, which turns raw microphone audio into a spectrogram the next model can read");

        public static readonly ManagedAsset EmbeddingModel = new(
            "https://github.com/dscripka/openWakeWord/releases/download/v0.5.1/embedding_model.onnx",
            "70d164290c1d095d1d4ee149bc5e00543250a7316b59f31d056cff7bd3075c1f",
            "the speech-embedding model - the second stage of Thalassa's ViolaWake wake-word listener, which turns that spectrogram into a numeric fingerprint of what was said");
    }
}
