# Phoenix Flame art

| File | Origin |
|---|---|
| `flame_sheet.png` | VFXPACK_FIRE_WALLCOEUR, third party. 3×3 flipbook, drawn by `Flame_Additive.mat`. |
| `smoke_sheet.png` | VFXPACK_FIRE_WALLCOEUR, third party. 3×3 flipbook, drawn by `Smoke_Alpha.mat`. |
| `ember_dot.png`, `flame_glow.png`, `backdrop.png` | Authored for this project. White, with the shape in the alpha channel. |

Only the two sheets and their two materials were kept from the pack; the rest of it — a demo scene,
fourteen demo prefabs and the unused smoke set — is not in the project. Both materials use built-in
`Mobile/Particles/*` shaders and tint by particle colour, which is what lets `FlamePaletteSet`
recolour art it did not author.

Those three are white with the shape in the alpha channel, so one texture serves every colour in
the cycle.
