# ENP Unity Extensions

Install and forget helpers for Unity maintained by **ENPe5v3ldj1nn**.

## What you get
- Runtime utilities ready for TextMeshPro.
- Optional integrations with **DOTween** and **UniRx**.
- An auto-detector on import that checks whether DOTween/UniRx are present and can offer quick install links.

## Install

### Via Git URL
1. In Unity, open **Window -> Package Manager**.
2. Click **+ -> Add package from git URL...**
3. Paste:

```text
https://github.com/ENPe5v3ldj1nn/enp-unity-extensions.git
```

### As classic Assets
You can also copy the needed scripts directly into `Assets/ENP/...` if you do not want to use UPM.

## Package ID
This package uses the UPM id:

```json
"name": "com.enope.unity.extensions"
```

## Dependencies
- **TextMeshPro** - declared in `package.json`
- **DOTween** *(optional)* - [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
- **UniRx** *(optional)* - [OpenUPM](https://openupm.com/packages/com.neuecc.unirx/)
- **Google Play In-App Review** *(Android, required for `InAppReviewController`)* - install the UPM package `com.google.play.review` or import the latest `com.google.play.review-*.unitypackage` from [GitHub Releases](https://github.com/google/play-in-app-reviews-unity/releases)

> The package compiles even if DOTween/UniRx are missing. Related modules stay inactive until installed.

### Newtonsoft Json
Some features in this project use **Newtonsoft Json**. Unity ships an official package for it called `com.unity.nuget.newtonsoft-json`.

You can install it in two ways:

#### 1. Install via Package Manager
1. In Unity, open **Window -> Package Manager**.
2. Click the **+** button in the top-left corner.
3. Choose **Add package by name...**.
4. In the **Name** field enter:

```text
com.unity.nuget.newtonsoft-json
```

5. Optionally set a version, for example:

```text
3.0.1
```

6. Click **Add**.

#### 2. Install via `manifest.json`
If your Unity version does not have **Add package by name...**, you can add the package manually:

1. Close Unity.
2. Open `Packages/manifest.json`.
3. Inside the `"dependencies"` section add:

```json
"com.unity.nuget.newtonsoft-json": "3.0.1"
```

## Google Play In-App Reviews
For Android in-app review this package uses the official **Google Play In-App Reviews plugin for Unity**:

https://github.com/google/play-in-app-reviews-unity

You must install this plugin before calling `InAppReviewController` on Android.

### Requirements
- **Unity**: 2019.x / 2020.x / newer
- **Android**: min. SDK **21 (Lollipop)** or higher

When you install the plugin, it will automatically bring in:
- **External Dependency Manager (EDM4U)**
- **Play Core plugin for Unity**
- **Play Common plugin for Unity**

You do not need to install these manually.

#### Option 1 - Install via OpenUPM
If you already use **OpenUPM** in your project:

1. Make sure OpenUPM is configured for your project.
2. In Unity, open **Window -> Package Manager**.
3. Switch the top-left filter to **My Registries**.
4. Find package **Google Play In-app Review** with id `com.google.play.review`.
5. Click **Install**.

#### Option 2 - Install via `.unitypackage`
If you do not want to set up OpenUPM:

1. Open the GitHub repo releases page.
2. Download the latest `com.google.play.review-*.unitypackage`.
3. In Unity, go to **Assets -> Import Package -> Custom Package...**.
4. Select the downloaded `.unitypackage` and click **Import**.
5. Keep everything checked.

## Auto-detector
- On import, you will get a dialog if something is missing.
- You can run it manually anytime from **ENP/Check Dependencies**.
- An option to disable future prompts is available.

## Versioning
This package follows Semantic Versioning: **MAJOR.MINOR.PATCH**.

## Troubleshooting
- **Missing DOTween types** - install DOTween.
- **UniRx not found** - install via OpenUPM and make sure the scoped registry is configured.

## Language System
This package includes a lightweight localization system built around `LanguageController`, `LanguageText` and `LanguageExtension`.

### How it works
- Each language has its own folder inside `Resources/Languages/`.
- Inside each folder you can place multiple JSON files.

Example structure:

```text
Assets/Resources/Languages/
  english/
    MainMenu.json
    Settings.json
  ukrainian/
    MainMenu.json
    Settings.json
```

- All JSON files inside the active language folder are loaded and merged into a single dictionary.

### JSON file format
Each JSON file is a simple dictionary of keys and strings:

```json
{
  "menu.play": "Play",
  "menu.settings": "Settings"
}
```

### Duplicate keys
- If the same key appears in multiple JSON files for the same language, the last loaded file overwrites the value.
- This allows you to override specific keys by adding small JSON patches without touching the main files.

### Using localized text in UI
```csharp
tmpText.SetKey("menu.play");
tmpText.SetKey("score", points);
```

If you frequently update only the formatting parameters, you can bind the key once and then update only values without re-fetching the localized string:

```csharp
tmpText.SetKey("round.label", currentRound, maxRounds);
tmpText.UpdateValue(currentRound, maxRounds);
```

### Switching language at runtime
```csharp
LanguageController.SetLanguage(SystemLanguage.Ukrainian);
```

This reloads all JSON files from `Resources/Languages/ukrainian/` and updates every `LanguageText` in the scene.

### Custom resources path
If you want a different folder than `Languages/`, set it once at startup:

```csharp
LanguageController.SetResourcesPath("MyLoc");
```

Then place your files under `Resources/MyLoc/english/`, `Resources/MyLoc/ukrainian/`, etc.

## Window / UI System
The package provides a small window stack built around `AbstractUiController` and
`AnimatedWindow`. Windows are registered automatically — no manual map to maintain.

### Setup
1. Create your controller by extending `AbstractUiController`:

```csharp
public class UiController : AbstractUiController
{
    public new void Initialize() => base.Initialize();
}
```

2. In the Inspector, assign **Windows Root** (`_windowsRoot`) to the Transform that
   contains your window objects (usually your main Canvas). Leave it empty to scan the
   controller's own children.

### Registering windows
Mark each top-level window type with `[UiWindow]`. On `Initialize()` the controller
scans **Windows Root** and auto-registers every `AnimatedWindow` that carries the
attribute:

```csharp
[UiWindow]
public class MainMenu : AnimatedWindow { }
```

- Opt-in: only attribute-marked types are registered, so nested `AnimatedWindow`
  sub-views stay out of the lookup table.
- Not inherited — each concrete window declares `[UiWindow]` explicitly.
- Registration order follows hierarchy order; it does not affect lookups.

For rare cases (windows outside the root, or not attribute-marked) you may still
override `SetupMap` and call `RegisterWindow(window)` — it is idempotent, so a window
covered by both discovery and a manual call is registered once.

### Opening and querying windows
```csharp
AbstractUiController.ShowExclusive<MainMenu>();          // close others, open this one
AbstractUiController.ShowExclusive<SettingsWindow>(onClose);
var window = AbstractUiController.GetWindow<MultiGameWindow>();

// Disambiguate when several instances of the same type exist, by GameObject name:
var named = AbstractUiController.GetWindow<PopupWindow>("ConfirmPopup");
```

`GetWindow<T>()` returns the exact-type match; if you request a base type and exactly
one subtype is registered, that subtype is returned. Passing a `name` matches the
window's `gameObject.name` directly.

## Ads
AdMob wrapper (`ENP.UnityExtensions.Ads`) with interstitial / rewarded / app open, UMP
consent, ATT and per-session throttling built in. The assembly only compiles when the
project defines **`ENP_ADMOB`** — without it, none of the Ads code (or the GoogleMobileAds
dependency) is part of the build.

### Setup
1. Import the **Google Mobile Ads Unity plugin** ([GitHub releases](https://github.com/googleads/googleads-mobile-unity/releases)).
   It brings EDM4U and resolves the native Android/iOS AdMob SDKs.
2. Add the scripting define symbol **`ENP_ADMOB`** (Project Settings → Player → Scripting
   Define Symbols). Add **`ENP_ADS_RELEASE`** as well in your release/production builds —
   it switches ad units from Google's public test ids to your production ids and silences
   verbose debug logging (`ConsentService`, `AdAnalyticsService`).
3. Create an `AdsConfig` asset: **Create → ENP → Ads → Ads Config**. Fill in production ad
   unit ids per platform (interstitial / rewarded / app open) and the UMP/throttling fields;
   test ids are hardcoded and used automatically while `ENP_ADS_RELEASE` is not defined.
4. Set up AdMob's `AndroidManifest.xml` App ID meta-data and the iOS `SKAdNetworkItems` /
   `NSUserTrackingUsageDescription` entries as required by the plugin — this package does not
   generate them.

### Registration (VContainer)
Requires `ENP_VCONTAINER` as well (separate `ENP.UnityExtensions.Ads.VContainer` assembly).

```csharp
builder.RegisterAdsModule(adsConfig);
```

This registers `AdAnalyticsService`, `AdThrottleService`, `ConsentService`, `AdMobService`,
`IosAttAuthorizationRequester` and `AdReadinessCoordinator` as singletons. It does **not**
register an `IAdAnalyticsSink` — `AdAnalyticsService` requires one, so supply it yourself
(see [Ads integration](#ads-integration) below for the built-in Firebase-backed option, or
implement `IAdAnalyticsSink` directly).

You must also register an `IAdSessionState` implementation (app-open ad needs it to know
whether the app is in the foreground) — the package has no default for it.

### Startup flow
```csharp
await _adReadinessCoordinator.WarmupAtStartupAsync();      // UMP consent info, no UI
var isReady = await _adReadinessCoordinator.EnsureAdsReadyAsync(allowUi: true);
// consent flow (shows UI if required) -> ATT prompt -> MobileAds.Initialize -> optional app open init
```

`AdReadinessCoordinator` is the intended single entry point — it sequences consent, ATT and
AdMob initialization so individual ad types don't need to know about that order.

```csharp
_adMobService.ShowInterstitialAd(onShow, placement: "level_complete");
_adMobService.ShowRewardedAd(onShowed, onReward, placement: "extra_life");
_adMobService.PreloadInterstitial();
_adMobService.PreloadRewarded();
```

`ConsentService.SetEditorAdsEnabled(true)` bypasses consent/ATT/init entirely in the Editor
(mock interstitial, `IsAdsReady` reports `true`) so you can iterate without waiting on the
UMP/AdMob SDK.

## Analytics
The analytics core (`ENP.UnityExtensions.Analytics`) is vendor-agnostic: it knows nothing
about Firebase. A backend is plugged in separately.

### What the core does
- Queues events logged before the backend is ready and **persists** that queue, so events
  from the very first session survive an app restart (capped at 200 events).
- Appends common parameters to every event via `IAnalyticsCommonParamsProvider`
  (`app_version` and `session_number` ship built in; add your own for A/B group and similar).
- Owns the per-install session counter (`AnalyticsSessionCounter`, stored via `Storage`).

```csharp
_analyticsService.BeginSession();

_analyticsService.LogEvent("level_completed",
    new AnalyticsParam("level", 12),
    new AnalyticsParam("duration_seconds", 41.5f));

_analyticsService.LogScreenView("MainMenu");
```

Project-specific common parameters:

```csharp
public sealed class AbGroupCommonParamsProvider : IAnalyticsCommonParamsProvider
{
    public void AppendParams(IList<AnalyticsParam> destination)
    {
        destination.Add(new AnalyticsParam("ab_group", ABController.AnalyticsGroup));
    }
}
```

Register it as `IAnalyticsCommonParamsProvider` and it is applied to every event. An event
parameter with the same key always wins over a common one.

### Backends
| Backend | Assembly | Requires |
| --- | --- | --- |
| `NullAnalyticsBackend` / `NullCrashReporter` | core | nothing (logs to console in the Editor) |
| `FirebaseAnalyticsBackend` / `FirebaseCrashReporter` | `ENP.UnityExtensions.Firebase` | `ENP_FIREBASE` |

### Firebase setup
1. Import the Firebase Unity SDK (`FirebaseAnalytics.unitypackage`, `FirebaseCrashlytics.unitypackage`)
   and add `google-services.json` / `GoogleService-Info.plist` to the project.
2. Add the scripting define symbol **`ENP_FIREBASE`** (Project Settings → Player → Scripting Define Symbols).
   Without it the Firebase assemblies are excluded from compilation entirely.
3. Register the module and call `BeginSession()` once at startup.

`FirebaseBootstrap` runs `CheckAndFixDependenciesAsync` once for both analytics and Crashlytics,
and resumes on the main thread. User ids, user properties and Crashlytics custom keys set before
initialization completes are cached and applied as soon as it does. String parameter values are
truncated to Firebase's 100-character limit; events are capped at 25 parameters.

### Registration (VContainer)
```csharp
builder.RegisterAnalyticsModule();

#if ENP_FIREBASE
builder.RegisterFirebaseAnalyticsBackend();
#else
builder.RegisterNullAnalyticsBackend();
#endif

builder.Register<IAnalyticsCommonParamsProvider, AbGroupCommonParamsProvider>(Lifetime.Singleton);
```

Then, at startup:

```csharp
_analyticsService.BeginSession();
_crashReporter.Initialize();
```

### Ads integration
With the Ads module present (`ENP_ADMOB`), `AnalyticsAdSink` forwards `IAdAnalyticsSink`
callbacks into `AnalyticsService`:

```csharp
builder.RegisterAdsModule(adsConfig);
builder.Register<IAdAnalyticsSink, AnalyticsAdSink>(Lifetime.Singleton);
```

It emits `ad_offer_shown`, `ad_load_failed`, `ad_clicked`, `ad_show_failed`,
`ad_reward_granted`, `ad_retry_stopped`, `ad_load_skipped_throttled` and `ad_load_expired`.

---

_Last updated: 2026-08-16_
