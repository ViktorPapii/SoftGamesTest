# SoftGames Test

Three small Unity tasks behind one menu — a card transfer, a fetched dialogue, and a fire effect —
built as one project rather than three, so the shared parts (navigation, HUD, scene teardown) are
solved once.

Unity **6000.5.4f1**, 2D URP, DOTween. Targets WebGL.

## The tasks

| | What it is | Where |
|---|---|---|
| **Ace of Shadows** | 144 cards moving one at a time between two decks, drawn with a handful of pooled objects | [README](Assets/_Project/AceOfShadows/README.md) |
| **Magic Words** | A dialogue fetched at run time, with emoji and portraits resolved from the payload | [README](Assets/_Project/MagicWords/README.md) |
| **Phoenix Flame** | A particle fire whose colour cycles on a button, driven by an animator and a palette asset | [README](Assets/_Project/PhoenixFlame/README.md) |

Everything they share — the menu, scene loading behind a fader, the persistent HUD and the
end-of-run popup — lives in **Core**: [architecture README](Assets/_Project/Core/README.md).

## Running it

Open the project in Unity 6000.5.4f1 and play `Assets/_Project/Core/Scenes/MainMenu.unity`.

Any game scene also runs on its own — each carries a `GameBootstrap` that builds the persistent
manager if none is alive, so there is no "you must start from the menu" rule.

Magic Words fetches its script over the network on entry; without a connection it shows the failure
state and offers Retry rather than hanging.

## Building

A Web build profile ships with the project: **File ▸ Build Profiles ▸ Web - Desktop - Release**.
DXT texture compression, LTO, Brotli with decompression fallback so it loads off any static host.

## Layout

```
Assets/_Project/{Core, AceOfShadows, MagicWords, PhoenixFlame}
```

Each task owns its folder and its own assembly definitions — 14 in all. No game assembly references
another and Core references none of them, so the boundaries are a build error rather than a
convention. Pure logic sits in `Scripts/Core/` assemblies that cannot compile against UnityEngine,
which is what lets the card and dialogue rules be tested with no scene.

## Tests

**52 edit mode, 17 play mode.** Window ▸ General ▸ Test Runner, or:

```bash
Unity -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml
```

Edit mode covers the transfer rules, the dialogue parser and emoji composition, and the catalog and
menu staying in step with Build Settings. Play mode drives the real scenes: it leaves each one
mid-animation and asserts nothing is still tweening, and presses Phoenix Flame's button to check the
fire actually changes colour.
