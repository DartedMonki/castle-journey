# Unity 6 Migration

## Editor

- Original version: Unity 2018.4.36f1, Git revision `314eff5`.
- Target: Unity 6000.6.4f1 (`12bfff696524`).
- Keep the Built-in Render Pipeline and Input Manager. Do not switch to URP or
  the new Input System as part of this migration.
- Enabled build scene indices remain MainMenu (0), World1 (1), World2 (2),
  World3 (3), and EndGame (4).
- Open `Assets/Scenes/UI/MainMenu.unity` to play.
- This is a one-way serialized-asset upgrade. Use a separate checkout of the
  original Git revision for Unity 2018; do not reopen these upgraded assets there.

## Packages

| Package | Version | Purpose |
| --- | --- | --- |
| Cinemachine | 2.10.7 | Preserve existing cameras and their serialized references |
| uGUI | 2.6.0 | Legacy Canvas UI and bundled TextMesh Pro |
| 2D Sprite / Tilemap Editor | 1.0.0 | Sprite slicing and tile editing |
| Test Framework | 1.8.0 | Edit Mode and Play Mode regression tests |
| External Dependency Manager | 2.1.0 | Replace the incompatible 1.2.95 Android/iOS resolver |
| Unity Pipeline | 0.8.0-exp.1 | Live Unity CLI integration |

Unity 6000.6 bundles a newer Cinemachine and can resolve it instead of a numeric
2.x dependency. The official 2.10.7 archive is therefore checked in as
`Packages/com.unity.cinemachine-2.10.7.tgz`, referenced with a relative `file:`
dependency. Keep that archive and `Packages/packages-lock.json` in source control.
The validator rejects an unexpected Cinemachine version.

Archive SHA-256:
`f409347e3c19d2cb7a783664fec2df262c09ce0596b13c04a79aebbcbcf894c1`.

Unused legacy Ads, Analytics, IAP, Collaboration, Package Manager UI, standalone
TextMesh Pro, and VR package entries were removed. Their APIs are not called by
the game's scripts. The Facebook SDK remains in place; its old dependency
resolver is retained, inactive, under `Legacy/PlayServicesResolver`.

## Repairs

- Unity upgraded asset/importer serialization while retaining asset GUIDs and
  sprite references.
- Rigidbody2D velocity and falling-platform body type use the supported APIs.
- Sorted object lookups retain legacy first-match behavior. Unity 6000.6 warns
  that these APIs are deprecated, but changing to arbitrary-match lookups could
  change which enemy the original combat code selects.
- The fixed joystick uses its Canvas camera, or null for overlay UI, instead of
  constructing an invalid Camera component.
- The menu loader tolerates the intentionally unassigned background/particle
  effects. Previously the Start button logged an exception before activating
  the loading UI.
- One pre-existing missing script, GUID
  `465934e62273057419e3793d809e27a5`, was removed from the attack trigger in
  World3 and the two player prefabs. It has no source in the original repo.
  Colliders and the existing combat scripts were retained.
- Audio and 3D physics settings use Unity 6's supported serialization versions.
  Original gravity, 2D physics configuration, layer names, and input axes remain.
- Migration and validation tools refuse to run with unsaved scenes or in Play
  mode, rather than discarding editor work.

## Verification

Verified on October 6, 2026 with the installed ARM64 Unity 6000.6.4f1 editor:

- Clean import and compilation succeeded.
- All 12 Edit Mode tests and all 5 Play Mode tests passed.
- An ARM64 macOS player build succeeded.
- All 3,876 original asset GUIDs were preserved, including the archived resolver.
- The isolated validation copy matched the repository's source files.
- The actual project editor was ready and connected to the Unity CLI.

Reports are in `Builds/Validation/`; the compiled player is in
`Builds/macOS/Castle Journey.app`. Android Build Support was not installed on
this machine, so an Android build was not claimed as verified.

Close the project editor before batch commands. These commands require a valid
local Unity license and package-registry access on the first import:

```sh
unity run . --timeout 600 -- -executeMethod CastleJourney.Editor.Unity6Migration.Validate
unity test . --mode EditMode --output Builds/Validation/editmode-results.xml
unity test . --mode PlayMode --output Builds/Validation/playmode-results.xml
unity run . --timeout 900 -- -executeMethod CastleJourney.Editor.Unity6Migration.BuildMac
```

The Mac build is written to `Builds/macOS/Castle Journey.app`. Test/build outputs
and the migration's isolated validation project are ignored by Git.

Edit Mode coverage includes all five build scenes, missing scripts, serialized
references, button callback methods, game prefab scripts, camera targets, tile
sprites, scene order, legacy input, physics timing, and the Cinemachine pin.
Play Mode coverage includes the start button's asynchronous scene load, all
three worlds, joystick movement, jump, attack timing, score, health, pause/resume,
death UI, and carried end-game score.

## Android

Unity 6000.6 requires Android 8.0 / API 26 or newer; the original API 16 minimum
cannot be preserved. ARMv7 and ARM64 are configured with IL2CPP. The existing
UnityPlayerActivity entry point is retained, with an exported launcher and
configuration-change handling in the manifest.

If Android Build Support is not installed, inspect and then install its SDK,
NDK, and OpenJDK dependencies:

```sh
unity install-modules --editor-version 6000.6.4f1 --architecture arm64 --module android --child-modules --dry-run
unity install-modules --editor-version 6000.6.4f1 --architecture arm64 --module android --child-modules
unity run . --timeout 1200 -- -executeMethod CastleJourney.Editor.Unity6Migration.BuildAndroid
```

Use the appropriate architecture flag on Intel machines. The APK is written to
`Builds/Android/CastleJourney.apk`. Install modules with the editor closed, then
restart it before switching platform.

The original keystore path points to an unavailable Windows drive. Local builds
use development signing. Before publishing an update, restore the original
release keystore and alias in Player Settings; do not replace the signing key of
an existing published app.

## Remaining Manual Checks

An editor migration and automated smoke tests cannot guarantee identical
gameplay or certify a production mobile release.

- Build and test on a physical Android device, including touch controls,
  different aspect ratios, audio, suspend/resume, and level completion.
- Validate the old Facebook 7.15.1 integration against the configured app and
  current service requirements. Login, sharing, credentials, and server-side
  permissions are not covered by offline smoke tests.
- Restore and verify release signing, app icons, target API, and store settings.
- Review the unused legacy player prefabs before placing new instances; some
  scene-specific UI references were already empty in the original repository.
- The unused `Assets/UI/PrefabGamesebelumnya/Canvas.prefab` already contains a
  missing script. It is not a dependency of the shipped game and was retained
  without removing its components.
