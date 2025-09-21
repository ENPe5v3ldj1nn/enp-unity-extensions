# ENP Unity Extensions (all-in-one)

“Install & forget” helpers for Unity maintained by **ENPe5v3ldj1nn**.

## What you get
- Runtime utilities (TextMeshPro-ready).
- Optional integrations with **DOTween** and **UniRx**.
- **Auto-detector** on import that checks if DOTween/UniRx are present and offers quick links to install them.

## Install

### Via Git URL (recommended)
1. In Unity, open **Window → Package Manager**
2. Click **+ → Add package from git URL…**
3. Paste:

```
https://github.com/ENPe5v3ldj1nn/enp-unity-extensions.git#v0.1.0
```

> `#v0.1.0` refers to the version tag. It is recommended to specify a tag for stability.  
> If omitted, Unity will pull the latest `main` branch.

### As classic Assets
You can also copy the needed scripts directly into `Assets/ENP/...` (no UPM management).

## Dependencies
- **TextMeshPro** (already declared in `package.json`)
- **DOTween** *(optional)* — [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
- **UniRx** *(optional)* — [OpenUPM](https://openupm.com/packages/com.neuecc.unirx/)

> The package compiles even if DOTween/UniRx are missing. Related modules stay inactive until installed.

## Auto-detector
- On import, you will get a dialog if something is missing.
- You can run it manually anytime from **ENP/Check Dependencies**.
- Option to disable future prompts is available.

## Versioning
This package follows Semantic Versioning: **MAJOR.MINOR.PATCH**.

## Troubleshooting
- **Missing DOTween types** → install DOTween.
- **UniRx not found** → install via OpenUPM (you may need to add a scoped registry).

---

_Last updated: 2025-09-21_
