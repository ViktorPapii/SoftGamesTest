# Ace of Shadows

144 cards stacked as a deck, moving one at a time to a second deck, with a counter over each and
Core's shared completion popup when the last one lands.

Scene: `Assets/_Project/AceOfShadows/Scenes/AceOfShadows.unity`. Entry point:
`AceOfShadowsController.Begin()`, which tears any running deal down and redeals from scratch — so it
is also what the shared completion popup gets handed as its Retry.

## 144 cards, a handful of objects

A deck is a **count**, not a pile of GameObjects. `DeckModel` holds the card ids; `DeckView` shows
the top `visibleDepth` of them as real cards and nothing else. At 144 cards the per-card offset
would fall below a pixel anyway, so the rest of the deck has no reason to exist as objects.

`CardPool` recycles the few that do. The worst case in flight is `(visible slots x 2 decks) + cards
in the air`, which is why the pool prewarms 10 and warns rather than silently growing if that turns
out to be wrong.

| Piece | Role |
|---|---|
| `DeckModel` | The count and the card ids. Raises `CountChanged`. Engine-free assembly. |
| `DeckView` | Slots, draw order, restacking. `Relayout` is the single pass for all of it. |
| `CardPool` | `ObjectPool<CardView>` with a prewarm and a growth warning. |
| `CardFlightAnimator` | One card, one arc, one sequence. |
| `AceOfShadowsController` | The clock. Owns the transfer loop and the end state. |
| `DeckCounterView` | Binds a `DeckModel` to a label, punches it on change. |
| `ScreenLayoutWatcher` | Keeps camera size and deck positions in step with the canvas. |
| `CardDeckAsset` | Face sprites, filled by scanning `Art/Images`. |

## The transfer

State changes the instant a transfer *starts*, never read back from the scene — a card in flight
belongs to neither deck, so several can be airborne at once when the speed is raised:

```
DetachTop  →  model.TakeTop  →  target.NotifyIncoming  →  fly  →  target.ReceiveCard
```

`NotifyIncoming` reserves the landing slot immediately, so the target restacks *during* the flight
rather than after it, and the card lands on a stack that has already made room. Reserved slots are
capped at `visibleDepth - 1` so the deck always keeps one visible card — the one covering the card
being retired.

Later launches get a higher sorting order than earlier ones, and both sit above either deck, so
overlapping cards in the air never fight.

The loop keeps the overshoot when it subtracts the interval, so transfer intervals do not quantise
to the frame rate. Speed is read every frame, which is what lets the slider change it mid-run.

## Retiring a card

When a card falls out of the visible depth it is not destroyed on the spot — it slides to the
covered slot while its replacement lands on top of it, and only then goes back to the pool. Both
tweens start in the same pass with the same duration, so the card is covered at exactly the moment
it is released. Several overlap once transfers come faster than `restackDuration`, which is why
retiring cards get their own descending sorting orders below every resident.

## Tweens and teardown

Every tween is created with `SetLink(card.gameObject)`, and the two hand-built sequences
(`CardFlightAnimator.Fly`, `DeckView.Retire`) also call `SetTarget(cardTransform)`. Both matter:

- Without `SetTarget`, nested tweens belong to the sequence rather than the transform, so
  `DOKill(transform)` cannot see them and the sequence outlives the card it was animating —
  including after that card has gone back to the pool.
- Without `SetLink`, leaving the scene mid-flight leaves a sequence whose `OnComplete` hands a card
  to a pool that no longer exists.

`Relayout` skips killing tweens on a card whose flight callback is still running, and leaves a
running settle alone when the destination has not changed — restarting it resets its clock, and
below `restackDuration` it would never arrive.

Covered by `SceneTeardownTests` in `Assets/_Project/Core/Tests/PlayMode/`, which leaves this scene
mid-transfer and asserts nothing is still tweening.

## Tests

`DeckModel` lives in `Scripts/Core/`, an assembly with `noEngineReferences: true` — the transfer
rules cannot reach UnityEngine, so `Tests/EditMode/DeckModelTests` runs them with no scene and no
play mode. Same split as `MagicWords/Scripts/Core`.

The rules worth pinning: a deck is a stack, `PeekFromTop(visibleDepth - 1)` is in range rather than
one past the end, `CountChanged` carries the new count and stays quiet on reads, and a card taken
from one deck belongs to neither until it is put on the other.

## Layout

`ScreenLayoutWatcher` matches the camera's orthographic size to the CanvasScaler's scale factor, so
a card and a UI element of the same on-screen size at the reference resolution stay that way at any
resolution. Decks anchor to their counters through the canvas rect rather than through screen
pixels — a letterboxed viewport makes screen space and the camera's pixel rect disagree, which put
the decks in the wrong place on every aspect but the authored one.

It polls, because Unity has no resolution-changed event and the UI layout message fires mid-rebuild
while anchored elements are still a frame behind. Two int comparisons per frame for the whole scene.
The canvas rect is watched alongside the camera because it resizes a frame later; watching only the
camera fires once against a stale rect and then never again.

## Art

`Art/Images` holds the face sprites, packed by `Art/AceOfShadows_Atlas`; `CardDeckAsset` scans that folder from its
**Load Sprites From Folder** context menu and sorts the result so the asset diff is stable. 144 cards
draw from ~53 sprites, so faces repeat — nothing here compares or matches cards, so there is no
suit/rank lookup.
