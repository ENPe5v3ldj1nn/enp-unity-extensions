# PopupSmoothTest

Card-style popup animations (separate from the window set).

- Controller: `PopupSmooth.controller`
- Animations: `OpenPopupCard`, `ClosePopupCard`, `IdlePopupCard`
- Use enum keys: `AnimatedWindowAnimation.OpenPopupCard` / `AnimatedWindowAnimation.ClosePopupCard`
- Assign the controller to a popup `AnimatedWindow`/`PopupWindow` and call `Open`/`Close` with the new enum values.
