using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gamenator.Core.TextureLoader.Utilities
{
    /// <summary>
    /// Provides utility methods for downscaling textures on CPU and GPU.
    /// Useful for memory optimization and texture size reduction.
    /// </summary>
    public static class TextureDownscaleUtility
    {
        // ---------------------------------------------------------------------
        // Public Methods
        // ---------------------------------------------------------------------

        /// <summary>
        /// Blits a source Texture2D into a temporarily allocated RenderTexture.
        /// </summary>
        /// <param name="source">The source texture to blit.</param>
        /// <param name="width">The target width of the RenderTexture.</param>
        /// <param name="height">The target height of the RenderTexture.</param>
        /// <returns>A temporary RenderTexture containing the blitted texture. Must be released with <see cref="RenderTexture.ReleaseTemporary(RenderTexture)"/>.</returns>
        public static RenderTexture BlitToRenderTexture(Texture2D source, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(
                width, height,
                0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            );
            Graphics.Blit(source, rt);
            return rt;
        }

        /// <summary>
        /// Creates a new empty Texture2D with RGBA32 format in linear color space.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>A new Texture2D instance.</returns>
        public static Texture2D CreateTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        }

        /// <summary>
        /// Applies changes to the texture without generating mipmaps and makes it non-readable.
        /// This optimizes memory usage by preventing CPU access to the texture data.
        /// </summary>
        /// <param name="texture">The texture to apply changes to.</param>
        public static void ApplyTexture(Texture2D texture)
        {
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        }

        /// <summary>
        /// Downscales a texture asynchronously on the GPU using AsyncGPUReadback.
        /// This method is more efficient for large textures but requires GPU support.
        /// </summary>
        /// <param name="source">The source texture to downscale.</param>
        /// <param name="newWidth">The target width of the downscaled texture.</param>
        /// <param name="newHeight">The target height of the downscaled texture.</param>
        /// <param name="onDone">Callback invoked when downscaling is complete. Receives the downscaled texture or the original texture on error.</param>
        /// <returns>Coroutine enumerator that should be started with <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>.</returns>
        public static IEnumerator DownscaleOnGPU(Texture2D source, int newWidth, int newHeight, Action<Texture2D> onDone)
        {
            var rt = BlitToRenderTexture(source, newWidth, newHeight);
            var request = AsyncGPUReadback.Request(rt, 0);

            yield return new UnityEngine.WaitUntil(() => request.done);

            if (request.hasError)
            {
                Debug.LogError("TextureDownscaleUtility.DownscaleOnGPU failed, returning original texture");
                onDone(source);
            }
            else
            {
                var raw = request.GetData<byte>(0);
                var newTex = CreateTexture(newWidth, newHeight);
                newTex.LoadRawTextureData(raw);
                ApplyTexture(newTex);

                Debug.Log("TextureDownscaleUtility.DownscaleOnGPU completed successfully");
                onDone(newTex);
            }

            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(source);
        }

        /// <summary>
        /// Downscales a texture synchronously on the CPU using ReadPixels.
        /// This method works on all platforms but may be slower for large textures.
        /// </summary>
        /// <param name="source">The source texture to downscale.</param>
        /// <param name="newWidth">The target width of the downscaled texture.</param>
        /// <param name="newHeight">The target height of the downscaled texture.</param>
        /// <returns>The downscaled Texture2D. The source texture is destroyed.</returns>
        public static Texture2D DownscaleWithReadPixels(Texture2D source, int newWidth, int newHeight)
        {
            var rt = BlitToRenderTexture(source, newWidth, newHeight);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var newTex = CreateTexture(newWidth, newHeight);
            newTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0, false);
            ApplyTexture(newTex);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            UnityEngine.Object.Destroy(source);

            Debug.Log("TextureDownscaleUtility.DownscaleWithReadPixels completed successfully");
            return newTex;
        }
    }
}
