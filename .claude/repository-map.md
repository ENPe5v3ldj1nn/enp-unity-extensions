# Repository Map — enp-unity-extensions

UPM package `com.enope.unity.extensions`. Reusable Unity runtime/editor modules, consumed by
game projects (e.g. NeuroDash) via git URL.

Source is the truth; this map is an index.

## Assemblies

| Assembly | Path | Define constraints |
| --- | --- | --- |
| `ENP.Extensions.Runtime` | `Runtime/` | — |
| `ENP.Extensions.Editor` | `Editor/` | — |
| `ENP.UnityExtensions.VContainer` | `Runtime/Integrations/VContainer/` | `ENP_VCONTAINER` |
| `ENP.UnityExtensions.Ads` | `Runtime/Modules/Ads/` | `ENP_ADMOB` |
| `ENP.UnityExtensions.Ads.VContainer` | `Runtime/Modules/Ads/Integrations/VContainer/` | `ENP_ADMOB`, `ENP_VCONTAINER` |
| `ENP.UnityExtensions.Analytics` | `Runtime/Modules/Analytics/` | — |
| `ENP.UnityExtensions.Analytics.Ads` | `Runtime/Modules/Analytics/Integrations/Ads/` | `ENP_ADMOB` |
| `ENP.UnityExtensions.Analytics.VContainer` | `Runtime/Modules/Analytics/Integrations/VContainer/` | `ENP_VCONTAINER` |
| `ENP.UnityExtensions.Firebase` | `Runtime/Modules/Firebase/` | `ENP_FIREBASE` |
| `ENP.UnityExtensions.Firebase.VContainer` | `Runtime/Modules/Firebase/Integrations/VContainer/` | `ENP_FIREBASE`, `ENP_VCONTAINER` |

Convention: optional third-party integrations live in their own assembly gated by a define
constraint, so the package compiles when the SDK is absent. `ENP_VCONTAINER` is derived from a
`versionDefines` entry; `ENP_ADMOB` and `ENP_FIREBASE` are set manually by the consuming project.

## Runtime modules (`Runtime/Modules/`)

- **Ads** — AdMob wrapper. `AdMobService` (init + gating) with `AdMobInterstitial` /
  `AdMobRewarded` / `AdMobAppOpenAd`; `ConsentService` (UMP), `AdThrottleService` (caps and
  cooldowns), `AdReadinessCoordinator`, `IosAttAuthorizationRequester`, `AdsConfig` (SO).
  `AdAnalyticsService` + `IAdAnalyticsSink` is the seam for reporting; the sink is not
  registered by `RegisterAdsModule` — the project supplies it.
- **Analytics** — vendor-agnostic core. `AnalyticsService` (composition + queueing) over
  `IAnalyticsBackend`; `PendingAnalyticsQueue` (persisted), `AnalyticsSessionCounter`,
  `IAnalyticsCommonParamsProvider`, `ICrashReporter`, Null implementations.
  `AnalyticsAdSink` bridges the Ads module into it.
- **Firebase** — `FirebaseBootstrap` (one dependency check, main-thread continuation) plus
  `FirebaseAnalyticsBackend` and `FirebaseCrashReporter`.
- **Window** — window stack: `AbstractUiController` (static `ShowExclusive` / `GetWindow`,
  `[UiWindow]` auto-registration), `AnimatedWindow` (DOTween + UniTask), `WindowHistory`,
  `WindowConfig`.
- **Popup**, **Pool**, **Storage** (JSON files under `persistentDataPath`), **Language**
  (JSON dictionaries in `Resources/Languages/<lang>/`), **Timer**, **Fonts**, **Gestures**,
  **Vibration**, **FPS**, **AppState**, **InAppReview**, **Debug** (`Deb`), **Other**.
- **UI** — `RoundedShapeGraphic` and SDF-based effects (EdgeGlow, InnerFog, LiquidEdge,
  ProceduralVignette, Wash), gradients (current + legacy), `AnimatedButton`, sliders, layout,
  scroll. Shaders in `Runtime/Shaders/`.
- **Sprite2D** — non-UI counterparts of the rounded-shape and gradient renderers.

## Editor (`Editor/`)

`BuildGuard` (`Editor/BuildGuard/`) — `BuildGuardBuildInterceptor` hooks
`BuildPlayerWindow.RegisterBuildPlayerHandler`, shows a Release/Development/Cancel dialog on
every build (settings in the consuming project's `ProjectSettings/BuildGuardSettings.asset`),
and on Release appends `BuildGuardSettings.ReleaseDefineSymbol` (default `APP_BUILD_RELEASE`)
to `BuildPlayerOptions.extraScriptingDefines` — **per build only, never written into Player
Settings' persistent `scriptingDefineSymbols`.** `IBuildGuardProjectAdapter` implementations
(discovered via `TypeCache`, e.g. the consuming project's `ProjectBuildGuardAdapter`) get
`Validate`/`Apply`/`Restore` hooks to flip other release-only state (debug logging, DOTween
debug mode) and undo it after the build if `_restoreStateAfterBuild` is set. See README.md
"Build Guard" section for the exact failure mode if this symbol is ever added to Player
Settings by hand (happened once, 26.08.2026, in CardGame — silently defeats the dialog).
`BuildGuardScriptingDefineGuard` (added 26.08.2026, same file group) is the automated guard
against a repeat: strips `ReleaseDefineSymbol` back out of every platform's Player Settings
scripting define symbols on editor load and again at the top of `OnBuildPlayer`, logging a
warning per platform it had to fix.
`LanguageSettings` (localization window with keys audit and translation tabs),
`TimeScaleToolbar`, custom editors for rounded shapes / sliders / images, `WindowSetupValidator`.

`iOS` (`Editor/iOS/`, added 26.08.2026 — moved here from a consuming project's own Editor
folder): `EnpNativeFrameworkLinker` is a `[PostProcessBuild]` step (idempotent via
`ContainsFramework`) that links, into the `UnityFramework` Xcode target only, the system
frameworks the plugin's own always-compiled native iOS code needs — `AppTrackingTransparency`
(weak, for `Runtime/Plugins/iOS/NeuroDashTrackingAuthorizationBridge.mm`) and `AudioToolbox`
(for `VibrationController.TriggerIOS`). Lives in the plugin, not per-project, because both
symbols compile into any iOS build that consumes this package, define-constraint-free. Add
future framework needs to its `RequiredFrameworks` array rather than a project-local copy.
Linking the framework is necessary but not sufficient for a P/Invoke onto it: `TriggerIOS`'s
`AudioServicesPlaySystemSound` `[DllImport]` must target `"__Internal"`, not `"AudioToolbox"`
(fixed 26.08.2026) — IL2CPP treats a named-framework `DllImport` as a runtime `dlopen`, which
iOS system frameworks (statically linked at build time) don't support; `__Internal` resolves
the symbol from the already-loaded process image instead.

## Conventions

- Namespaces: `ENP.UnityExtensions.*` (root runtime namespace is `ENP.UnityExtensions.Runtime`).
- Optional deps must never break compilation — gate with a define constraint on a
  dedicated assembly, never with `#if` inside the main runtime assembly.
- Verbose debug logging is compiled out with `APP_BUILD_RELEASE` in the Ads module.
- `APP_BUILD_RELEASE` (or whatever `BuildGuardSettings.ReleaseDefineSymbol` is set to) must
  never be added to a consuming project's Player Settings scripting define symbols directly —
  it belongs solely to Build Guard's per-build `extraScriptingDefines` injection. See
  `Editor/BuildGuard` above.
- VContainer wiring is exposed as `IContainerBuilder` extension methods per module
  (`RegisterAdsModule`, `RegisterAnalyticsModule`, `RegisterFirebaseAnalyticsBackend`, ...).
