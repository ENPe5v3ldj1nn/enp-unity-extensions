# Changelog

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
