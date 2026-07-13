using System.Security.Cryptography;

using Microsoft.Extensions.Logging;

namespace StarmaidIntegrationComputer.Common.Assets
{
    /// <summary>
    /// Ensures a <see cref="ManagedAsset"/> exists at a destination path, downloading it if missing
    /// or if its hash doesn't match. Verifying before downloading skips redundant fetches on every
    /// startup; verifying after downloading catches corruption or an unexpected upstream change.
    /// Takes <see cref="IHttpClientFactory"/> (registered via services.AddHttpClient() in Startup)
    /// rather than owning an HttpClient itself, so handler pooling/DNS rotation is managed for us.
    /// </summary>
    public sealed class AssetDownloader
    {
        private readonly IHttpClientFactory httpClientFactory;

        public AssetDownloader(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public void EnsureDownloaded(string destinationPath, ManagedAsset asset, ILogger logger)
        {
            if (File.Exists(destinationPath) && ComputeSha256(destinationPath) == asset.Sha256Hex)
            {
                return;
            }

            logger.LogInformation($"Downloading {asset.Description} from {asset.Url}...");

            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            string temporaryPath = destinationPath + ".download";

            try
            {
                using (HttpClient httpClient = httpClientFactory.CreateClient())
                using (HttpResponseMessage response = httpClient.GetAsync(asset.Url).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();

                    using FileStream fileStream = File.Create(temporaryPath);
                    response.Content.CopyToAsync(fileStream).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                File.Delete(temporaryPath);
                throw new InvalidOperationException(
                    $"Could not download {asset.Description} from {asset.Url}: {ex.Message} " +
                    $"Check your internet connection, or manually download the file and place it at '{destinationPath}'.",
                    ex);
            }

            string downloadedHash = ComputeSha256(temporaryPath);
            if (downloadedHash != asset.Sha256Hex)
            {
                File.Delete(temporaryPath);
                throw new InvalidOperationException(
                    $"Downloaded {asset.Description} from {asset.Url}, but its SHA-256 ({downloadedHash}) did not match " +
                    $"the expected value ({asset.Sha256Hex}). The file may be corrupted or openWakeWord may have published " +
                    $"a new version at that URL; if this persists, manually verify and place the file at '{destinationPath}'.");
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
            logger.LogInformation($"Downloaded and verified {asset.Description}.");
        }

        private static string ComputeSha256(string path)
        {
            using FileStream fileStream = File.OpenRead(path);
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(fileStream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
