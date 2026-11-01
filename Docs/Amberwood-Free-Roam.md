# Amberwood — playable free-roam prototype

## Start

Open `Assets/Scenes/Amberwood-Free-Roam.unity` in Unity **6000.3.9f1** and press Play.
This is now the first scene in Build Settings. The earlier rendering study scenes are preserved.

The existing local Windows build is `Builds/Amberwood/Amberwood.exe` and predates the atmosphere update.
Per the user's request, it was not rebuilt. Use Unity Play mode for the new time/weather/wind/water features.
Keep the executable, UnityPlayer.dll, MonoBleedingEdge and Amberwood_Data together when moving the build.
Build binaries are excluded from Git; source models, license notices, scripts and scenes are included.

## Controls

| Input | Action |
| --- | --- |
| WASD / Arrow keys | Move relative to the camera |
| Left Shift | Sprint |
| Space | Jump |
| Q / E | Rotate the camera |
| Mouse wheel | Zoom |
| R | Return to camp |
| Escape | Pause / resume |
| H | Show / hide controls |
| T / O / Tab | Compare Toon lighting / linework / pixel rendering |
| F1 / F2 / F3 / F4 | Morning / noon / sunset / midnight |
| 1 / 2 / 3 | Clear / rain / snow (smooth transition) |
| L | Run / stop the day clock |
| B / V | Wind / water movement |
| U | Discrete / smooth character facing |

## Explore

The approximately **150 × 150 metre** connected map includes Amberwood Camp, Oldwatch Ruins,
Stillwater Lake, Whispering Pines and Sunrise Lookout.
Walk the paths or go through the forest. The minimap marks the player and discovered regions.
Find three gold waystones at the ruins, pines and lookout; pickup is automatic at close range.
Deep water and falling out of the map return the player to camp.

This is a finite exploration prototype, not an infinite or streamed world.
Combat, swimming, inventory, dialogue and save/load are not implemented.

## Player implementation

- CharacterController with terrain and prop collisions, slope limit, step offset and a ground sphere probe.
- Movement/gravity are subdivided into at most 1/60s steps to keep jumping stable in slow frames.
- Walk 4.2m/s, sprint 7.2m/s, jump target height 1.65m, gravity −26m/s².
- Short jump buffering and coyote time; no airborne double jump.
- Animator blend tree for Idle / Walking_A / Running_A.
- Jump_Start and Jump_Idle states follow takeoff, airborne movement and landing.
- Root motion is disabled; the controller owns movement.
- Smooth-follow orthographic camera with screen-space pixel snapping and integer upscale.

## Rendering and assets

Time of day, precipitation, wet/snow surfaces, water waves and vegetation/flag wind are now integrated.
The full day is 480 real seconds by default; `WorldEnvironment` exposes time, cycle duration and wind strength.
See [technical research and implementation](Atmosphere-Research.md).

Existing Toon shader, object-ID linework and torch falloff are reused.
The playable player is the free KayKit Knight with its original rig and animations.
The environment props use Kenney Nature/Castle/Platformer packs.
Ground and connected trail meshes are generated to provide continuous collision surfaces.
See [asset sources and licenses](Third-Party-Assets.md).

## Rebuild and verify

- `Pixel Art Lab > Build Playable Amberwood Free Roam` regenerates the playable scene and previews.
  Save hand-edited scene variants separately before using this command.
- `Pixel Art Lab > Apply Day Night and Weather to Free Roam` adds/updates atmosphere without regenerating the map.
- `Pixel Art Lab > Build Windows Free Roam Player` produces the local Windows build.
- Editor verification entry point: `PixelArtLab.Editor.OpenWorldVerification.Run`.
- Atmosphere verification: `PixelArtLab.Editor.AtmosphereVerification.Run` (Unity Play mode only, no player build).
- Standalone verification: run the executable with `--amberwood-smoke -batchmode -logFile <path>`.
  The test exits with 0 on success and 1 on failure. Normal launches do not run the test.
- [Gameplay verification](../Previews/Open-World-Playtest.txt) covers walking, sprint speed, obstacle blocking,
  jump height, jumping over obstacles, landing, no double jump, water recovery, region discovery,
  collecting all waystones, camera centering and the five movement animation clips.
- [Map preview](../Previews/14-Amberwood-Map.png).

Large-world streaming and platform performance profiling have not been performed.
