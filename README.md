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


## Language System

This package includes a lightweight **localization system** built around `LanguageController`, `LanguageText` and `LanguageExtension`.

### How it works
- Each **language** has its own folder inside `Resources/Languages/`.
- Inside each folder you can place **multiple JSON files**.  
  Example structure:

  ```
  Assets/Resources/Languages/
    english/
      MainMenu.json
      Settings.json
    ukrainian/
      MainMenu.json
      Settings.json
  ```

- All JSON files inside the active language folder are loaded and merged into a single dictionary.

### JSON file format
Each JSON file is a simple dictionary of keys and strings:

```json
{
  "menu.play": "Play",
  "menu.settings": "Settings"
}
```

### Duplicate keys
- If the same key appears in multiple JSON files for the same language, the **last loaded file overwrites the value**.
- This allows you to override specific keys by adding small JSON patches without touching the main files.

### Using localized text in UI
- Attach `LanguageText` automatically by using the extension methods:

```csharp
tmpText.SetKey("menu.play");
tmpText.SetKeyWithParams("score", points);
```

- The component subscribes to `LanguageController.OnLanguageChanged` and updates automatically when you switch languages.

### Switching language at runtime
```csharp
LanguageController.SetLanguage(SystemLanguage.Ukrainian);
```

This reloads all JSON files from `Resources/Languages/ukrainian/` and updates every `LanguageText` in the scene.

### Custom resources path
If you want a different folder than `Languages/`, set it once at startup:

```csharp
LanguageController.SetResourcesPath("MyLoc");
```

Then place your files under `Resources/MyLoc/english/`, `Resources/MyLoc/ukrainian/`, etc.

---

---

_Last updated: 2025-09-21_
