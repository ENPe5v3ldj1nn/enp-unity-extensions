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

`BuildGuard` (build-mode validation via `IBuildGuardProjectAdapter`), `LanguageSettings`
(localization window with keys audit and translation tabs), `TimeScaleToolbar`, custom editors
for rounded shapes / sliders / images, `WindowSetupValidator`.

## Conventions

- Namespaces: `ENP.UnityExtensions.*` (root runtime namespace is `ENP.UnityExtensions.Runtime`).
- Optional deps must never break compilation — gate with a define constraint on a
  dedicated assembly, never with `#if` inside the main runtime assembly.
- Verbose debug logging is compiled out with `ENP_ADS_RELEASE` in the Ads module.
- VContainer wiring is exposed as `IContainerBuilder` extension methods per module
  (`RegisterAdsModule`, `RegisterAnalyticsModule`, `RegisterFirebaseAnalyticsBackend`, ...).
