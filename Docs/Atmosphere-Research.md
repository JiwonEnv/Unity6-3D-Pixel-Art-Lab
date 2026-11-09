# Developer research and stylized environment implementation

Research / implementation date: 2026-11-01. This update edits the Unity scene and source only. No player build was made.

## Sources and decisions

### Never’s End — Ryan Juckett, Hypersect, GDC 2026

[How We Draw a 3D Sprite World: The Stylized Art of Never’s End — original slides](https://media.gdcvault.com/gdc2026/Slides/Juckett_Ryan_HowWeDrawA3DSpriteWorldTheStylizedArtOfNeversEnd.pdf)

The presentation describes per-light toon shading, separate ambient color for shaded areas by time of day,
pixel-aligned camera/model/particle placement, quantized model rotation, and separate simulation/render timing for particles.
Relevant PDF pages: 35–43, 65–70, 92–94. It also discusses custom shadow geometry and object-level sprite ordering.
These are design references; this project does not contain the game's proprietary renderer or source code.

Applied here: day/night shade palette, camera and visual-root snapping, 16-direction facing,
source-pixel-sized rain/snow, and a shared 12Hz presentation clock for environmental movement.
The user's Shorts link and attached screenshots are visual references; technical attribution is to the original developer slides.

### Vegetation — AMD GPUOpen and Horizon Zero Dawn

[AMD GPUOpen — Procedural grass rendering, Wind animation](https://gpuopen.com/learn/mesh_shaders/mesh_shaders-procedural_grass_rendering/#wind-animation)

The article explains sine-based wind offset, cites Gilbert Sanders's Horizon Zero Dawn vegetation talk,
and discusses root-relative shading and blending grass normals toward up.
Our version uses height-weighted displacement with a fixed root and multiple spatial/time frequencies.
It is a simple vertex deformation in the existing Built-in pipeline, not AMD's mesh-shader grass system.
No code from the article is copied.

Additional original presentation reference: [Between Tech and Art: The Vegetation of Horizon Zero Dawn, GDC 2018](https://media.gdcvault.com/gdc2018/presentations/gilbert_sanders_between_tech_and.pdf).

### Related 2D-looking 3D art

[Arc System Works — official Guilty Gear Xrd GDC 2015 talk announcement](https://www.arcsystemworks.com/guilty-gear-xrds-art-style-the-x-factor-between-2d-and-3d-talk-from-gdc-2015-is-now-available-online/).
This is a related art-direction reference, also cited by Never’s End's presentation.
The current environment additions are based on the specific techniques described above, not a claim to reproduce Guilty Gear's renderer.

## What changed

| Area | Current implementation |
| --- | --- |
| Day / night | 8-minute full cycle, adjustable time, moving sun/moon direction, daylight intensity/color, separate shade tint, fog/background palette |
| Camp lighting | Existing smooth local-light falloff retained; camp lights become stronger at night |
| Rain | 320 camera-local source-pixel streaks, wind drift, 36 expanding ground splash shapes, wet surface darkening and small toon highlights |
| Snow | 220 two-pixel flakes, slow lateral drift, four-level upward-facing surface coverage; coverage accumulates and melts gradually |
| Clouds | Slow world-space stepped dark patches, stronger under precipitation; a stylized shadow approximation |
| Water | Tessellated elliptical lake, two small world-space waves, analytic water normals, moving short highlights, narrow shore foam and rain rings |
| Trees / plants | Root-anchored height-weighted sway; trunks/colliders remain stable while upper geometry bends |
| Flag | Original free FBX cloth subdivided once, pinned-side weighted deformation; generated mesh reused on subsequent upgrades |
| Character | 22.5° facing steps and visual-root screen-grid snapping; physics/controller positions stay continuous |
| Linework | Visible geometry, ID silhouettes and custom depth/normals all use the same deformation/time |

## Files and integration

- `WorldEnvironment.cs`: time, weather blending, lighting/fog state and shared shader globals.
- `WorldSurfaceMotion.cs`: renderer property blocks defining tree/grass/flag/water deformation.
- `WorldMotion.cginc` (R2 이후 `LabWind.hlsl`): shared vertex deformation, called by the existing Toon and identity/depth shaders.
- `SpriteToon.shader`: existing local light behavior plus time-of-day shade, wetness, snow and water appearance.
- `ObjectIdentity.shader`: existing replacement shader now also renders animated depth/normals.
- `PixelLinework.cs`: retains independent ID/depth targets so moving surfaces and ink agree.
- `PixelWeatherParticles.cs` / `PixelWeather.shader`: deterministic bounded precipitation mesh around the player.
- `PixelCharacterAlignment.cs`: moves only the rendered model to the screen grid; movement and collision are unchanged.
- `OpenWorldAtmosphereUpgrade.cs`: edits the existing free-roam scene and generates previews/assets without building a player.

The sky/background is a palette/fog transition, not volumetric clouds or a full sky-atmosphere model.
Rain falls onto collision surfaces; the lake surface is handled explicitly because it has no solid collider.
Snow is a rendering layer, not a change to terrain height, friction or navigation.
Water waves are visual and do not simulate fluid flow or affect the water-recovery gameplay threshold.
Vegetation shading normals are approximate during bending; water normals follow the wave slopes.

## Pixel stability and costs

Precipitation centers are snapped in camera-oriented screen coordinates.
Rain is one source pixel wide; snowflakes are two source pixels wide. The scene remains rendered at its existing low resolution.
Weather simulation is presented at 12Hz rather than adding continuous subpixel shimmer.
Character facing is discrete, while movement and collision retain the existing substepped controller.

Moving objects must match in their color, ID and depth silhouettes. The replacement shader shares vertex deformation with
the main shader and outputs either encoded IDs or encoded depth/normals. Surface Shader shadow passes use the same vertex function.
This adds geometry passes; broad platform performance profiling and large-world streaming have not been performed.

Separate authored shadow-caster/receiver meshes, material-ID linework, joint-translation pixel snapping,
and sprite-volume ordering of intersecting characters/environment remain future work.
Object IDs currently control ink, not full object draw order; the sword/terrain ordering examples are not claimed as solved.

## Verification

- [Environment Play-mode verification](../Previews/Atmosphere-Verification.txt): readable night/day contrast,
  night torch strength, weather activation, two-pixel snow width, visible wind and water motion,
  wind-off stability, moving flag ID silhouette, shader/runtime errors.
- [Repeatable authoring](../Previews/Atmosphere-Authoring-Verification.txt): flag vertex count remains fixed on re-application.
- [Gameplay regression](../Previews/Open-World-Playtest.txt): movement, jumping at low frame rate, collisions, discovery and collecting.

Previews: [Day](../Previews/16-Weather-Day.png), [Night](../Previews/17-Weather-Night.png),
[Sunset](../Previews/24-Weather-Sunset.png),
[Rain](../Previews/18-Weather-Rain.png), [Snow](../Previews/19-Weather-Snow.png),
[Water frame A](../Previews/20-Lake-Motion-A.png), [Water frame B](../Previews/21-Lake-Motion-B.png).
