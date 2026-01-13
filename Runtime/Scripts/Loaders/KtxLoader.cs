using System;
using System.Collections;
using System.Collections.Generic;
using KtxUnity;
using UnityEngine;
using UnityEngine.Networking;

namespace Gamenator.Core.TextureLoader
{
    /// <summary>
    /// KTX2 texture loader using the KtxUnity library.
    /// Supports KTX2/Basis Universal compressed textures with configurable image index, face slice, mip limit, and color space.
    /// </summary>
    public class KtxLoader : BaseTextureLoader
    {
        // ---------------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------------

        /// <summary>
        /// Progress steps for download (0.9) and decode (1.0) phases.
        /// </summary>
        private readonly float[] _progressSteps = { 0.9f, 1f };

        /// <summary>
        /// Image index to load from the KTX2 file (for texture arrays).
        /// </summary>
        private readonly uint _imageIndex;

        /// <summary>
        /// Face slice index to load (for cubemaps).
        /// </summary>
        private readonly uint _faceSlice;

        /// <summary>
        /// Maximum mip level to load.
        /// </summary>
        private readonly uint _mipLimit;

        /// <summary>
        /// Whether to import mipmaps from the KTX2 file.
        /// </summary>
        private readonly bool _importMip;

        /// <summary>
        /// Whether to load the texture in linear color space (true) or sRGB (false).
        /// </summary>
        private readonly bool _linear;

        // ---------------------------------------------------------------------
        // Constructors
        // ---------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="KtxLoader"/> class.
        /// </summary>
        /// <param name="maxRetry">Maximum number of retry attempts before giving up.</param>
        /// <param name="delayBetweenRetry">Delay in seconds between retry attempts.</param>
        /// <param name="nonReadable">Whether the loaded texture should be non-readable (memory optimization).</param>
        /// <param name="imageIndex">Image index to load from the KTX2 file (for texture arrays).</param>
        /// <param name="faceSlice">Face slice index to load (for cubemaps).</param>
        /// <param name="mipLimit">Maximum mip level to load.</param>
        /// <param name="importMip">Whether to import mipmaps from the KTX2 file.</param>
        /// <param name="linear">Whether to load the texture in linear color space (true) or sRGB (false).</param>
        public KtxLoader(int maxRetry, float delayBetweenRetry, bool nonReadable, uint imageIndex, uint faceSlice, uint mipLimit, bool importMip, bool linear)
            : base(maxRetry, delayBetweenRetry, nonReadable)
        {
            _imageIndex = imageIndex;
            _faceSlice = faceSlice;
            _mipLimit = mipLimit;
            _importMip = importMip;
            _linear = linear;
        }

        // ---------------------------------------------------------------------
        // Protected Methods
        // ---------------------------------------------------------------------

        /// <inheritdoc/>
        protected override IEnumerator RunSingleAttempt(
            string url,
            int attempt,
            Dictionary<string, string> headers,
            Action<Texture2D> onComplete,
            Action<FailureReason> onFailed,
            IProgress<LoadProgress> progress)
        {
            Debug.Log($"KtxLoader.RunSingleAttempt() Starting download attempt {attempt + 1} at {Time.time}");
            byte[] raw = null;

            // Download KTX2 data
            yield return DownloadKtx2Bytes(url, data => raw = data, onFailed, progress, headers);

            if (CheckCancel()) yield break;
            if (raw == null) yield break;

            // Decode the raw bytes into Texture2D
            Debug.Log($"KtxLoader.RunSingleAttempt() starting Decoding KTX2 bytes at {Time.time}");
            yield return DecodeKtx2Bytes(raw, onComplete, (reason) => { CancelInternal(); onFailed?.Invoke(reason); }, progress);

            bool CheckCancel()
            {
                if (_cancel)
                {
                    onComplete?.Invoke(null);
                    return true;
                }

                return false;
            }
        }

        // ---------------------------------------------------------------------
        // Private Methods
        // ---------------------------------------------------------------------

        /// <summary>
        /// Downloads the KTX2 file as raw bytes.
        /// </summary>
        /// <param name="url">The URL of the KTX2 file to download.</param>
        /// <param name="onComplete">Callback to invoke with the downloaded bytes on success.</param>
        /// <param name="onFailed">Callback to invoke with a failure reason on error.</param>
        /// <param name="progress">Optional progress reporter for tracking download progress.</param>
        /// <param name="headers">Optional HTTP headers to include in the request.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator DownloadKtx2Bytes(
            string url,
            Action<byte[]> onComplete,
            Action<FailureReason> onFailed,
            IProgress<LoadProgress> progress,
            Dictionary<string, string> headers)
        {
            Debug.Log($"KtxLoader.DownloadKtx2Bytes() started at {Time.time}");

            progress?.Report(_loadProgress.UpdateProgress(0f));

            using var uwr = UnityWebRequest.Get(url);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            if (headers != null)
                foreach (var kv in headers)
                    uwr.SetRequestHeader(kv.Key, kv.Value);

            // Send the request
            var op = uwr.SendWebRequest();
            while (!op.isDone)
            {
                if (_cancel)
                {
                    Debug.Log($"KtxLoader.DownloadKtx2Bytes(): canceled at {Time.time}");
                    if (uwr != null) uwr.Abort();
                    onComplete?.Invoke(null);
                    yield break;
                }
                // Scale download progress into the first phase (0→0.9)
                progress?.Report(_loadProgress.UpdateProgress(uwr.downloadProgress * _progressSteps[0]));
                yield return null;
            }

            // Check for network error
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"KtxLoader.DownloadKtx2Bytes(): network error {uwr.error} at {Time.time}");
                onFailed?.Invoke(FailureReason.RequestError);
                yield break;
            }

            // Grab the data
            var data = uwr.downloadHandler.data;
            if (data == null || data.Length == 0)
            {
                Debug.LogError($"KtxLoader.DownloadKtx2Bytes(): empty response at {Time.time}");
                onFailed?.Invoke(FailureReason.EmptyResponse);
                yield break;
            }

            Debug.Log($"KtxLoader.DownloadKtx2Bytes(): success at {Time.time}");
            progress?.Report(_loadProgress.UpdateProgress(_progressSteps[0]));
            onComplete?.Invoke(data);
        }

        /// <summary>
        /// Decodes the KTX2 bytes into a Unity Texture2D.
        /// </summary>
        /// <param name="rawData">The raw KTX2 file bytes to decode.</param>
        /// <param name="onComplete">Callback to invoke with the decoded texture on success.</param>
        /// <param name="onFailed">Callback to invoke with a failure reason on error.</param>
        /// <param name="progress">Optional progress reporter for tracking decode progress.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator DecodeKtx2Bytes(
            byte[] rawData,
            Action<Texture2D> onComplete,
            Action<FailureReason> onFailed,
            IProgress<LoadProgress> progress)
        {
            Debug.Log($"KtxLoader.DecodeKtx2Bytes(): starting at {Time.time}");
            if (_cancel)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            progress?.Report(_loadProgress.UpdateProgress(_progressSteps[0]));

            using var mna = new ManagedNativeArray(rawData);
            var slice = mna.nativeArray;
            var loader = new KtxTexture();
            try
            {
                if (loader.Open(slice) != ErrorCode.Success)
                {
                    Debug.LogError($"KtxLoader.DecodeKtx2Bytes(): Open failed at {Time.time}");
                    onFailed?.Invoke(FailureReason.Other);
                    yield break;
                }

                var task = loader.LoadTexture2D(_linear, _imageIndex, _faceSlice, _mipLimit, _importMip);
                while (!task.IsCompleted)
                {
                    if (_cancel)
                    {
                        Debug.Log($"KtxLoader.DecodeKtx2Bytes(): canceled at {Time.time}");
                        onComplete?.Invoke(null);
                        yield break;
                    }
                    yield return null;
                }

                var result = task.Result;

                if (result.errorCode != ErrorCode.Success || result.texture == null)
                {
                    Debug.LogError($"KtxLoader.DecodeKtx2Bytes: failed {result.errorCode} at {Time.time}");
                    onFailed?.Invoke(FailureReason.Other);
                }
                else
                {
                    Debug.Log($"KtxLoader.DecodeKtx2Bytes(): finished at {Time.time}");
                    progress?.Report(_loadProgress.UpdateProgress(_progressSteps[1]));
                    onComplete?.Invoke(result.texture);
                }
            }
            finally
            {
                loader.Dispose();
            }
        }
    }
}
