# WindowSmoothTest

New smooth window animations for testing without touching the original set.

- Controller: `WindowSmooth.controller`
- Animations: `OpenSmoothRight`, `OpenSmoothLeft`, `CloseSmoothRight`, `CloseSmoothLeft`, `IdleSmooth`
- Use enum keys: `AnimatedWindowAnimation.OpenSmoothRight`, `AnimatedWindowAnimation.OpenSmoothLeft`, `AnimatedWindowAnimation.CloseSmoothRight`, `AnimatedWindowAnimation.CloseSmoothLeft`
- Assign the controller to a test `AnimatedWindow` and call `Open`/`Close` with the new enum values.
