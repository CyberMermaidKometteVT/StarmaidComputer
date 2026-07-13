namespace StarmaidIntegrationComputer.Common.Assets
{
    /// <summary>
    /// Describes a file fetchable from a URL and verifiable against a known SHA-256 hash before
    /// being trusted - for assets (e.g. ONNX models) too large to check into source control, where
    /// we still want to detect a corrupted or unexpectedly-changed download.
    /// </summary>
    public sealed record ManagedAsset(string Url, string Sha256Hex, string Description);
}
