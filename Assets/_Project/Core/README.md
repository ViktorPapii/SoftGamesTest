# Core

Everything that is not one of the three tasks: the menu, scene navigation, and the chrome that has
to be on screen in all of them.

## Layout

Each task owns a folder under `Assets/_Project/` with the same shape, so nothing is shared by
accident and nothing has to be hunted for:

```
Assets/_Project/
├── Core/            navigation, menu, HUD, the UI kit
├── AceOfShadows/
├── MagicWords/
└── PhoenixFlame/
        README.md  Animation/  Art/  Editor/  Prefabs/  Scenes/  ScriptableObjects/  Scripts/  Tests/
```

A folder only exists where it has content — an empty one is a directory git will not carry and Unity
will recreate. The vocabulary is fixed even where a game does not use all of it.

Core's scripts group by responsibility, each folder matching a section below:

```
Core/Scripts/
├── Bootstrap/    GameBootstrap, GameManager
├── Navigation/   IGameNavigation, SceneNavigationController, SceneCatalog, SceneFader
├── Hud/          GameHud, FpsCounter, ICompletionPopup, CompletionPopup
└── Menu/         MainMenuController, MenuEntryButton
```

Namespaces stay flat at `SoftGames.Core` — folders group files, assemblies draw the boundaries.

Assembly definitions follow the same lines: `SoftGames.Core`, `SoftGames.AceOfShadows.{Core,}`,
`SoftGames.MagicWords.{Core,Runtime}`, `SoftGames.PhoenixFlame`, plus an `.Editor` and `.Tests`
assembly per game where there is code for it. No game references another; `Core` references none of
them. The compiler enforces the folder boundary, so "a file ended up in the wrong task" is a build
error rather than a code review comment.

The two `.Core` assemblies set `noEngineReferences: true`: the card transfer rules and the dialogue
parser cannot reach UnityEngine at all, which is what lets both be tested in EditMode with no scene.

Outside `_Project` sit the things Unity or a vendor dictates: `Plugins/` (DOTween), `Settings/`
(URP, input actions), `Resources/` (DOTween's settings asset), `TextMesh Pro/`, and `Docs/` for the
assignment itself.

## Art and atlases

Every game keeps its images in `Art/Images/` and has one sprite atlas beside it:

| Atlas | Packs |
|---|---|
| `Core/Art/Core_Atlas` | the shared UI kit and the menu backdrop |
| `AceOfShadows/Art/AceOfShadows_Atlas` | 53 card faces |
| `MagicWords/Art/MagicWords_Atlas` | the placeholder portrait |
| `PhoenixFlame/Art/PhoenixFlame_Atlas` | the backdrop |

Each atlas's packable is the **folder**, not a list of sprites. Art dropped into `Images/` is
atlased from then on without anyone remembering to add it — which is the only version of this that
stays true six months later.

Sprite sources are imported **uncompressed**; the atlas carries the compression, crunched at 4096.
Compressing the source as well would throw away pixels before packing, and Unity warns about exactly
that. Both halves matter — uncompressed sources with an uncompressed atlas ships raw RGBA, which on
WebGL is download size and heap both.

Two sets of textures are deliberately outside this:

- **The Phoenix Flame particle sheets** (`flame_sheet`, `smoke_sheet`, `flame_glow`, `ember_dot`) are
  `Default` textures sampled by particle materials, not sprites. A sprite atlas cannot pack them, and
  they ship compressed on their own. They live in `Images/` with the rest; the packer skips them.
- **`MagicWords/Art/Emoji/EmojiAtlas.png`** is TextMeshPro's own sprite-asset sheet. TMP resolves
  emoji through its `TMP_SpriteAsset` and its own UV rects, so it must not be repacked. It is kept
  out of `Images/` for that reason.

## The persistent root

`Core/Prefabs/GameManager.prefab` is one root object with four children, and it is the only thing in
the game that survives a scene change:

```
GameManager                       ← DontDestroyOnLoad; GameManager, SceneNavigationController
├── EventSystem                   EventSystem + InputSystemUIInputModule
├── PopupCanvas      order 32740  CompletionPopup — scrim, message, Retry, Menu
├── HudCanvas        order 32745  GameHud, FpsCounter; ExitButton (plain uGUI Button)
└── FadeCanvas       order 32750  CanvasGroup, SceneFader, full-screen Cover image
```

The three canvases stack in the order they have to: the HUD draws over the popup, so the Menu button
stays reachable with a modal open, and the fader covers both — so a transition hides the chrome it is
about to reposition.

The frame rate readout and the Exit button live here rather than in each scene: one copy, present
everywhere, impossible to forget when adding a scene.

`GameHud` owns all of it — what the Exit button does, when it shows, and where the readout
sits. Nothing on the HUD decides any of that for itself, so the button is a plain uGUI `Button` with
no script of its own and the readout knows nothing about the button.

It asks the navigator two questions, `IsOnMenu` and `IsBusy`, through `IGameNavigation`, and is
told when to re-ask: `StateChanged` fires when `IsBusy` flips, which is the only moment either
answer can change — by the time a transition ends, `IsOnMenu` already describes the scene that
arrived. So the HUD has no `Update` at all. `IsOnMenu`
is answered by the navigator so no UI has to know a scene name, or that the catalog is where the
menu is recorded.

### Composition

`GameManager` is the composition root. It holds the concrete pieces and hands each one what it needs
in `Awake`:

```csharp
completion.Init(navigation);   // both as IGameNavigation
hud.Init(navigation);
```

`GameHud` owns its own chrome — the Exit button and the frame rate readout are its serialized views,
not things the root passes down.

Wiring happens there rather than in each component's own `Awake`, because Awake order within a
prefab is undefined — a component cannot rely on having been handed anything yet.

Everything below the root talks to `IGameNavigation` and `ICompletionPopup`, never to the classes
behind them. Each concrete type is named in exactly one place — its serialized field on
`GameManager` — because Unity cannot serialize an interface.

The navigator knows no UI at all. It raises `SceneChanging` behind the cover, and whoever holds
state belonging to the outgoing scene drops it there — that is how the popup clears its retry
callback without the navigator having heard of a popup.

The HUD sits **under** the fader. Scene chrome takes its place while `IsSwapping` is false but the
cover is still opaque, so the reveal brings the Exit button in with the scene instead of popping it
on afterwards — which only works if the cover is drawn over it. The frame rate readout is unaffected
either way: `FpsCounter` is IMGUI and draws outside the canvas system, so it stays legible across a
transition regardless of sorting order.

It also means the cover's `blocksRaycasts` genuinely covers the Exit button during a transition,
rather than the button relying on `Load`'s own `IsBusy` guard to ignore the click.

### The frame rate readout

`FpsCounter` draws with IMGUI, not uGUI. It sits on the HUD with no canvas of its own, so updating
it every frame costs no UI rebuild and can never dirty the canvas the Menu button lives on. IMGUI
also draws over every canvas, so the reading survives a fade.

That trade is not free in one direction only: the old label rebuilt a canvas twice a second, while
this runs the IMGUI event loop every frame for the life of the app. The `EventType.Repaint` guard
skips the layout pass, and the label string is only rebuilt when the rounded value changes, but on
WebGL per-frame IMGUI dispatch is plausibly the dearer of the two. It stays because the brief asks
for the frame rate on screen in the shipped build, so gating it on `Debug.isDebugBuild` is not an
option — but it is a cost, not a saving.

It reads every frame with no refresh window, smoothed by a time-weighted moving average — a slow
frame moves the number on the frame it lands, which a windowed mean would have averaged away. The
label string is rebuilt only when the rounded value changes.

It knows nothing about the Menu button. It exposes an `Anchor` rect to sit past, and `GameHud`
hands the button's rect over or takes it back, at which point the readout falls to the corner.
Measuring off a live rect is also what keeps the two aligned under any canvas scaler; a fixed pixel
offset drifts as soon as the resolution changes. It uses the kit's `.ttf` so it matches the rest of
the UI.

### Getting there from any scene

Every scene — the menu included — carries a `GameBootstrap`: a one-component prefab holding a direct
reference to the manager prefab. On `Awake` it instantiates the manager unless one is already alive,
so the first scene of a session builds it and every later one finds it already there.

A direct asset reference rather than `Resources.Load` by name: the link is visible in the inspector,
survives renaming or moving the prefab, and keeps the manager out of a `Resources` folder, which
ships whole and cannot be stripped. `[DefaultExecutionOrder(-1000)]` puts it ahead of anything that
reads `GameManager.Instance`, and since everything does that in `Start`, the manager is always there
in time.

The payoff is that pressing Play on `AceOfShadows.unity` directly gives the readout, the fader, the
popup and a working EventSystem, with no "you must start from the menu" rule to remember.

### Startup order

There is exactly one ordering rule, and Unity enforces it:

> **`GameManager.Instance` is null during `Awake` and guaranteed from every `Start`.**

`GameBootstrap` runs at `[DefaultExecutionOrder(-1000)]`, so it builds the manager before any other
`Awake`; `Instantiate` runs the manager's own `Awake` synchronously, which claims the singleton.
Everything that needs the manager asks in `Start` or later.

That is the whole init flow — there is no initialiser interface or registry, because there is
nothing to sequence. Only `MainMenuController` reads the manager at startup at all; the game
controllers touch it just once each, at completion, through `GameManager.Instance.Completion`. Every other
`Awake` in the project only caches its own components. A registry here would be a framework for a
single edge.

Game controllers start themselves from `Start` when their `autoStart` is on, and expose `Begin` for
anything that wants to drive them instead — the completion popup's Retry is the one caller that
does.

## Navigation

`SceneCatalog` (`Core/ScriptableObjects/SceneCatalog.asset`) is the single place a scene is named.
Each entry holds a `SceneAsset` picked in the inspector, baked to a plain string on validate —
`SceneAsset` is editor-only, so the string is what ships, and moving or renaming a scene updates the
entry instead of leaving a dead name behind.

Everything reads from it:

- `MainMenuController` turns a press on an authored row into a load.
- `SceneNavigationController.ReturnToMenu` resolves the menu from it.
- `SceneCatalogTests` fails if a catalog scene is missing from Build Settings, or if the menu is not
  at build index 0.

A transition is always the same five steps:

```
cover  →  DOTween.KillAll  →  LoadSceneAsync(Single)  →  UnloadUnusedAssets  →  reveal
```

`SceneFader` animates its own `CanvasGroup` on unscaled time rather than tweening. That is the point:
because the fader owns no tween, the navigator can sweep the whole engine on the way out without
having to spare its own animation. Unscaled time also means a scene that has paused itself, or one
still hitching on its first frame, cannot leave the player looking at a half-faded screen. Raycasts
are blocked for the entire transition, so a click aimed at the outgoing scene cannot land on the
incoming one.

## The menu

The rows are **authored in `MainMenu.unity`**, one `MenuEntryButton` prefab instance each, with their
title and tagline set on the labels. The menu is what it looks like in the editor, with no play mode
needed to see it.

Each row names the scene it opens and nothing else; `MainMenuController` is what turns a press into
`IGameNavigation.Load`. Nothing is instantiated at run time.

The cost is that the catalog no longer generates the menu, so a new task means a row in the scene as
well as a catalog entry. `MenuEntryTests` is what keeps the two honest: it fails if a row points at
a scene that is not in the catalog or not in Build Settings, and if a catalog game has no row.

## The completion popup

Two of the three games end with the same panel, and half of it — the way back to the menu — is
Core's business either way, so it lives on the persistent root instead of being authored into each
scene. Phoenix Flame simply never raises it.

The split is: **Core owns the panel and the Exit; the game owns what Retry means.**

```csharp
GameManager.Instance.Completion.Show("All 144 cards have landed.", Begin);  // Ace of Shadows — redeal
GameManager.Instance.Completion.Show("That's the last line.", Replay);      // Magic Words — replay
GameManager.Instance.Completion.Show(error, Begin);                         // Magic Words — refetch
```

Retry is a callback rather than a `UnityEvent` wired in the inspector, which is what makes the same
popup mean three different things without Core knowing any of them. Magic Words is the case that
justifies the shape: *finishing* and *failing to load* both raise the panel, and they need different
restarts. Pass `null` and the Retry button hides itself — the row is a layout group, so Menu
re-centres on its own.

`Completion` is the one lookup a game does, and it hands back an `ICompletionPopup` — the games
never name the concrete panel. It goes through `GameManager.Instance` because the popup outlives
every scene, so no scene can hold a serialized reference to it.

**The callback is dropped on every transition.** It closes over the controller that raised it, and
the popup outlives that controller — holding it would keep a destroyed object reachable and make
Retry run against a torn-down scene. `SceneNavigationController` calls `Dismiss()` on the covered
frame of each load, and `CompletionPopupTests` asserts a stale callback cannot fire after a scene
change.

## Leaving a scene: what actually stops

The question this architecture had to answer is whether tweens are correctly stopped when a scene is
left. They are, by two mechanisms, and the second is the one that guarantees it.

**The sweep.** `DOTween.KillAll()` runs after the screen is covered and before the load starts. At
that moment every live tween belongs to the scene being left, so killing them is unambiguous. It is
not the guarantee, because the outgoing scene keeps running for the frames the load takes and can
start more.

**The guarantee.** Every tween in the project is created with `SetLink(gameObject)`, and every
manually built `Sequence` also calls `SetTarget(transform)`. `SetLink` kills the tween when its
GameObject is destroyed, which is exactly what a scene unload does. The kill is a kill, not a
complete, so `OnComplete` callbacks do not fire against a torn-down scene — the one in
`DeckView.Retire` would otherwise hand a card back to a pool that no longer exists.

`SetTarget` matters separately: nested tweens belong to their sequence, not to the transform, so an
untargeted sequence is invisible to `DOKill(transform)` and outlives whatever it was animating —
including a card that has gone back to the pool.

Non-tween teardown falls out of Unity's own rules and needed nothing added: coroutines die with
their MonoBehaviour, and `MagicWordsController`'s async loads are bound to `destroyCancellationToken`
and catch the resulting `OperationCanceledException`. The one thing that is not Unity's to clean up
is the downloaded portrait textures, which `AvatarService` disposes in `OnDestroy`;
`Resources.UnloadUnusedAssets` after each load catches what is left.

None of this is asserted by inspection. `SceneTeardownTests` loads each game scene through the real
navigator, lets it run 2.5 seconds, navigates away, and asserts `DOTween.TotalPlayingTweens() == 0`.
A companion test asserts that Ace of Shadows *is* tweening at that same point, so the first test
cannot pass because the scene quietly stopped animating.

## UI input

Player Settings runs on the Input System package (`activeInputHandler: 1`). An EventSystem left on
the legacy `StandaloneInputModule` looks completely correct in the inspector while every button and
slider in the project silently stops responding, and nothing short of driving a pointer catches it.

There is now exactly one EventSystem, on the persistent root, carrying `InputSystemUIInputModule`.
`UiInputTests` asserts the active module's type, that it has an actions asset, and that it is
enabled; then raycasts the menu through `EventSystem.RaycastAll` to prove nothing invisible covers
it, clicks a menu entry and asserts a transition starts, and drags the Ace of Shadows speed slider to
its maximum. `SceneTeardownTests` separately asserts each scene has exactly one EventSystem.

## Editor tools

None in Core. The manager, the bootstrap, the menu entry button and the menu scene are ordinary
assets, edited in the inspector.

Two elsewhere, both writing an asset that would be tedious and error-prone by hand: Magic Words'
**Rebuild Emoji Sprite Asset**, which packs the emoji atlas the dialogue text draws from, and Phoenix
Flame's **Rebuild Colour Animator**, which builds the state graph from the palette asset so the two
cannot drift.
