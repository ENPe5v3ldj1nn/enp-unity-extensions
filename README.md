# ENP Unity Extensions (all‑in‑one)

“Install & forget” helpers for Unity maintained by **ENPe5v3ldj1nn**.

## What you get
- Runtime utilities (TextMeshPro-ready).
- Optional integrations with **DOTween** and **UniRx**.
- **Auto‑detector** on import that checks if DOTween/UniRx are present and offers quick links to install them.

## Install

### As a local UPM package
1. Copy `enp-unity-extensions` someplace in your project or drive.
2. Unity → **Window → Package Manager → + → Add package from disk…** → pick this folder’s `package.json`.

### As classic Assets
Copy scripts you need under `Assets/ENP/...` (no UPM management).

## Dependencies
- **TextMeshPro** (declared in `package.json`).
- **DOTween** *(optional)* — Asset Store: https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676
- **UniRx** *(optional)* — OpenUPM: https://openupm.com/packages/com.neuecc.unirx/

> Package compiles even if DOTween/UniRx are missing. Related modules remain inactive until you install them.

## Auto‑detector
- After import, you'll get a dialog if something is missing.
- Run manually anytime: **ENP/Check Dependencies**.
- Disable future prompts via “Hide future prompts”.

## Versioning
Semantic Versioning: MAJOR.MINOR.PATCH.

## Troubleshooting
- **Missing DOTween types** → install DOTween or temporarily disable modules that need it.
- **UniRx not found** → install via OpenUPM (may require a scoped registry in your project manifest).

_Last updated: 2025-09-21._
