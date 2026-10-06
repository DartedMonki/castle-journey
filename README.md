# Castle Journey

## Game Description
Welcome to our latest creation: a thrilling 2D platformer for Android, brought to life with the power of Unity. Follow the journey of a brave prince as he embarks on a perilous adventure through various palaces, encountering menacing monsters at every turn. Your mission, is to help the prince overcome these obstacles and emerge victorious. But be warned, defeating these fierce creatures won't be a walk in the park - you'll need to summon all your skills and strike multiple times to bring them down. One false move and the prince's fate is sealed. You have three chances to dodge the monsters' attacks, or it's game over. As you guide the prince on his quest, you'll also have the opportunity to collect coins and set new high scores. How far can you lead the prince on his journey of discovery? Only time will tell. Get ready to experience a world of wonder and excitement with our latest game.

## Deskripsi Game
Selamat datang di game terbaru kami: sebuah platformer 2D yang seru untuk perangkat Android, diciptakan dengan teknologi Unity yang canggih. Ikuti perjalanan seorang pangeran yang berani saat ia menjelajahi berbagai istana yang penuh bahaya, dengan monster-monster menyeramkan di setiap sudut. Misi Anda, adalah membantu sang pangeran mengatasi rintangan ini dan meraih kemenangan. Namun, waspadalah, mengalahkan para makhluk buas ini takkan mudah - Anda perlu mengeluarkan semua kemampuan Anda dan menyerang beberapa kali untuk mengalahkan mereka. Satu kesalahan dan nasib sang pangeran akan hancur. Anda punya tiga kesempatan untuk menghindari serangan monster, jika tidak, permainan akan berakhir. Selain membantu sang pangeran dalam perjalanannya, Anda juga berkesempatan untuk mengumpulkan koin dan mencetak skor tertinggi. Seberapa jauhkah Anda bisa membawa sang pangeran dalam perjalanan petualangannya? Hanya waktu yang bisa menjawab. Bersiaplah untuk merasakan dunia keajaiban dan kegembiraan dengan game terbaru kami.

## Development

Want to contribute? Great!

Castle Journey uses **Unity 6000.6.4f1**, **URP's 2D Renderer**, the **Input System**,
and **Cinemachine 6.6**. Existing levels and artwork are retained, with modernized
movement and consistent landscape HUD scaling.

Open your favorite Terminal and run this command.
```sh
$ git clone https://github.com/DartedMonki/castle-journey.git
```
Open Unity3D.

Open the game project folder.

Open Assets > Scenes > UI then load the MainMenu.

With the Unity CLI installed:

```sh
unity open . --editor-version 6000.6.4f1
```

Run regression tests with the project editor closed:

```sh
unity test . --mode EditMode --output Builds/Validation/editmode-results.xml
unity test . --mode PlayMode --output Builds/Validation/playmode-results.xml
```

The editor also provides **Castle Journey > Validate Project**. Save open scenes
and stop Play mode before using it.

See [the Unity 6 migration notes](docs/UNITY6_MIGRATION.md) for package pins,
build commands, Android setup, and remaining device/service verification.

## Controls

| Action | Keyboard | Gamepad | Touch |
| --- | --- | --- | --- |
| Move | A/D or left/right arrows | Left stick or D-pad | Left joystick |
| Jump / double jump | Space | South button | Jump button |
| Attack | J or left Ctrl | West button | Attack button |
| Pause / resume | Escape | Start | Pause / Resume buttons |

Menus support mouse, touch, keyboard navigation, and gamepad navigation.
Gameplay attacks do not use the left mouse button, so menu clicks cannot attack.

Movement is tuned on `PlayerController` in each world and the player prefabs.
`Run Speed` is in world units per second; `Jump Speed`, smoothing, coyote time,
and jump buffering can be adjusted independently.

See [the modernization notes](docs/UNITY6_MODERNIZATION.md) for implementation
details, verification, and a gradual playtesting checklist.
