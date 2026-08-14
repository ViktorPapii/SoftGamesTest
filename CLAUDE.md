# SoftGamesTest

Unity 6 (6000.5) 2D URP project. Gameplay code lives in `Assets/Scripts/`, namespaced per task
(e.g. `SoftGames.AceOfShadows`). DOTween is the animation library.

## Naming

### Private fields

**Non-serialized private fields use a leading underscore: `_camelCase`.**

```csharp
private CardPool _pool;
private readonly List<CardView> _residents = new();
private int _pendingIncoming;
```

**Serialized private fields do NOT: plain `camelCase`, no underscore.**

```csharp
[SerializeField] private int visibleDepth = 2;
[SerializeField] private CardView cardPrefab;
```

This split is not cosmetic. Unity keys serialized data on the **field name**, so renaming a
serialized field silently drops whatever was assigned to it in every scene, prefab and
ScriptableObject that referenced it — the inspector slot just shows up empty, with no compile error
and no warning. The underscore therefore doubles as a marker: a field without one is part of the
asset contract and cannot be renamed freely.

If a serialized field genuinely must be renamed, carry the old name so existing assets still
deserialize:

```csharp
[FormerlySerializedAs("oldName")]   // UnityEngine.Serialization
[SerializeField] private int newName;
```

### Everything else

No underscores. Locals and parameters are `camelCase`; methods, properties (including private ones)
constants and types are `PascalCase`.

```csharp
private int CoveredSlot => visibleDepth - 1;      // private property — still PascalCase
public const int NoSlot = -1;
private void Retire(int index, bool animated) { }
```

## Keep the public surface minimal

**Public fields, properties, methods and events with no caller are removed, not kept "just in case".**
If something is only used inside its own class, make it `private`. Unused public API is read as
intent — it implies a contract someone relies on — and it is the cheapest thing in a review to hold
against you.

Applies equally to write-only state: a property that is assigned but never read is dead weight, and
so is an event that is raised but never subscribed to.

**Before deleting, check more than the C# — a grep of `Assets/Scripts/` is not sufficient.** Unity
calls into code from places the compiler cannot see, and a member with zero code references may
still be live:

| Referenced from | How to check |
|---|---|
| `UnityEvent` hooked up in the inspector (buttons, sliders) | `grep m_MethodName Assets/Scenes/*.unity` |
| `[ContextMenu]`, `[MenuItem]`, `[RuntimeInitializeOnLoadMethod]` | attribute on the member |
| `Invoke` / `InvokeRepeating` / `SendMessage` / animation events | string literals naming the method |
| Unity messages (`Awake`, `Start`, `OnDestroy`, `Reset`, `LateUpdate`…) | never remove; the engine calls them |

`AceOfShadowsController.Begin` is the live example: it has no C# caller at all — the retry button
invokes it through a serialized `UnityEvent` (`m_MethodName: Begin` in `AceOfShadows.unity`).
Deleting it on grep evidence alone would have compiled cleanly and broken the button silently.

Note that `[ContextMenu]` works on private methods, so a method does not need to be public just to
appear in the inspector's context menu.

## Comments

Say what the code does, in as few words as possible — or say nothing. A comment that restates the
line below it is noise.

- **Delete the obvious.** `SetFace` sets the face; `Bind` binds. No comment.
- **One line by default.** Summaries above ~3 lines belong in a design doc, not a header.
- **A one-liner is `//`, never `/// <summary>`.** XML doc is for multi-line explanations only.
  Don't wrap a single sentence in tags, and don't use `<see cref="..."/>` or `<paramref name="..."/>`
  in a `//` comment — write the plain name.

  ```csharp
  // Removes and returns the top card; the caller reparents it for flight.
  public CardView DetachTop()

  /// <summary>
  /// Assigns every resident a slot, retires what no longer fits, and lets the card taking the
  /// covered slot pool the retired one once it settles on top of it.
  /// </summary>
  private void Relayout(bool animated, CardView skipKillFor = null)
  ```
- **No justification prose.** Drop "rather than…", "deliberately…", "the usual way this gets slow…",
  and any comparison to an approach that is not in the code. Describe what is there now.
- **No history.** Comments explain the current code, not what it used to be or which bug prompted it.
- **Keep only what stops a bug.** A non-obvious constraint that would be broken by an innocent edit
  earns its line — e.g. "Sequences are killable only by target; untargeted, this outlives the pooled
  card." Everything else goes.

`[Tooltip]` is not a comment — it is inspector UI, and should stay useful for whoever tunes the value.

## Conventions

- Prefer `[SerializeField] private` over `public` for inspector-facing state; expose reads through
  properties where callers need them.
- `MonoBehaviour`s cannot use constructors — initialise via an explicit `Init`/`Setup` method called
  after `Instantiate`.
- Any manually built DOTween `Sequence` must call `SetTarget(transform)`. Without it the sequence is
  invisible to `DOKill(transform)` (nested tweens belong to the sequence, not the transform), so it
  outlives whatever it was animating — including a card that has gone back to the pool.
