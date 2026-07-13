using System;
using System.IO;
using System.Net;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using StarmaidIntegrationComputer.Common.Assets;

namespace StarmaidIntegrationComputer.Thalassa.Tests
{
    /// <summary>
    /// Exercises AssetDownloader against the real filesystem (temp files) - not a unit test.
    /// </summary>
    [TestClass]
    public class AssetDownloaderIntegrationTests
    {
        private const string FileContent = "fake onnx model bytes";

        private string destinationPath = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            destinationPath = Path.Combine(Path.GetTempPath(), $"AssetDownloaderTests_{Guid.NewGuid()}.onnx");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            string temporaryPath = destinationPath + ".download";
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        [TestMethod]
        public void EnsureDownloaded_FileAlreadyPresentWithMatchingHash_DoesNotDownload()
        {
            File.WriteAllText(destinationPath, FileContent);
            string expectedHash = ComputeSha256(destinationPath);
            ManagedAsset asset = new("https://example.com/model.onnx", expectedHash, "test model");

            AssetDownloader downloader = new(new ThrowingHttpClientFactory());

            downloader.EnsureDownloaded(destinationPath, asset, NullLogger.Instance);

            Assert.AreEqual(FileContent, File.ReadAllText(destinationPath));
        }

        [TestMethod]
        public void EnsureDownloaded_FileMissing_DownloadsAndVerifies()
        {
            ManagedAsset asset = new("https://example.com/model.onnx", ComputeSha256Of(FileContent), "test model");
            AssetDownloader downloader = new(new FakeHttpClientFactory { Content = FileContent, StatusCode = HttpStatusCode.OK });

            downloader.EnsureDownloaded(destinationPath, asset, NullLogger.Instance);

            Assert.IsTrue(File.Exists(destinationPath));
            Assert.AreEqual(FileContent, File.ReadAllText(destinationPath));
            Assert.IsFalse(File.Exists(destinationPath + ".download"), "Temporary download file should be renamed away, not left behind.");
        }

        [TestMethod]
        public void EnsureDownloaded_HashMismatch_ThrowsAndDoesNotLeaveFiles()
        {
            ManagedAsset asset = new("https://example.com/model.onnx", "0000000000000000000000000000000000000000000000000000000000000", "test model");
            AssetDownloader downloader = new(new FakeHttpClientFactory { Content = FileContent, StatusCode = HttpStatusCode.OK });

            Assert.ThrowsException<InvalidOperationException>(() => downloader.EnsureDownloaded(destinationPath, asset, NullLogger.Instance));

            Assert.IsFalse(File.Exists(destinationPath), "A file that failed hash verification should not be left at the destination.");
            Assert.IsFalse(File.Exists(destinationPath + ".download"), "The temporary download file should be cleaned up after a hash mismatch.");
        }

        [TestMethod]
        public void EnsureDownloaded_NetworkFailure_ThrowsInvalidOperationExceptionWithClearMessage()
        {
            ManagedAsset asset = new("https://example.com/model.onnx", "irrelevant", "test model");
            AssetDownloader downloader = new(new FakeHttpClientFactory { Content = FileContent, StatusCode = HttpStatusCode.ServiceUnavailable });

            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(
                () => downloader.EnsureDownloaded(destinationPath, asset, NullLogger.Instance));

            StringAssert.Contains(exception.Message, "test model");
            Assert.IsFalse(File.Exists(destinationPath));
        }

        private static string ComputeSha256(string path)
        {
            using FileStream fileStream = File.OpenRead(path);
            using System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(fileStream)).ToLowerInvariant();
        }

        private static string ComputeSha256Of(string content)
        {
            using System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
