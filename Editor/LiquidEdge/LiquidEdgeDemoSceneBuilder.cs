using enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace enp_unity_extensions.Editor.LiquidEdge
{
    public static class LiquidEdgeDemoSceneBuilder
    {
        public static void CreateDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Liquid Edge Demo";

            GameObject canvasObject = new GameObject("Liquid Edge Demo Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create Liquid Edge Demo");

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            CreateBackground(root);

            LivingInnerWashMotionController controller = CreateLiquidCard(root);

            Selection.activeGameObject = controller.gameObject;
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Liquid Edge Demo");
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void CreateBackground(RectTransform parent)
        {
            GameObject background = CreateUiObject("Dark Background", parent);
            RectTransform rect = background.GetComponent<RectTransform>();
            Stretch(rect);

            Image image = background.AddComponent<Image>();
            image.color = new Color(0.015f, 0.016f, 0.024f, 1f);
            image.raycastTarget = false;
        }

        private static LivingInnerWashMotionController CreateLiquidCard(RectTransform parent)
        {
            GameObject area = CreateUiObject("Rounded Rectangle Living Inner Wash", parent);
            RectTransform rect = area.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 70f);
            rect.sizeDelta = new Vector2(820f, 430f);

            RoundedRectLivingInnerWashGraphic graphic = area.AddComponent<RoundedRectLivingInnerWashGraphic>();
            graphic.raycastTarget = false;
            graphic.color = Color.white;

            return area.AddComponent<LivingInnerWashMotionController>();
        }

        private static GameObject CreateUiObject(string name, RectTransform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return obj;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
