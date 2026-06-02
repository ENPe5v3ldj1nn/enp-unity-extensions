# ENP Unity Extensions

Install and forget helpers for Unity maintained by **ENPe5v3ldj1nn**.

## What you get
- Runtime utilities ready for TextMeshPro.
- Optional integrations with **DOTween** and **UniRx**.
- An auto-detector on import that checks whether DOTween/UniRx are present and can offer quick install links.

## Install

### Via Git URL
1. In Unity, open **Window -> Package Manager**.
2. Click **+ -> Add package from git URL...**
3. Paste:

```text
https://github.com/ENPe5v3ldj1nn/enp-unity-extensions.git
```

### As classic Assets
You can also copy the needed scripts directly into `Assets/ENP/...` if you do not want to use UPM.

## Package ID
This package uses the UPM id:

```json
"name": "com.enope.unity.extensions"
```

## Dependencies
- **TextMeshPro** - declared in `package.json`
- **DOTween** *(optional)* - [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
- **UniRx** *(optional)* - [OpenUPM](https://openupm.com/packages/com.neuecc.unirx/)
- **Google Play In-App Review** *(Android, required for `InAppReviewController`)* - install the UPM package `com.google.play.review` or import the latest `com.google.play.review-*.unitypackage` from [GitHub Releases](https://github.com/google/play-in-app-reviews-unity/releases)

> The package compiles even if DOTween/UniRx are missing. Related modules stay inactive until installed.

### Newtonsoft Json
Some features in this project use **Newtonsoft Json**. Unity ships an official package for it called `com.unity.nuget.newtonsoft-json`.

You can install it in two ways:

#### 1. Install via Package Manager
1. In Unity, open **Window -> Package Manager**.
2. Click the **+** button in the top-left corner.
3. Choose **Add package by name...**.
4. In the **Name** field enter:

```text
com.unity.nuget.newtonsoft-json
```

5. Optionally set a version, for example:

```text
3.0.1
```

6. Click **Add**.

#### 2. Install via `manifest.json`
If your Unity version does not have **Add package by name...**, you can add the package manually:

1. Close Unity.
2. Open `Packages/manifest.json`.
3. Inside the `"dependencies"` section add:

```json
"com.unity.nuget.newtonsoft-json": "3.0.1"
```

## Google Play In-App Reviews
For Android in-app review this package uses the official **Google Play In-App Reviews plugin for Unity**:

https://github.com/google/play-in-app-reviews-unity

You must install this plugin before calling `InAppReviewController` on Android.

### Requirements
- **Unity**: 2019.x / 2020.x / newer
- **Android**: min. SDK **21 (Lollipop)** or higher

When you install the plugin, it will automatically bring in:
- **External Dependency Manager (EDM4U)**
- **Play Core plugin for Unity**
- **Play Common plugin for Unity**

You do not need to install these manually.

#### Option 1 - Install via OpenUPM
If you already use **OpenUPM** in your project:

1. Make sure OpenUPM is configured for your project.
2. In Unity, open **Window -> Package Manager**.
3. Switch the top-left filter to **My Registries**.
4. Find package **Google Play In-app Review** with id `com.google.play.review`.
5. Click **Install**.

#### Option 2 - Install via `.unitypackage`
If you do not want to set up OpenUPM:

1. Open the GitHub repo releases page.
2. Download the latest `com.google.play.review-*.unitypackage`.
3. In Unity, go to **Assets -> Import Package -> Custom Package...**.
4. Select the downloaded `.unitypackage` and click **Import**.
5. Keep everything checked.

## Auto-detector
- On import, you will get a dialog if something is missing.
- You can run it manually anytime from **ENP/Check Dependencies**.
- An option to disable future prompts is available.

## Versioning
This package follows Semantic Versioning: **MAJOR.MINOR.PATCH**.

## Troubleshooting
- **Missing DOTween types** - install DOTween.
- **UniRx not found** - install via OpenUPM and make sure the scoped registry is configured.

## Language System
This package includes a lightweight localization system built around `LanguageController`, `LanguageText` and `LanguageExtension`.

### How it works
- Each language has its own folder inside `Resources/Languages/`.
- Inside each folder you can place multiple JSON files.

Example structure:

```text
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
- If the same key appears in multiple JSON files for the same language, the last loaded file overwrites the value.
- This allows you to override specific keys by adding small JSON patches without touching the main files.

### Using localized text in UI
```csharp
tmpText.SetKey("menu.play");
tmpText.SetKey("score", points);
```

If you frequently update only the formatting parameters, you can bind the key once and then update only values without re-fetching the localized string:

```csharp
tmpText.SetKey("round.label", currentRound, maxRounds);
tmpText.UpdateValue(currentRound, maxRounds);
```

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

_Last updated: 2026-06-02_
