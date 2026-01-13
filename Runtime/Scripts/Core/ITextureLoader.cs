using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamenator.Core.TextureLoader
{
    /// <summary>
    /// Common interface for texture loading (regular or KTX).
    /// Provides asynchronous texture loading with progress reporting, error handling, and cancellation support.
    /// </summary>
    public interface ITextureLoader
    {
        /// <summary>
        /// Loads a texture from the given URL asynchronously.
        /// </summary>
        /// <param name="url">The URL of the texture to load.</param>
        /// <param name="onComplete">Callback invoked when the texture is successfully loaded. Receives the loaded <see cref="Texture2D"/> or null if cancelled.</param>
        /// <param name="onFailed">Callback invoked when loading fails. Receives a <see cref="FailureReason"/> indicating the cause of failure.</param>
        /// <param name="progress">Optional progress reporter for tracking loading progress. Receives <see cref="LoadProgress"/> updates.</param>
        /// <param name="headers">Optional HTTP headers to include in the request (e.g., authentication tokens).</param>
        /// <returns>Coroutine enumerator that should be started with <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>.</returns>
        IEnumerator Load(
            string url,
            Action<Texture2D> onComplete,
            Action<FailureReason> onFailed,
            IProgress<LoadProgress> progress = null,
            Dictionary<string, string> headers = null);

        /// <summary>
        /// Cancels any in-progress load operation.
        /// After cancellation, <paramref name="onComplete"/> will be called with null.
        /// </summary>
        void CancelLoad();
    }

    /// <summary>
    /// Represents the progress of a texture loading operation.
    /// Used with <see cref="IProgress{T}"/> to report loading status.
    /// </summary>
    public class LoadProgress
    {
        /// <summary>
        /// Gets or sets the loading progress as a value between 0.0 and 1.0.
        /// </summary>
        public float Progress { get; set; }

        /// <summary>
        /// Gets or sets the current attempt number (1-based).
        /// </summary>
        public int Attempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the maximum number of retry attempts has been reached.
        /// </summary>
        public bool MaxAttemptReached { get; set; }

        /// <summary>
        /// Updates the attempt number and returns this instance for method chaining.
        /// </summary>
        /// <param name="attempt">The new attempt number.</param>
        /// <returns>This instance for method chaining.</returns>
        public LoadProgress UpdateAttempt(int attempt)
        {
            Attempt = attempt;
            return this;
        }

        /// <summary>
        /// Updates the max attempt reached flag and returns this instance for method chaining.
        /// </summary>
        /// <param name="maxAttemptReached">Whether the maximum attempts have been reached.</param>
        /// <returns>This instance for method chaining.</returns>
        public LoadProgress UpdateMaxAttemptReached(bool maxAttemptReached)
        {
            MaxAttemptReached = maxAttemptReached;
            return this;
        }

        /// <summary>
        /// Updates the progress value and returns this instance for method chaining.
        /// </summary>
        /// <param name="progress">The new progress value (0.0 to 1.0).</param>
        /// <returns>This instance for method chaining.</returns>
        public LoadProgress UpdateProgress(float progress)
        {
            Progress = progress;
            return this;
        }
    }

    /// <summary>
    /// Enumeration of possible reasons for texture loading failure.
    /// </summary>
    public enum FailureReason
    {
        /// <summary>
        /// Network request failed (e.g., connection error, timeout, HTTP error status).
        /// </summary>
        RequestError,

        /// <summary>
        /// Maximum number of retry attempts has been reached without success.
        /// </summary>
        MaxAttemptReached,

        /// <summary>
        /// The server returned an empty response or no data was received.
        /// </summary>
        EmptyResponse,

        /// <summary>
        /// The loading operation was cancelled by calling <see cref="ITextureLoader.CancelLoad"/>.
        /// </summary>
        Canceled,

        /// <summary>
        /// An unexpected error occurred (e.g., decoding failure, invalid format).
        /// </summary>
        Other
    }
}
