# Unity 6 Modernization

## Scope

The Unity 2018 project was first upgraded to a working Unity 6000.6.4f1
compatibility baseline. This second pass modernizes the systems rather than
requiring identical legacy behavior. Scene order, level content, artwork, asset
GUIDs, score carry-over, and existing button callbacks remain important contracts.

## Stack

| System | Version | Configuration |
| --- | --- | --- |
| Unity | 6000.6.4f1 | ARM64 editor on macOS |
| Cinemachine | 6.6.0 | `CinemachineCamera` and `CinemachinePositionComposer` |
| Input System | 1.20.0 | New backend only; no legacy/Both backend requirement |
| Universal RP | 17.6.0 | 2D Renderer; unlit sprite material; HDR/post-processing off |
| uGUI | 2.6.0 | Existing Canvas UI, with `InputSystemUIInputModule` |

The editor's supported package versions replace the Cinemachine 2 archive
dependency. The AI packages, Unity CLI integration, External Dependency Manager,
and Test Framework remain installed. Facebook SDK 7.15.1 remains unchanged apart
from example-script input reads; its online service compatibility is not certified.

## Assets

- `Assets/Settings/CastleJourneyURP.asset` is assigned in Graphics Settings and
  every quality level.
- `Assets/Settings/CastleJourney2DRenderer.asset` uses unlit sprites by default.
- `Assets/Settings/SpriteUnlit.mat` replaces Built-in sprite materials in the
  build scenes and game prefabs. The original artwork is retained.
- `Assets/Settings/CastleJourney.inputactions` contains editable Gameplay and UI
  maps. Players clone their Gameplay actions so scene reloads do not leave shared
  gameplay actions enabled.
- All three world cameras were converted using Unity's Cinemachine upgrader.
  Tracking targets and lens framing are retained; tracking damping is reduced
  to 0.35 horizontally and 0.5 vertically for more responsive movement.

Do not edit scene YAML to perform another camera or renderer conversion.
`Assets/Editor/ModernizeProject.cs` performs the conversion through Unity APIs.
Save open scenes and stop Play mode before using its menu command. Existing input
bindings are retained on reruns, but movement/HUD defaults are intentionally
reapplied: this is a migration tool, not a routine setup step after tuning.

## Movement And Combat

- Horizontal speed is 6.5 world units per second at full input, rather than the
  old effective 4 units per second at the default 50 Hz physics rate.
- Velocity is no longer multiplied by the physics timestep. Smoothing uses an
  explicit fixed timestep, and Rigidbody2D interpolation smooths rendering.
- Keyboard A/D and arrows, gamepad stick/D-pad, and the existing touch joystick
  share the controller. Joystick input is analog with a 0.15 dead zone.
- Jump speed is 12 units per second, with a double jump, 0.1-second coyote time,
  and 0.12-second input buffer. There is no third mid-air jump.
- Grounding probes the non-trigger body collider downward by 0.08 units rather
  than relying on the old, inconsistently positioned ground-check markers.
- Moving platforms use kinematic Rigidbody2D movement in FixedUpdate. Their
  velocity carries the player without parenting the player's Rigidbody2D to the
  platform; walking and jumping remain relative to the supporting surface.
- Attack duration is 0.2 seconds. Each contacted enemy can be damaged once per
  attack, instead of damaging the first enemy in the scene.
- Knockback yields properly and temporarily retains control of horizontal
  velocity; the original non-yielding force loop is gone.
- Legacy `PlayerMovement` and `PlayerAttack` scripts remain as callback adapters,
  not competing update loops.
- Pause actions remain usable when time is stopped, but cannot resume a dead
  player. The pause menu selects Resume for keyboard/gamepad navigation.
- Single-scene loads restore normal time, including returning from death to a
  menu. Spawning a player no longer changes the global time scale.

Tune the Movement fields on the world players and both player prefabs. Existing
level reachability and combat balance need human playtesting after these changes.

## UI

All screen-space canvases use a 1280x720 reference, Scale With Screen Size, and a
0.5 width/height match. This includes menus, loading, gameplay, pause, death, and
the end-game screen.

Each canvas has a SafeArea content root that updates for screen size, rotation,
and device safe-area changes. Health, score, pause, joystick, jump, and attack
use consistent anchors and dimensions across worlds. The fixed joystick computes
its input in local UI coordinates, tracks its owning pointer, and resets on
disable or focus loss, so canvas scaling and another finger do not leave it stuck.
The boss health bar uses the top HUD row, and the pause button uses a pause
symbol rather than the old close graphic. End-game content no longer overlaps
the title artwork.

Landscape-left and landscape-right rotation are enabled. Portrait is disabled.
The existing visual identity remains; this is not a replacement-artwork redesign.

## Verification

Verified on October 6, 2026 with Unity 6000.6.4f1:

- All 19 Edit Mode tests passed.
- All 19 Play Mode tests passed.
- The ARM64 macOS player build succeeded. The validation artifact is
  `Builds/Modernized/macOS/Castle Journey.app`; the earlier baseline build remains
  in `Builds/macOS`.
- All 3,910 asset GUIDs from the preceding compatibility commit were retained.
- All 7,577 asset/source/metadata files matched the isolated verification copy
  before the build; temporary test-runner scenes were excluded.
- Fifteen scene screenshots passed pixel and clipping checks at the three
  aspect ratios below. End-game buttons and score text also passed overlap checks.
- The moving-platform regression failed before the physics fix and passed
  afterward. The tests exercise idle riding, walking left/right, two jumps
  from a real platform, and landing/jump resets in all three worlds.

The editor was left open for the user's World1 play session. Final test/build
commands used the isolated copy rather than closing that editor.
The build refreshed URP's generated shader-prefilter, runtime-settings, and
neutral default-volume caches in that copy. These are build-generated state,
not changes to the tested gameplay, camera, input, or HUD configuration.

Run with the project editor closed:

```sh
unity run . --timeout 600 -- -executeMethod CastleJourney.Editor.Unity6Migration.Validate
unity test . --mode EditMode --output Builds/Validation/modern-editmode-results.xml
unity test . --mode PlayMode --output Builds/Validation/modern-playmode-results.xml
unity run . --timeout 1200 -- -executeMethod CastleJourney.Editor.Unity6Migration.BuildMac
```

The suites cover scene integrity, modern camera targets, supported packages, URP
quality assignments, action bindings, UI action references, canvas configuration,
all worlds, score/health/death, touch joystick ownership/reset, keyboard/gamepad
movement, pause/resume, double-jump limits, physics-rate-independent speed,
contact-specific attack damage, moving-platform support, and real-level landing
resets.

Rendering tests capture all five scenes at 1280x720, 2400x1080, and 1024x768,
check for blank output/missing-shader pixels, and check gameplay buttons for
screen-edge clipping. Screenshots are written to `Builds/Validation/Screenshots`.
Offscreen screenshots include UI by temporarily rendering canvases through the
camera; production canvases remain screen-space overlays.

Builds, logs, screenshots, reports, and isolated validation copies are ignored.
Android builds still require Android Build Support, SDK/NDK/OpenJDK, and physical
device testing. The previous migration notes include installation/build commands
and release-signing requirements.

## Gradual Playtesting

1. Start MainMenu. Check Start, Settings, About, exit confirmation, and Back using
   mouse/keyboard, then touch/gamepad where available.
2. Play World1 slowly. Try short joystick movement, full-speed runs, stopping,
   first jump, double jump, ledges, coins, and multiple enemies.
3. Pause while moving. Resume, retry after death, and return to the menu. Check
   that movement does not stay held after the joystick is hidden.
4. Repeat World2, especially moving/falling platforms, then World3 and the boss.
   Check score carry-over and the end-game screen.
5. Try a wide phone and a tablet. Check HUD spacing, touch targets, simultaneous
   joystick/jump/attack, cutouts, orientation changes, and audio.
6. On Android, check suspend/resume, controller reconnect, performance, Facebook
   integration, and release signing before publishing.

Automated smoke tests are not a claim of identical gameplay or production release
readiness. The unused archival UI prefab with a pre-existing missing script is
still retained and is not a dependency of the shipped build scenes.
