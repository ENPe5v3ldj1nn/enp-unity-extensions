# Changelog

## [Unreleased]

### Added
- `LanguageId.Belarusian` (`be`, folder `be_belarusian`), detected from `be` device locales.
  Appended at the end of the enum so persisted integer values of existing languages stay unchanged.
- **Plurals**: a JSON value can be an object of CLDR forms (`zero`/`one`/`two`/`few`/`many`/`other`).
  `LanguageController.GetPlural(key, count)` and `tmpText.SetPluralKey(key, count[, arg1])` pick
  the form via the new `PluralRules` (integer CLDR rules for every `LanguageId`), falling back to
  `other`.
- `LanguageController.TryResolveAvailable` — regional fallback: a device or stored language that
  is not available resolves to another variant of the same language (pt-PT → pt-BR, fr-CA → fr-FR)
  before English. `ResolveSelectedLanguage` uses it.
- Device locale aliases: legacy Android `in` (Indonesian), `iw` (Hebrew), `tl` (Filipino), and
  Norwegian `nb`/`nn`. Previously these devices fell back to English.
- `LanguageId.ToNativeName()` (for language pickers), `ToPrimaryCode()`, and `TryFromCode()`
  (exact inverse of `ToCode()`).
- `LanguageFilesValidator` (Editor): checks every `Resources` language root against English —
  missing keys, type and `{n}` placeholder mismatches, missing plural forms, invalid JSON, and
  literal keys used in code but absent from English. Runs on every Build Guard build through the
  new `LanguageFilesBuildGuardAdapter` (Release fails on errors, Development only logs) and from the
  Language Settings window's audit tab ("Validate all language files").

### Changed
- `LanguageId` is now serialized by Newtonsoft as its code (`"uk"`, `"pt-BR"`) through
  `LanguageIdJsonConverter` instead of an integer. Reading still accepts the legacy integer and the
  enum name, so existing saves migrate on their next write; unknown values read as `EnglishUS`.
  A save written by this version is not readable by an older build (downgrade only).
- `SetKey` / `SetArrayKey` with a null or empty key now clear the text instead of showing `<>`.
- `FitOnceContentSizeFitter` re-fits after `LanguageController.LanguageChanged`, and every fit now
  rebuilds its own layout first (twice: before width, then before height). Previously it measured
  the layout group's cached size, so a label changed while the window was alive (e.g. a runtime
  language switch) kept the old width and wrapped mid-word.
- Keys audit also recognises `SetPluralKey` / `GetPlural` / `GetArray` call sites.
- **Analytics module** (`ENP.UnityExtensions.Analytics`, always compiled) — vendor-agnostic
  analytics core: `AnalyticsService`, `AnalyticsParam`, persisted `PendingAnalyticsQueue`
  (events logged before the backend is ready survive an app restart), `AnalyticsSessionCounter`
  and `IAnalyticsCommonParamsProvider` for per-project common parameters.
  `NullAnalyticsBackend` / `NullCrashReporter` keep the module usable without any SDK.
- **Firebase module** (`ENP.UnityExtensions.Firebase`, define constraint `ENP_FIREBASE`) —
  `FirebaseBootstrap` (single dependency check, main-thread continuation),
  `FirebaseAnalyticsBackend` and `FirebaseCrashReporter`.
- `AnalyticsAdSink` (define constraint `ENP_ADMOB`) — bridges the Ads module's
  `IAdAnalyticsSink` into `AnalyticsService`, so ad events reach whichever backend is wired.
- VContainer registration helpers: `RegisterAnalyticsModule`, `RegisterNullAnalyticsBackend`,
  `RegisterFirebaseAnalyticsBackend`.

## [1.1.0]

### Changed (BREAKING)
- Window and popup enter/exit animations migrated from Unity `Animator` + `AnimationClip`
  to **DoTween**. `AnimatedWindow` no longer uses an `Animator`; motion is now data-driven
  via a `WindowAnimationConfig` ScriptableObject and driven by DoTween tweens.
- `AbstractUiController.OpenNext` rewritten to a linear async flow using **UniTask**, with
  cancellation of the previous transition when a new one starts.
- `AnimatedWindow.Close(...)` now resolves its end reliably from the tween's completion
  instead of guessing the clip length via `WaitForSeconds`.
- Window registry in `AbstractUiController` changed from `Dictionary<Type, AnimatedWindow>` to a
  built-once `(Type, AnimatedWindow)[]` array, allowing several windows of the same type
  (disambiguated by `gameObject.name`). **`SetupMap` signature changed** to
  `List<(Type, AnimatedWindow)>` — consuming controllers must update their overrides.
  `GetWindow<T>(string name = null)` accepts an optional name to pick a specific instance.

### Added
- New dependency: **UniTask** (`com.cysharp.unitask`). Install via OpenUPM or git URL and
  ensure the `UniTask` assembly is referenced by the runtime asmdef (already wired).
- `WindowAnimationConfig` ScriptableObject holding per-`AnimatedWindowAnimation` recipes
  (offset / alpha / duration / ease). Defaults are seeded from the legacy `.anim` clips.

### Migration (per consuming project)
Each window/popup that used the Animator-based system must be updated in scene/prefab:
1. Ensure the window GameObject has a `CanvasGroup` and a `RectTransform`
   (auto-assigned on `AnimatedWindow` via `OnValidate`).
2. Create a `WindowAnimationConfig` asset (Create → ENP → UI → Window Animation Config)
   and assign it to each `AnimatedWindow._config`.
3. Remove the `Animator` component from windows (the field no longer exists).
4. Verify the seeded recipe values against previous visuals and tune in the inspector.

> The legacy `.anim` / `AnimatorController` assets under `Animations/` are left in place and
> can be deleted in a separate cleanup step once the DoTween path is confirmed.

## [1.0.3]
- Previous releases.
