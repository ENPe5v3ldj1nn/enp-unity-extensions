# WindowSmoothTest

New smooth window animations for testing without touching the original set.

- Controller: `WindowSmooth.controller`
- Animations: `OpenSmoothRight`, `OpenSmoothLeft`, `CloseSmoothRight`, `CloseSmoothLeft`, `IdleSmooth`
- Use enum keys: `AnimatedWindowConstant.OpenSmoothRight`, `AnimatedWindowConstant.OpenSmoothLeft`, `AnimatedWindowConstant.CloseSmoothRight`, `AnimatedWindowConstant.CloseSmoothLeft`
- Assign the controller to a test `AnimatedWindow` and call `Open`/`Close` with the new enum values.
