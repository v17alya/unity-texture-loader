using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamenator.Core.TextureLoader
{
    /// <summary>
    /// Abstract base class implementing retry and cancellation logic for texture loaders.
    /// Provides a template method pattern for subclasses to implement specific loading strategies.
    /// </summary>
    public abstract class BaseTextureLoader : ITextureLoader
    {
        // ---------------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------------

        /// <summary>
        /// Maximum number of retry attempts before giving up.
        /// </summary>
        protected readonly int _maxRetry;

        /// <summary>
        /// Delay in seconds between retry attempts.
        /// </summary>
        protected readonly float _delayBetweenRetry;

        /// <summary>
        /// Whether the loaded texture should be non-readable (memory optimization).
        /// </summary>
        protected readonly bool _nonReadable;

        /// <summary>
        /// Progress tracking object shared across attempts.
        /// </summary>
        protected readonly LoadProgress _loadProgress;

        /// <summary>
        /// Flag indicating whether the current load operation has been cancelled.
        /// </summary>
        protected bool _cancel;

        // ---------------------------------------------------------------------
        // Constructors
        // ---------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTextureLoader"/> class.
        /// </summary>
        /// <param name="maxRetry">Maximum number of retry attempts before giving up.</param>
        /// <param name="delayBetweenRetry">Delay in seconds between retry attempts.</param>
        /// <param name="nonReadable">Whether the loaded texture should be non-readable (memory optimization).</param>
        protected BaseTextureLoader(int maxRetry, float delayBetweenRetry, bool nonReadable)
        {
            _maxRetry = maxRetry;
            _delayBetweenRetry = delayBetweenRetry;
            _nonReadable = nonReadable;
            _loadProgress = new LoadProgress { Attempt = 0, MaxAttemptReached = false, Progress = 0f };
        }

        // ---------------------------------------------------------------------
        // Public Methods
        // ---------------------------------------------------------------------

        /// <summary>
        /// Cancels the current loading operation.
        /// </summary>
        public void CancelLoad() => CancelInternal();

        /// <summary>
        /// Template method handling retry loop, cancellation, and error reporting.
        /// Delegates a single attempt to <see cref="RunSingleAttempt"/>.
        /// </summary>
        /// <param name="url">The URL of the texture to load.</param>
        /// <param name="onComplete">Callback invoked when the texture is successfully loaded.</param>
        /// <param name="onFailed">Callback invoked when loading fails.</param>
        /// <param name="progress">Optional progress reporter for tracking loading progress.</param>
        /// <param name="headers">Optional HTTP headers to include in the request.</param>
        /// <returns>Coroutine enumerator.</returns>
        public IEnumerator Load(
            string url,
            Action<Texture2D> onComplete,
            Action<FailureReason> onFailed,
            IProgress<LoadProgress> progress = null,
            Dictionary<string, string> headers = null)
        {
            int attempt = 0;
            Texture2D result = null;

            Debug.Log($"[{GetType().Name}].Load() Starting load for {url}");

            while (attempt < _maxRetry && !_cancel)
            {
                Debug.Log($"[{GetType().Name}].Load(): loading attempt {attempt + 1} at {Time.time}");
                // Update attempt count and reset progress
                progress?.Report(_loadProgress.UpdateAttempt(attempt + 1).UpdateProgress(0f));

                // Perform the subclass-specific load attempt
                yield return RunSingleAttempt(url, attempt, headers,
                    tex => result = tex, onFailed, progress);

                if (_cancel)
                {
                    Debug.Log($"[{GetType().Name}].Load() cancelled");
                    onComplete?.Invoke(null);
                    yield break;
                }

                if (result != null)
                {
                    Debug.Log($"[{GetType().Name}].Load() succeeded on attempt {attempt + 1}");
                    onComplete?.Invoke(result);
                    yield break;
                }

                Debug.LogWarning($"[{GetType().Name}].Load() Attempt {attempt + 1} failed, retrying...");
                attempt++;
                yield return new WaitForSeconds(_delayBetweenRetry);
            }

            progress?.Report(_loadProgress.UpdateAttempt(_maxRetry).UpdateMaxAttemptReached(true));
            Debug.LogError($"[{GetType().Name}].Load() All {_maxRetry} attempts failed");
            onFailed?.Invoke(FailureReason.MaxAttemptReached);
        }

        // ---------------------------------------------------------------------
        // Protected Methods
        // ---------------------------------------------------------------------

        /// <summary>
        /// Cancels the internal loading operation.
        /// </summary>
        protected void CancelInternal() => _cancel = true;

        /// <summary>
        /// Implement this method to perform a single loading or decoding attempt.
        /// Should call <paramref name="onComplete"/> with the loaded texture or <paramref name="onFailed"/> on error.
        /// </summary>
        /// <param name="url">The URL of the texture to load.</param>
        /// <param name="attempt">The current attempt number (0-based).</param>
        /// <param name="headers">Optional HTTP headers to include in the request.</param>
        /// <param name="onComplete">Callback to invoke with the loaded texture on success.</param>
        /// <param name="onFailed">Callback to invoke with a failure reason on error.</param>
        /// <param name="progress">Optional progress reporter for tracking loading progress.</param>
        /// <returns>Coroutine enumerator.</returns>
        protected abstract IEnumerator RunSingleAttempt(
            string url,
            int attempt,
            Dictionary<string, string> headers,
            Action<Texture2D> onComplete,
            Action<FailureReason> onFailed,
            IProgress<LoadProgress> progress);
    }
}
