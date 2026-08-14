# Emoji atlas

`EmojiAtlas.png` is a 3x2 grid of 64px glyphs taken from [Noto Emoji](https://github.com/googlefonts/noto-emoji)
(Copyright 2013 Google LLC, SIL Open Font License 1.1).

Only the glyphs the dialogue actually needs are shipped, so the WebGL build carries 192x128 pixels
of emoji instead of a full set. `EmojiAtlas.asset` is the TextMeshPro sprite asset built from it;
each sprite carries the Unicode codepoint of its glyph, so a plain Unicode emoji in a string
resolves to the coloured sprite. Regenerate both with **Tools > SoftGames > Rebuild Emoji Sprite
Asset** after changing the grid.

| Cell | Codepoint | Glyph | Token |
|---|---|---|---|
| 0 | U+1F60C | 😌 | satisfied |
| 1 | U+1F928 | 🤨 | intrigued |
| 2 | U+1F610 | 😐 | neutral |
| 3 | U+1F44D | 👍 | affirmative |
| 4 | U+1F602 | 😂 | laughing |
| 5 | U+1F3C6 | 🏆 | win |

A token with no entry in `EmojiCatalog` renders as nothing at all — no stand-in glyph, since that
would put a mood in the line the payload never sent. It is reported once in the load warning.
