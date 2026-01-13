using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Gamenator.Core.TextureLoader
{
    /// <summary>
    /// Standard texture loader using Unity's <see cref="UnityWebRequestTexture"/>.
    /// Supports standard image formats (PNG, JPG, etc.) and provides retry logic with progress reporting.
    /// </summary>
    public class StandardTextureLoader : BaseTextureLoader
    {
        // ---------------------------------------------------------------------
        // Constructors
        // ---------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardTextureLoader"/> class.
        /// </summary>
        /// <param name="maxRetry">Maximum number of retry attempts before giving up.</param>
        /// <param name="delayBetweenRetry">Delay in seconds between retry attempts.</param>
        /// <param name="nonReadable">Whether the loaded texture should be non-readable (memory optimization).</param>
        public StandardTextureLoader(int maxRetry, float delayBetweenRetry, bool nonReadable)
            : base(maxRetry, delayBetweenRetry, nonReadable)
        {
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
            using var uwr = UnityWebRequestTexture.GetTexture(url, _nonReadable);
            if (headers != null)
                foreach (var kv in headers) uwr.SetRequestHeader(kv.Key, kv.Value);

            yield return LoadTexture(uwr, onComplete, onFailed, attempt, progress);
        }

        // ---------------------------------------------------------------------
        // Private Methods
        // ---------------------------------------------------------------------

        /// <summary>
        /// Performs the actual texture download and loading.
        /// </summary>
        /// <param name="uwr">The Unity web request to use for downloading.</param>
        /// <param name="onComplete">Callback to invoke with the loaded texture on success.</param>
        /// <param name="onFailed">Callback to invoke with a failure reason on error.</param>
        /// <param name="attempt">The current attempt number (for logging).</param>
        /// <param name="progress">Optional progress reporter for tracking download progress.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator LoadTexture(
            UnityWebRequest uwr,
            Action<Texture2D> onComplete,
            Action<FailureReason> onFailed,
            int attempt,
            IProgress<LoadProgress> progress)
        {
            Debug.Log($"StandardTextureLoader.LoadTexture(): downloading attempt {attempt + 1} at {Time.time}");
            var op = uwr.SendWebRequest();
            while (!op.isDone)
            {
                if (_cancel)
                {
                    Debug.Log($"StandardTextureLoader.LoadTexture(): canceled at {Time.time}");
                    if (uwr != null) uwr.Abort();
                    onComplete?.Invoke(null);
                    yield break;
                }
                progress?.Report(_loadProgress.UpdateAttempt(attempt + 1).UpdateProgress(uwr.downloadProgress));
                yield return null;
            }

            if (uwr.result != UnityWebRequest.Result.Success ||
                (uwr.responseCode != 200 && uwr.responseCode != 201))
            {
                Debug.LogError($"StandardTextureLoader.LoadTexture(): network error {uwr.error} at {Time.time}");
                onFailed?.Invoke(FailureReason.RequestError);
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(uwr);
            if (texture == null)
            {
                Debug.LogError($"StandardTextureLoader.LoadTexture(): DownloadHandler returned null at {Time.time}");
                onFailed?.Invoke(FailureReason.EmptyResponse);
                yield break;
            }

            Debug.Log($"StandardTextureLoader.LoadTexture(): texture loaded successfully at {Time.time}");
            onComplete?.Invoke(texture);
        }
    }
}
