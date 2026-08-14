using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace SoftGames.MagicWords
{
    /// <summary>
    /// Loads the payload, stages the cast and plays the script one line at a time. Every public
    /// method here is an inspector-wired UnityEvent target (advance, auto-play, history, retry).
    /// </summary>
    public class MagicWordsController : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Loading,
            Playing,
            Finished,
            Failed
        }

        [Header("Data")]
        [SerializeField] private string endpoint = "https://private-624120-softgamesassignment.apiary-mock.com/v3/magicwords";

        [SerializeField] private EmojiCatalog emojiCatalog;

        [Min(1)]
        [SerializeField] private int requestTimeoutSeconds = 10;

        [Min(0)]
        [SerializeField] private int retryCount = 1;

        [Min(0f)]
        [SerializeField] private float retryDelaySeconds = 1f;

        [Tooltip("Per avatar. A url that never connects is abandoned after this.")]
        [Min(1)]
        [SerializeField] private int avatarTimeoutSeconds = 5;

        [Tooltip("Total wait for portraits before starting anyway. Late arrivals still swap in.")]
        [Min(0f)]
        [SerializeField] private float avatarBudgetSeconds = 8f;

        [Header("Scene references")]
        [SerializeField] private DialogueBoxView box;

        [SerializeField] private DialogueHistoryView history;
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private GameObject retryButton;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private TMP_Text autoPlayLabel;

        [Header("Playback")]
        [SerializeField] private bool autoPlay = true;

        [Tooltip("Seconds a fully revealed line is held before auto-advancing, plus per character.")]
        [Min(0f)]
        [SerializeField] private float autoPlayBaseHold = 1f;

        [Min(0f)]
        [SerializeField] private float autoPlayPerCharacter = 0.02f;

        [Min(0f)]
        [SerializeField] private float autoPlayMinHold = 1.4f;

        [Min(0f)]
        [SerializeField] private float autoPlayMaxHold = 3.5f;

        [Tooltip("Start on scene load. Turn off when a GameManager owns the load sequence.")]
        [SerializeField] private bool autoStart = true;

        private readonly Dictionary<string, Sprite> _portraits = new(StringComparer.OrdinalIgnoreCase);

        private AvatarService _avatarService;
        private DialogueScript _script;
        private State _state = State.Idle;
        private int _index;
        private float _hold;
        private bool _hasFocus = true;

        public void Begin()
        {
            if (_state == State.Loading)
            {
                return;
            }

            _ = LoadAndPlayAsync();
        }

        public void Advance()
        {
            if (_state != State.Playing || history.IsOpen)
            {
                return;
            }

            if (box.IsRevealing)
            {
                box.CompleteReveal();
                return;
            }

            if (_index + 1 >= _script.Lines.Count)
            {
                Finish();
                return;
            }

            ShowLine(_index + 1);
        }

        public void ToggleAutoPlay()
        {
            autoPlay = !autoPlay;
            UpdateAutoPlayLabel();
        }

        public void ToggleHistory()
        {
            history.Toggle();
        }

        public void Replay()
        {
            if (_script == null || _script.IsEmpty)
            {
                Begin();
                return;
            }

            history.Clear();
            endPanel.SetActive(false);
            _state = State.Playing;
            ShowLine(0);
        }

        private async Awaitable LoadAndPlayAsync()
        {
            CancellationToken token = destroyCancellationToken;

            try
            {
                EnterLoadingState();

                MagicWordsClient client = new(endpoint, requestTimeoutSeconds, retryCount, retryDelaySeconds);
                MagicWordsClient.FetchOutcome outcome = await client.FetchAsync(token);

                if (!outcome.Success)
                {
                    Fail($"Could not reach the dialogue service.\n{outcome.Error}");
                    return;
                }

                EmojiTextComposer composer = new(emojiCatalog);
                DialogueScriptBuilder builder = new();
                _script = builder.Build(outcome.Data, composer);
                ReportPayloadIssues(builder.Warnings, composer.UnknownTokens);

                if (_script.IsEmpty)
                {
                    Fail("The dialogue service returned no lines to play.");
                    return;
                }

                await LoadPortraitsAsync(token);

                statusPanel.SetActive(false);
                _state = State.Playing;
                ShowLine(0);
            }
            catch (OperationCanceledException)
            {
                // The scene went away mid-request; nothing left to show it to.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Fail("Something went wrong while loading the dialogue.");
            }
        }

        private void EnterLoadingState()
        {
            _state = State.Loading;
            _script = null;
            _portraits.Clear();

            _avatarService?.Dispose();
            _avatarService = new AvatarService(avatarTimeoutSeconds);

            box.Clear();
            history.Clear();
            history.Close();
            endPanel.SetActive(false);
            statusPanel.SetActive(true);
            retryButton.SetActive(false);
            statusLabel.text = "Loading dialogue…";
            UpdateAutoPlayLabel();
        }

        private async Awaitable LoadPortraitsAsync(CancellationToken token)
        {
            statusLabel.text = "Loading portraits…";

            // Scoped to this load: a request left over from an abandoned one cannot decrement it.
            int pending = _script.PortraitRequests.Count;

            foreach (DialogueCharacter character in _script.PortraitRequests)
            {
                _ = LoadPortraitAsync(character);
            }

            float deadline = Time.realtimeSinceStartup + avatarBudgetSeconds;
            while (pending > 0 && Time.realtimeSinceStartup < deadline)
            {
                await Awaitable.NextFrameAsync(token);
            }

            if (pending > 0)
            {
                Debug.LogWarning($"[{name}] {pending} portrait(s) still loading after " +
                                 $"{avatarBudgetSeconds}s; starting on placeholders.", this);
            }

            async Awaitable LoadPortraitAsync(DialogueCharacter character)
            {
                try
                {
                    Sprite sprite = await _avatarService.LoadAsync(character.AvatarUrl, token);
                    if (sprite != null)
                    {
                        AdoptPortrait(character, sprite);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
                finally
                {
                    pending--;
                }
            }
        }

        // Runs after the load budget too, so a slow portrait still reaches what is already on screen.
        private void AdoptPortrait(DialogueCharacter character, Sprite sprite)
        {
            _portraits[character.Name] = sprite;
            history.SetPortrait(character.Name, sprite);

            if (_state == State.Playing && _script != null && _script.Lines[_index].Speaker == character)
            {
                box.SetPortrait(sprite);
            }
        }

        private void ShowLine(int index)
        {
            _index = index;
            DialogueLine line = _script.Lines[index];

            Sprite portrait = PortraitFor(line.Speaker);
            box.Show(line, portrait, index, _script.Lines.Count);
            history.Append(line, portrait);

            _hold = Mathf.Clamp(autoPlayBaseHold + box.CharacterCount * autoPlayPerCharacter,
                autoPlayMinHold, autoPlayMaxHold);
        }

        private Sprite PortraitFor(DialogueCharacter character)
        {
            if (character == null)
            {
                return null;
            }

            _portraits.TryGetValue(character.Name, out Sprite sprite);
            return sprite;
        }

        private void Finish()
        {
            _state = State.Finished;
            endPanel.SetActive(true);
        }

        private void Fail(string message)
        {
            _state = State.Failed;
            statusPanel.SetActive(true);
            retryButton.SetActive(true);
            statusLabel.text = message;
        }

        private void ReportPayloadIssues(IReadOnlyList<string> warnings, IReadOnlyList<string> unknownTokens)
        {
            if (warnings.Count > 0)
            {
                Debug.LogWarning($"[{name}] Payload issues:\n - {string.Join("\n - ", warnings)}", this);
            }

            if (unknownTokens.Count > 0)
            {
                Debug.LogWarning($"[{name}] Emoji catalog has no entry for: {string.Join(", ", unknownTokens)}.", this);
            }
        }

        private void UpdateAutoPlayLabel()
        {
            if (autoPlayLabel != null)
            {
                autoPlayLabel.text = autoPlay ? "Auto: on" : "Auto: off";
            }
        }

        private void Update()
        {
            if (_state != State.Playing || box.IsRevealing || !autoPlay || history.IsOpen || !_hasFocus)
            {
                return;
            }

            _hold -= Time.deltaTime;
            if (_hold <= 0f)
            {
                Advance();
            }
        }

        private void Start()
        {
            if (emojiCatalog == null)
            {
                Debug.LogError($"[{name}] No emoji catalog assigned; every token will be dropped.", this);
            }

            if (autoStart)
            {
                Begin();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
        }

        private void OnApplicationPause(bool paused)
        {
            _hasFocus = !paused;
        }

        private void OnDestroy()
        {
            _avatarService?.Dispose();
        }
    }
}
