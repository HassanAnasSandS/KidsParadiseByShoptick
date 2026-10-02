using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KidsParadiseByShoptick.Infrastructure.Services;

/// <summary>
/// Free offline fingerprinting:
/// 1) 64-bit difference hash (near-exact same file / lightly edited)
/// 2) CLIP ViT-B/32 vision embedding via free ONNX model (same product, different photo)
/// Model auto-downloads once from Hugging Face (Xenova quantized CLIP, Apache/MIT-friendly weights).
/// </summary>
public sealed class ImageFingerprintService : IImageFingerprintService, IDisposable
{
    // CLIP ViT-B/32 vision encoder (quantized ~87MB). Outputs last_hidden_state; we take CLS token.
    private const string ModelFileName = "clip-vit-base-patch32-vision_quantized.onnx";
    private const string ModelDownloadUrl =
        "https://huggingface.co/Xenova/clip-vit-base-patch32/resolve/main/onnx/vision_model_quantized.onnx";

    private static readonly float[] ClipMean = [0.48145466f, 0.4578275f, 0.40821073f];
    private static readonly float[] ClipStd = [0.26862954f, 0.26130258f, 0.27577711f];

    private readonly string _modelPath;
    private readonly ILogger<ImageFingerprintService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private InferenceSession? _session;
    private string? _inputName;
    private int _embeddingDims = 768;
    private bool _downloadFailed;

    public ImageFingerprintService(IConfiguration configuration, ILogger<ImageFingerprintService> logger)
    {
        _logger = logger;
        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        var modelsDir = Path.Combine(basePath, "models");
        Directory.CreateDirectory(modelsDir);
        _modelPath = Path.Combine(modelsDir, ModelFileName);
    }

    public int EmbeddingDimensions => _embeddingDims;
    public bool IsEmbeddingReady => _session is not null;

    public async Task EnsureEmbeddingModelAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null || _downloadFailed)
            return;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_session is not null || _downloadFailed)
                return;

            if (!File.Exists(_modelPath))
            {
                _logger.LogInformation("Downloading free CLIP vision ONNX model to {Path}…", _modelPath);
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    await using var remote = await http.GetStreamAsync(ModelDownloadUrl, cancellationToken);
                    var temp = _modelPath + ".tmp";
                    await using (var file = File.Create(temp))
                        await remote.CopyToAsync(file, cancellationToken);
                    File.Move(temp, _modelPath, overwrite: true);
                    _logger.LogInformation("CLIP model downloaded successfully.");
                }
                catch (Exception ex)
                {
                    _downloadFailed = true;
                    _logger.LogWarning(ex,
                        "Could not download CLIP model. Image search will use perceptual hash only until the model is available at {Path}.",
                        _modelPath);
                    return;
                }
            }

            try
            {
                var options = new SessionOptions();
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                _session = new InferenceSession(_modelPath, options);
                _inputName = _session.InputMetadata.Keys.First();
                var outputMeta = _session.OutputMetadata.Values.First();
                // last_hidden_state is typically [batch, seq, hidden]; hidden = last dim
                var dims = outputMeta.Dimensions;
                if (dims.Length >= 1)
                {
                    var last = dims[^1];
                    if (last > 0) _embeddingDims = last;
                }
                _logger.LogInformation("CLIP ONNX session ready (input={Input}, dims={Dims}).", _inputName, _embeddingDims);
            }
            catch (Exception ex)
            {
                _downloadFailed = true;
                _logger.LogWarning(ex, "Failed to load CLIP ONNX model from {Path}.", _modelPath);
                _session?.Dispose();
                _session = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public long ComputePerceptualHash(Stream imageStream)
    {
        using var image = Image.Load<Rgba32>(imageStream);
        return ComputeDifferenceHash(image);
    }

    public long ComputePerceptualHashFromFile(string absolutePath)
    {
        using var image = Image.Load<Rgba32>(absolutePath);
        return ComputeDifferenceHash(image);
    }

    public async Task<float[]?> ComputeEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        await EnsureEmbeddingModelAsync(cancellationToken);
        if (_session is null || _inputName is null)
            return null;

        using var image = await Image.LoadAsync<Rgba32>(imageStream, cancellationToken);
        return RunEmbedding(image);
    }

    public async Task<float[]?> ComputeEmbeddingFromFileAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        await EnsureEmbeddingModelAsync(cancellationToken);
        if (_session is null || _inputName is null)
            return null;

        using var image = await Image.LoadAsync<Rgba32>(absolutePath, cancellationToken);
        return RunEmbedding(image);
    }

    private float[]? RunEmbedding(Image<Rgba32> image)
    {
        if (_session is null || _inputName is null)
            return null;

        // CLIP preprocess: resize shortest side then center-crop to 224, normalize.
        using var clone = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(224, 224),
                Mode = ResizeMode.Crop,
                Sampler = KnownResamplers.Bicubic
            });
        });

        var tensor = new DenseTensor<float>([1, 3, 224, 224]);
        for (var y = 0; y < 224; y++)
        {
            for (var x = 0; x < 224; x++)
            {
                var p = clone[x, y];
                tensor[0, 0, y, x] = ((p.R / 255f) - ClipMean[0]) / ClipStd[0];
                tensor[0, 1, y, x] = ((p.G / 255f) - ClipMean[1]) / ClipStd[1];
                tensor[0, 2, y, x] = ((p.B / 255f) - ClipMean[2]) / ClipStd[2];
            }
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, tensor)
        };

        using var results = _session.Run(inputs);
        var output = results.First().AsTensor<float>();
        var dims = output.Dimensions.ToArray();

        // Prefer CLS token: [1, seq, hidden] → index 0
        float[] embedding;
        if (dims.Length == 3)
        {
            var hidden = dims[2];
            embedding = new float[hidden];
            for (var i = 0; i < hidden; i++)
                embedding[i] = output[0, 0, i];
            _embeddingDims = hidden;
        }
        else if (dims.Length == 2)
        {
            var hidden = dims[1];
            embedding = new float[hidden];
            for (var i = 0; i < hidden; i++)
                embedding[i] = output[0, i];
            _embeddingDims = hidden;
        }
        else
        {
            embedding = output.ToArray();
            _embeddingDims = embedding.Length;
        }

        // L2 normalize
        double norm = 0;
        for (var i = 0; i < embedding.Length; i++)
            norm += embedding[i] * embedding[i];
        norm = Math.Sqrt(norm);
        if (norm > 1e-8)
        {
            for (var i = 0; i < embedding.Length; i++)
                embedding[i] = (float)(embedding[i] / norm);
        }

        return embedding;
    }

    /// <summary>Difference hash: 9x8 grayscale, compare adjacent pixels → 64 bits.</summary>
    private static long ComputeDifferenceHash(Image<Rgba32> image)
    {
        using var small = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(9, 8),
                Mode = ResizeMode.Stretch,
                Sampler = KnownResamplers.Triangle
            });
            ctx.Grayscale();
        });

        ulong hash = 0;
        var bit = 0;
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var left = small[x, y].R;
                var right = small[x + 1, y].R;
                if (left > right)
                    hash |= 1UL << bit;
                bit++;
            }
        }

        return unchecked((long)hash);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _gate.Dispose();
    }
}
