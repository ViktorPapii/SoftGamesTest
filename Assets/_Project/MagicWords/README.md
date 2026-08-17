# Magic Words

One line of dialogue on screen at a time. The name plate carries the speaker's portrait and sits on
their side of the box, so the speaker reads at a glance without a second element competing with the
background. Not a scrolling chat: each line replaces the last, and the backlog lives behind the
**Log** button.

Scene: `Assets/_Project/MagicWords/Scenes/MagicWords.unity`. The run starts in
`MagicWordsController`'s private `Begin()`, which is also the popup's Retry after a failed fetch.

## Layers

```
Assets/_Project/MagicWords/Scripts/
├── Core/        no engine dependency, unit tested   (SoftGames.MagicWords.Core)
├── Services/    HTTP, textures, catalog asset
├── Views/       MonoBehaviours that draw things
└── MagicWordsController.cs                          (SoftGames.MagicWords.Runtime)
```

| Piece | Role |
|---|---|
| `DialogueScriptBuilder` | Payload to `DialogueScript`. All validation lives here. |
| `EmojiTextComposer` | One payload line to TMP rich text. Pure `string` to `string`. |
| `DialogueScript` | Lines, cast, and `PortraitRequests` — the only portraits worth fetching. |
| `MagicWordsClient` | Fetch + parse. Timeout, one retry, no scene knowledge. |
| `AvatarService` | Portrait downloads, caching and texture disposal. |
| `EmojiCatalog` | Token to Unicode table (ScriptableObject). |
| `DialogueBoxView` | The single visible line: name plate, portrait, typewriter. |
| `DialogueHistoryView` | Backlog overlay. |
| `ResponsiveLayout` | Landscape and portrait arrangements. |

`Core` carries no engine dependency, so the builder and the composer are unit tested without a
scene — see `Assets/_Project/MagicWords/Tests/EditMode/` (25 tests).

## What the payload does to you

The v3 endpoint is a fixture with teeth. Every one of these is handled and covered by a test:

- **`Sheldon` is listed twice** — once `left` with a working url, once `right` with
  `https://api.dicebear.com:81/blub`, which never connects. Avatars are **first wins**, so a later
  broken entry cannot replace a working one, and his side stays `left`.
- **`Neighbour` speaks but has no avatar entry** — no url and no position. Keeps the placeholder
  portrait, and his side — which edge of the box the name plate sits on — is resolved to **whichever
  side has fewer characters**, putting him opposite the two on the right. Sides are decided once, at
  build time, so nobody migrates mid-scene.
- **`Nobody` has an avatar but never speaks** — left off the stage, and never fetched: portraits are
  requested from `DialogueScript.PortraitRequests`, which is built from the cast rather than from the
  payload's avatars array. Both dead urls are gone before the network is touched.
- **`https://api.dicebear.com/5.x/personas/` returns 400 with JSON** — the response is checked for an
  `image/*` content type and a decodable texture, so a non-image never becomes a broken portrait.
- **Markup in the text** — literal runs are wrapped in `<noparse>`, so tags arriving from the endpoint
  render as text instead of executing. A `</noparse>` inside the payload is neutralised first.

Missing text drops the line; a missing name renders it as narration. Everything recoverable is
logged once, batched, at load.

## Emoji

v3 ships `{tokens}` and no emoji of its own, so the mapping is ours: `EmojiCatalog` turns
`{laughing}` into U+1F602. The composed string carries real Unicode, and TMP resolves it through
`EmojiAtlas.asset` — a sprite asset whose sprites are registered under their codepoints. Without it
a build with no system emoji font renders tofu. An unrecognised token is dropped and nothing is
drawn in its place — a stand-in glyph would put a mood in the line the payload never sent — and the
token is recorded for the load warning.

See `Assets/_Project/MagicWords/Art/Emoji/README.md` for the atlas and its licence.

## Ending

Both endings raise Core's shared completion popup, each handing it the retry that fits: a script that
played to the end replays from line one (`Replay`), while one that never arrived is fetched again
(`Begin`). The in-scene status panel is now only the *loading* state — the failure case moved to the
popup, which is also what gives a dead endpoint a way back to the menu.

## Playback

Auto-play is on by default and the timer never stops on its own: a tap **completes the reveal** if
the line is still typing, otherwise **advances**. Pausing is only the explicit toggle, so there is no
hidden state. Auto-play also holds while the backlog is open or the app loses focus.

Hold per line is `clamp(base + characters x perCharacter, min, max)`, all four tunable on the
controller.

## Layout

`ResponsiveLayout` swaps anchors, offsets and scale per aspect — a centred box in landscape, a taller
full-width one in portrait. Arrange the scene, then use the component's context menu
(**Capture current as landscape** / **portrait**) to record the arrangement.
