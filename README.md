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
https://github.com/ENPe5v3ldj1nn/enp-unity-extensions.git
```

### As classic Assets
You can also copy the needed scripts directly into `Assets/ENP/...` (no UPM management).

## Dependencies
- **TextMeshPro** (already declared in `package.json`)
- **DOTween** *(optional)* — [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
- **UniRx** *(optional)* — [OpenUPM](https://openupm.com/packages/com.neuecc.unirx/)
- **Google Play In-App Review** *(Android, required for `InAppReviewController`)* — install the UPM package `com.google.play.review` or import the latest `com.google.play.review-*.unitypackage` from [GitHub Releases](https://github.com/google/play-in-app-reviews-unity/releases) (see “Google Play In-App Reviews (Android)” below)

> The package compiles even if DOTween/UniRx are missing. Related modules stay inactive until installed.

### Newtonsoft Json (optional)

Some features in this project use **Newtonsoft Json**.  
Unity ships an official package for it called `com.unity.nuget.newtonsoft-json`.

You can install it in two ways:

#### 1. Install via Package Manager (recommended)

1. In Unity, open **Window → Package Manager**.
2. Click the **+** button in the top-left corner.
3. Choose **Add package by name...** (in older Unity versions this option may be missing).
4. In the **Name** field enter:

   `com.unity.nuget.newtonsoft-json`

5. (Optional) In the **Version** field you can specify a version, for example:

   `3.0.1`

   If you leave it empty, Unity will install the latest available version.
6. Click **Add**.

Unity will download and install the official Newtonsoft Json package.  
You should now see **Newtonsoft Json** in the Package Manager list.

#### 2. Install via `manifest.json`

If your Unity version does not have **Add package by name...**, you can add the package manually:

1. Close Unity.
2. In your project folder, open:  
   `Packages/manifest.json`
3. Inside the `"dependencies"` section add a line like this:

   ```json
   "com.unity.nuget.newtonsoft-json": "3.0.1",
### Google Play In-App Reviews (Android)

For Android in-app review this package uses the official **Google Play In-App Reviews plugin for Unity**:  
`https://github.com/google/play-in-app-reviews-unity` :contentReference[oaicite:0]{index=0}

You **must** install this plugin into your project before calling `InAppReviewController` on Android.

#### Requirements

- **Unity**: 2019.x / 2020.x / newer (2018.4+ is also supported; older versions are not). :contentReference[oaicite:1]{index=1}
- **Android**: min. SDK **21 (Lollipop)** or higher. :contentReference[oaicite:2]{index=2}

When you install the plugin, it will automatically bring in:

- **External Dependency Manager (EDM4U)**
- **Play Core plugin for Unity**
- **Play Common plugin for Unity** :contentReference[oaicite:3]{index=3}

You don’t need to install these manually.

---

#### Option 1 — Install via OpenUPM (UPM package `com.google.play.review`)

If you already use **OpenUPM** in your project:

1. Make sure OpenUPM is configured for your project (scoped registry added).
2. In Unity, open **Window → Package Manager**.
3. In the top-left filter, switch to **My Registries** (or the registry that points to OpenUPM).
4. Find package **Google Play In-app Review** with id:

   `com.google.play.review` :contentReference[oaicite:4]{index=4}

5. Click **Install**.

Alternatively, if you use the **OpenUPM CLI**, you can add the package from the command line (see the “Install via command-line” section on the OpenUPM page). :contentReference[oaicite:5]{index=5}

---

#### Option 2 — Install via `.unitypackage` from GitHub Releases (simple)

If you don’t want to set up OpenUPM, you can import the plugin as a classic Unity package:

1. Open the GitHub repo releases page: **Releases → Latest**. :contentReference[oaicite:6]{index=6}
2. Download the latest `com.google.play.review-*.unitypackage`.
3. In Unity, go to **Assets → Import Package → Custom Package…**.
4. Select the downloaded `.unitypackage` and click **Import**.
5. Keep everything checked (the plugin + EDM4U + Play Core + Play Common).

Unity will import the plugin and all required Google Play support plugins automatically. :contentReference[oaicite:7]{index=7}

---

#### Notes

- The Google plugin is **Android-only**. It’s safe to keep it in a cross-platform project – it won’t affect iOS builds.
- On iOS, `InAppReviewController` uses `Device.RequestStoreReview()` and does **not** require any external plugins.


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
