# Texture Loader

Modular texture loading system for Unity with support for standard formats and KTX2/Basis Universal compression. Features retry logic, progress reporting, cancellation support, and GPU/CPU downscaling utilities.

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Usage](#-usage)
- [API Reference](#-api-reference)
- [Examples](#-examples)
- [Dependencies](#-dependencies)
- [Performance](#-performance)

## 🎯 Overview

Texture Loader provides a clean, modular interface for loading textures asynchronously in Unity. It supports:

- **Standard formats** (PNG, JPG, etc.) via `StandardTextureLoader`
- **KTX2/Basis Universal** compressed textures via `KtxLoader` (requires [KtxUnity](https://github.com/atteneder/KtxUnity))
- **Automatic retry logic** with configurable attempts and delays
- **Progress reporting** for download and decode phases
- **Cancellation support** for long-running operations
- **Memory optimization** options (non-readable textures)
- **GPU/CPU downscaling utilities** for texture size reduction

## ✨ Features

### Core Features

- **Modular architecture** - Clean interface-based design with pluggable loaders
- **Retry mechanism** - Automatic retries with configurable attempts and delays
- **Progress tracking** - Real-time progress reporting for download and decode phases
- **Cancellation support** - Cancel in-progress loads at any time
- **Error handling** - Comprehensive error reporting with specific failure reasons
- **Memory optimization** - Option to create non-readable textures for reduced memory footprint

### Loader Types

- **StandardTextureLoader** - Loads standard image formats using Unity's `UnityWebRequestTexture`
- **KtxLoader** - Loads KTX2/Basis Universal compressed textures with configurable options

### Utilities

- **TextureDownscaleUtility** - GPU and CPU-based texture downscaling methods

## 📦 Installation

### Step 1: Install KtxUnity (Required for KtxLoader)

**This package requires [KtxUnity](https://github.com/atteneder/KtxUnity) to be installed separately.** KtxUnity is needed only if you plan to use `KtxLoader` for loading KTX2/Basis Universal textures.

#### Option A: Install via OpenUPM (Recommended)

1. Add scoped registries to your project's `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.atteneder.ktx"
      ]
    },
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.atteneder"
      ]
    }
  ]
}
```

2. Add KtxUnity to dependencies in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.atteneder.ktx": "2.2.3"
  }
}
```

#### Option B: Install from GitHub

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.atteneder.ktx": "https://github.com/atteneder/KtxUnity.git"
  }
}
```

### Step 2: Install Texture Loader

#### Option 1: Unity Package Manager (Git URL)

1. Open Unity Package Manager (Window → Package Manager)
2. Click the **+** button → **Add package from git URL**
3. Enter: `https://github.com/v17alya/unity-texture-loader.git`

#### Option 2: Manual Installation via manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gamenator.texture-loader": "https://github.com/v17alya/unity-texture-loader.git"
  }
}
```

#### Option 3: Manual Installation (Copy Files)

1. Clone or download this repository
2. Copy the `Runtime` folder into your project's `Packages` or `Assets` folder
3. Ensure KtxUnity is installed (see Step 1 above)

### Complete Example: manifest.json

If you want to install both packages at once via OpenUPM:

```json
{
  "dependencies": {
    "com.gamenator.texture-loader": "https://github.com/v17alya/unity-texture-loader.git",
    "com.atteneder.ktx": "2.2.3"
  },
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.atteneder.ktx"
      ]
    },
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.atteneder"
      ]
    }
  ]
}
```

## 📁 Project Structure

```
Runtime/
├── Core/
│   ├── ITextureLoader.cs          # Main interface and core types
│   └── BaseTextureLoader.cs       # Abstract base class with retry logic
├── Loaders/
│   ├── StandardTextureLoader.cs   # Standard format loader (PNG, JPG, etc.)
│   └── KtxLoader.cs               # KTX2/Basis Universal loader
├── Utilities/
│   └── TextureDownscaleUtility.cs # GPU/CPU texture downscaling utilities
└── Gamenator.TextureLoader.asmdef # Assembly definition
```

## 🚀 Quick Start

```csharp
using System.Collections;
using Gamenator.Core.TextureLoader;
using UnityEngine;

public class TextureLoaderExample : MonoBehaviour
{
    private ITextureLoader _loader;

    void Start()
    {
        // Create a standard texture loader with 3 retries, 1 second delay
        _loader = new StandardTextureLoader(
            maxRetry: 3,
            delayBetweenRetry: 1f,
            nonReadable: true
        );

        // Start loading
        StartCoroutine(LoadTexture("https://example.com/texture.png"));
    }

    IEnumerator LoadTexture(string url)
    {
        Texture2D texture = null;
        FailureReason? failureReason = null;

        yield return _loader.Load(
            url: url,
            onComplete: tex => texture = tex,
            onFailed: reason => failureReason = reason,
            progress: new Progress<LoadProgress>(p => 
                Debug.Log($"Progress: {p.Progress:P0}, Attempt: {p.Attempt}")
            )
        );

        if (texture != null)
        {
            Debug.Log("Texture loaded successfully!");
            // Use texture...
        }
        else if (failureReason.HasValue)
        {
            Debug.LogError($"Failed to load texture: {failureReason.Value}");
        }
    }
}
```

## 📖 Usage

### Standard Texture Loader

```csharp
var loader = new StandardTextureLoader(
    maxRetry: 3,              // Maximum retry attempts
    delayBetweenRetry: 1f,    // Delay between retries (seconds)
    nonReadable: true         // Create non-readable texture (memory optimization)
);

yield return loader.Load(
    url: "https://example.com/image.png",
    onComplete: texture => { /* Handle success */ },
    onFailed: reason => { /* Handle failure */ },
    progress: new Progress<LoadProgress>(p => { /* Track progress */ }),
    headers: new Dictionary<string, string> { { "Authorization", "Bearer token" } }
);
```

### KTX2 Texture Loader

```csharp
var ktxLoader = new KtxLoader(
    maxRetry: 3,
    delayBetweenRetry: 1f,
    nonReadable: true,
    imageIndex: 0,           // Image index for texture arrays
    faceSlice: 0,            // Face slice for cubemaps
    mipLimit: 0,             // Maximum mip level to load
    importMip: true,         // Import mipmaps from KTX2
    linear: false            // sRGB color space (false) or linear (true)
);

yield return ktxLoader.Load(
    url: "https://example.com/texture.ktx2",
    onComplete: texture => { /* Handle success */ },
    onFailed: reason => { /* Handle failure */ }
);
```

### Cancellation

```csharp
// Start loading
StartCoroutine(loader.Load(url, onComplete, onFailed));

// Cancel at any time
loader.CancelLoad();
```

### Progress Tracking

```csharp
var progress = new Progress<LoadProgress>(p =>
{
    Debug.Log($"Progress: {p.Progress:P0}");
    Debug.Log($"Attempt: {p.Attempt}");
    Debug.Log($"Max attempts reached: {p.MaxAttemptReached}");
});

yield return loader.Load(url, onComplete, onFailed, progress);
```

### Texture Downscaling

```csharp
using Gamenator.Core.TextureLoader.Utilities;

// GPU-based downscaling (async)
yield return TextureDownscaleUtility.DownscaleOnGPU(
    source: originalTexture,
    newWidth: 512,
    newHeight: 512,
    onDone: downscaledTexture => { /* Use downscaled texture */ }
);

// CPU-based downscaling (sync)
var downscaled = TextureDownscaleUtility.DownscaleWithReadPixels(
    source: originalTexture,
    newWidth: 512,
    newHeight: 512
);
```

## 📚 API Reference

### ITextureLoader

Main interface for texture loading operations.

#### Methods

- `IEnumerator Load(string url, Action<Texture2D> onComplete, Action<FailureReason> onFailed, IProgress<LoadProgress> progress = null, Dictionary<string, string> headers = null)`
  - Loads a texture asynchronously from the given URL.

- `void CancelLoad()`
  - Cancels any in-progress load operation.

### LoadProgress

Represents the progress of a texture loading operation.

#### Properties

- `float Progress` - Loading progress (0.0 to 1.0)
- `int Attempt` - Current attempt number (1-based)
- `bool MaxAttemptReached` - Whether maximum retry attempts have been reached

### FailureReason

Enumeration of possible failure reasons.

#### Values

- `RequestError` - Network request failed
- `MaxAttemptReached` - Maximum retry attempts reached
- `EmptyResponse` - Server returned empty response
- `Canceled` - Operation was cancelled
- `Other` - Unexpected error (e.g., decoding failure)

### StandardTextureLoader

Standard texture loader for PNG, JPG, and other common formats.

#### Constructor

```csharp
StandardTextureLoader(int maxRetry, float delayBetweenRetry, bool nonReadable)
```

### KtxLoader

KTX2/Basis Universal texture loader.

#### Constructor

```csharp
KtxLoader(
    int maxRetry,
    float delayBetweenRetry,
    bool nonReadable,
    uint imageIndex,
    uint faceSlice,
    uint mipLimit,
    bool importMip,
    bool linear
)
```

### TextureDownscaleUtility

Static utility class for texture downscaling.

#### Methods

- `RenderTexture BlitToRenderTexture(Texture2D source, int width, int height)`
- `Texture2D CreateTexture(int width, int height)`
- `void ApplyTexture(Texture2D texture)`
- `IEnumerator DownscaleOnGPU(Texture2D source, int newWidth, int newHeight, Action<Texture2D> onDone)`
- `Texture2D DownscaleWithReadPixels(Texture2D source, int newWidth, int newHeight)`

## 💡 Examples

### Loading with Retry and Progress

```csharp
IEnumerator LoadWithRetry(string url)
{
    var loader = new StandardTextureLoader(maxRetry: 5, delayBetweenRetry: 2f, nonReadable: true);
    var progress = new Progress<LoadProgress>(p =>
    {
        Debug.Log($"Loading: {p.Progress:P0} (Attempt {p.Attempt}/{loader.MaxRetry})");
    });

    yield return loader.Load(
        url: url,
        onComplete: texture =>
        {
            if (texture != null)
            {
                GetComponent<Renderer>().material.mainTexture = texture;
            }
        },
        onFailed: reason => Debug.LogError($"Failed: {reason}"),
        progress: progress
    );
}
```

### Loading with Authentication

```csharp
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer " + authToken },
    { "X-API-Key", apiKey }
};

yield return loader.Load(url, onComplete, onFailed, progress: null, headers: headers);
```

### Custom Loader Implementation

```csharp
public class CustomTextureLoader : BaseTextureLoader
{
    public CustomTextureLoader(int maxRetry, float delayBetweenRetry, bool nonReadable)
        : base(maxRetry, delayBetweenRetry, nonReadable)
    {
    }

    protected override IEnumerator RunSingleAttempt(
        string url,
        int attempt,
        Dictionary<string, string> headers,
        Action<Texture2D> onComplete,
        Action<FailureReason> onFailed,
        IProgress<LoadProgress> progress)
    {
        // Implement custom loading logic
        // Call onComplete(texture) on success
        // Call onFailed(reason) on failure
        // Report progress via progress?.Report(...)
        yield break;
    }
}
```

## 🔗 Dependencies

### Required

- **Unity 2021.3 or later**

### Optional (Required for KtxLoader)

- **[KtxUnity (com.atteneder.ktx)](https://github.com/atteneder/KtxUnity)** - Required only if you plan to use `KtxLoader` for loading KTX2/Basis Universal textures

#### Installing KtxUnity

KtxUnity must be installed separately. See the [Installation](#-installation) section above for detailed instructions.

**Quick Setup:**

1. Add scoped registries to `Packages/manifest.json` (see Installation section)
2. Add `"com.atteneder.ktx": "2.2.3"` to dependencies

Or install directly from GitHub:
```json
{
  "dependencies": {
    "com.atteneder.ktx": "https://github.com/atteneder/KtxUnity.git"
  }
}
```

### Optional

- **[Gamenator.Logger](https://github.com/v17alya/UnityLogger)** - Recommended for structured logging (optional, can use `UnityEngine.Debug` instead)

## ⚡ Performance

### Memory Optimization

- Use `nonReadable: true` to create textures that cannot be read back by the CPU, reducing memory usage
- Use `TextureDownscaleUtility` to reduce texture size before loading
- Consider using KTX2/Basis Universal for better compression ratios

### Best Practices

- **Cancel unused loads** - Always cancel loads when objects are destroyed or no longer needed
- **Reuse loaders** - Create loader instances once and reuse them
- **Monitor progress** - Use progress reporting to show loading UI to users
- **Handle errors** - Always provide error callbacks to handle failures gracefully

## 📝 License

MIT License - see LICENSE file for details.

## 👤 Author

**Vitalii Novosad**

- GitHub: [@v17alya](https://github.com/v17alya)

## 🙏 Acknowledgments

- [KtxUnity (com.atteneder.ktx)](https://github.com/atteneder/KtxUnity) version 2.2.3 or higher for KTX2/Basis Universal support (available via OpenUPM)
