# Vertigo Wheel Demo

A Unity wheel-of-fortune game developed for the Vertigo Games internship case.

**Unity version:** 6000.3.23f1 LTS

**Target platform:** Android, ARM64, landscape

**Status:** Working prototype. An initial APK has been tested in an Android emulator; final polish and submission materials are still in progress.

## Implemented

- [x] Eight-slot wheel with DOTween spin animation and input locking during a spin.
- [x] Editor-configurable rewards, slot amounts, wheel sprites, and indicators using ScriptableObjects.
- [x] Zone progression with bronze wheels in normal zones.
- [x] Bomb-free silver wheels at every fifth zone.
- [x] Bomb-free golden wheels with special item rewards at every thirtieth zone, taking priority over silver wheels.
- [x] Gold, cash, and item accumulation during a session.
- [x] Increasing gold and cash rewards as the player progresses.
- [x] Bomb results that clear the session rewards and end the run.
- [x] Cash-out flow at eligible zones, a separate success message, and session restart.
- [x] Per-slot reward amounts, shared calculation for preview and payout, and hidden amount labels for bombs.
- [x] Upright reward icons and amount labels while the wheel rotates in the latest source.
- [x] TextMeshPro UI, sliced button sprites, and a Sprite Atlas.
- [x] Reusable `game_wheel` prefab containing the UI and session controller.
- [x] Canvas scaling with a 1920 x 1080 reference resolution and Expand mode.
- [x] Android build configuration and an initial APK smoke test in an ARM64 emulator.

## Gameplay

- **ÇEVİR / Spin:** Spin the current wheel. After a successful spin, the next press advances to the next zone and spins again.
- Normal zones contain one bomb. Landing on it loses all rewards accumulated in the current session.
- Every fifth zone uses a safe silver wheel; every thirtieth zone uses a golden wheel with special rewards. Neither contains a bomb.
- **ÖDÜLLERİ AL / Collect rewards:** End the session successfully while keeping its reward totals visible. In the current implementation, this becomes available after completing a spin in a safe or super zone, while the wheel is idle.
- **YENİDEN BAŞLAT / Restart:** After a bomb or a successful cash-out, start a new session at zone 1 with empty reward totals.

Rewards are held in memory for the current session; they are not saved between application launches.

### Reward progression

Gold and cash use the following calculation:

```text
earned amount = base amount + floor(base amount * (zone - 1) / 10)
```

Each new zone adds 10% of the base amount, rather than compounding the previous reward. For a base reward of 50, zones 1, 2, 5, and 10 award 50, 55, 70, and 95 respectively. Item quantities remain unchanged. Base values in the wheel assets are not modified at runtime.

This growth formula is a game-design choice; the case brief requires improving rewards but does not specify a numerical formula.

## Run in the Editor

1. Clone this repository and open its root folder through Unity Hub with **Unity 6000.3.23f1**.
2. Allow Unity to import the assets and packages.
3. Open `Assets/Scenes/WheelDemo.unity`.
4. Enter Play mode.

Reward definitions are in `Assets/GameData/Rewards`, and wheel configurations are in `Assets/GameData/Wheels`.

## Build for Android

1. Install **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** for this Editor version through Unity Hub.
2. Open **File > Build Profiles** and switch to Android.
3. Use `Assets/Scenes/WheelDemo.unity` as the enabled scene at build index 0.
4. Use **IL2CPP** with **ARM64**. The application ID is `com.berkaykosen.vertigowheeldemo`.
5. Keep **Auto Rotation** enabled with only Landscape Left and Landscape Right allowed.
6. Disable **Export Project** and **Build App Bundle (Google Play)** to produce an APK. Disable **Development Build** for the final submission build.
7. Select **Build** and save the APK under `Builds/Android`.

Generated build files are ignored by Git. The final APK will be attached to a GitHub Release.

## Validation and remaining work

An initial APK was manually smoke-tested in an Android 16 / API 36 ARM64 emulator. The application launched in landscape, displayed the game UI, and ran the wheel interaction. Editor layout checks include 4:3; final captures and regression checks across all required ratios remain pending.

**The existing test APK predates the latest upright-icon and amount-label rotation fix.** It has not been rebuilt with that change yet.

- [ ] Review cash-out timing against the brief: the current flow requires completing the safe/super-zone spin before collecting rewards.
- [ ] Finish UI polish, feedback effects, and animations.
- [ ] Complete a final regression pass for normal/safe/super zones, reward growth, bombs, cash-out, restart, and repeated button presses.
- [ ] Verify the finished UI and capture screenshots at **20:9, 16:9, and 4:3**.
- [ ] Rebuild the APK with the final source and repeat Android testing, including a physical device if available.
- [ ] Record a gameplay video covering the main flows.
- [ ] Publish the final APK as a **GitHub Release** and add the release, video, and screenshots to this README.

## Project layout

```text
Assets/
  Art/UI/             Provided UI sprites
  Art/Atlases/        UI Sprite Atlas
  GameData/Rewards/   Reward ScriptableObjects
  GameData/Wheels/    Bronze, silver, and golden wheel configurations
  Prefabs/            Reusable game prefab
  Scenes/             WheelDemo scene
  Scripts/Data/       Reward and wheel definitions
  Scripts/Gameplay/   Session rules and controller
  Scripts/UI/         Wheel, buttons, and result presentation
```
