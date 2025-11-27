# PopupSmoothTest

Card-style popup animations (separate from the window set).

- Controller: `PopupSmooth.controller`
- Animations: `OpenPopupCard`, `ClosePopupCard`, `IdlePopupCard`
- Use enum keys: `AnimatedWindowConstant.OpenPopupCard` / `AnimatedWindowConstant.ClosePopupCard`
- Assign the controller to a popup `AnimatedWindow`/`PopupWindow` and call `Open`/`Close` with the new enum values.
