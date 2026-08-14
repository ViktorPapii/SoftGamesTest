using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace SoftGames.MagicWords
{
    /// <summary>
    /// Downloads avatar portraits, one sprite per url. A settled url is never fetched again, and
    /// everything created here is destroyed on Dispose — downloaded textures are not managed assets
    /// and would otherwise outlive the scene. Requests issued for the same url before the first one
    /// settles do each download; the extra results are still tracked, so nothing leaks.
    /// </summary>
    public sealed class AvatarService : IDisposable
    {
        private readonly int _timeoutSeconds;
        private readonly Dictionary<string, Sprite> _cache = new();
        private readonly List<Texture2D> _textures = new();
        private readonly List<Sprite> _sprites = new();

        private bool _disposed;

        public AvatarService(int timeoutSeconds)
        {
            _timeoutSeconds = timeoutSeconds;
        }

        public async Awaitable<Sprite> LoadAsync(string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (_cache.TryGetValue(url, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = await DownloadAsync(url, cancellationToken);

            // Dispose ran while this was in flight, so its lists have already been emptied.
            if (_disposed)
            {
                Discard(sprite);
                return null;
            }

            _cache[url] = sprite;
            return sprite;
        }

        private async Awaitable<Sprite> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, nonReadable: true);
            request.timeout = _timeoutSeconds;

            await request.SendWebRequest();
            cancellationToken.ThrowIfCancellationRequested();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AvatarService] {url} failed: {request.error}. Using a placeholder.");
                return null;
            }

            // A 200 carrying JSON or html still hands back a 8x8 error texture.
            string contentType = request.GetResponseHeader("Content-Type");
            if (contentType == null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[AvatarService] {url} returned '{contentType}', not an image. Using a placeholder.");
                return null;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null || texture.width <= 8 || texture.height <= 8)
            {
                Debug.LogWarning($"[AvatarService] {url} did not decode into a usable image. Using a placeholder.");
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            _textures.Add(texture);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            _sprites.Add(sprite);
            return sprite;
        }

        private static void Discard(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);

            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (Sprite sprite in _sprites)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
            }

            foreach (Texture2D texture in _textures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            _cache.Clear();
            _sprites.Clear();
            _textures.Clear();
        }
    }
}
