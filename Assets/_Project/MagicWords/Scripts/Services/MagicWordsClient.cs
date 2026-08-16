using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace SoftGames.MagicWords
{
    // Fetches and parses the dialogue payload. Knows nothing about the scene.
    public sealed class MagicWordsClient
    {
        private readonly string _endpoint;
        private readonly int _timeoutSeconds;
        private readonly int _retryCount;
        private readonly float _retryDelaySeconds;

        public MagicWordsClient(string endpoint, int timeoutSeconds, int retryCount, float retryDelaySeconds)
        {
            _endpoint = endpoint;
            _timeoutSeconds = timeoutSeconds;
            _retryCount = retryCount;
            _retryDelaySeconds = retryDelaySeconds;
        }

        public async Awaitable<FetchOutcome> FetchAsync(CancellationToken cancellationToken)
        {
            string lastError = "Request never ran.";

            for (int attempt = 0; attempt <= _retryCount; attempt++)
            {
                if (attempt > 0)
                {
                    await Awaitable.WaitForSecondsAsync(_retryDelaySeconds, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                using UnityWebRequest request = UnityWebRequest.Get(_endpoint);
                request.timeout = _timeoutSeconds;

                await request.SendWebRequest();
                cancellationToken.ThrowIfCancellationRequested();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    lastError = request.error;
                    continue;
                }

                if (TryParse(request.downloadHandler.text, out MagicWordsResponseDto response, out string parseError))
                {
                    return FetchOutcome.Ok(response);
                }

                lastError = parseError;
            }

            return FetchOutcome.Failed(lastError);
        }

        private static bool TryParse(string json, out MagicWordsResponseDto response, out string error)
        {
            response = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Response was empty.";
                return false;
            }

            try
            {
                response = JsonUtility.FromJson<MagicWordsResponseDto>(json);
            }
            catch (Exception exception)
            {
                error = $"Response was not valid JSON: {exception.Message}";
                return false;
            }

            if (response == null)
            {
                error = "Response did not match the expected shape.";
                return false;
            }

            return true;
        }

        public readonly struct FetchOutcome
        {
            private FetchOutcome(MagicWordsResponseDto data, string error)
            {
                Data = data;
                Error = error;
            }

            public MagicWordsResponseDto Data { get; }

            public string Error { get; }

            public bool Success => Data != null;

            public static FetchOutcome Ok(MagicWordsResponseDto data) => new(data, null);

            public static FetchOutcome Failed(string error) => new(null, error);
        }
    }
}
