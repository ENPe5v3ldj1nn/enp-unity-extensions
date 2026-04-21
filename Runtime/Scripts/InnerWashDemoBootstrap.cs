using UnityEngine;
using UnityEngine.UI;

namespace ENP.ProceduralInnerWash.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ENP/Procedural Inner Wash Demo Bootstrap")]
    public sealed class InnerWashDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private Vector2 _cardSize = new Vector2(720f, 240f);
        [SerializeField] private float _verticalSpacing = 40f;
        [SerializeField] private Color _backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);
        [SerializeField] private Color _cardBackgroundColor = new Color(0.12f, 0.16f, 0.22f, 0.92f);
        [SerializeField] private bool _buildOnAwake;

        private const string CanvasRootName = "InnerWashDemoCanvas";
        private const string ShowcaseRootName = "InnerWashShowcase";

        private void Awake()
        {
            if (_buildOnAwake)
            {
                BuildDemo();
            }
        }

        [ContextMenu("Build Demo")]
        public void BuildDemo()
        {
            var canvasRoot = GetOrCreateCanvasRoot();
            BuildBackground(canvasRoot);
            BuildShowcase(canvasRoot);
        }

        [ContextMenu("Clear Demo")]
        public void ClearDemo()
        {
            var canvasRoot = transform.Find(CanvasRootName);

            if (canvasRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(canvasRoot.gameObject);
            }
            else
            {
                DestroyImmediate(canvasRoot.gameObject);
            }
        }

        private RectTransform GetOrCreateCanvasRoot()
        {
            var existing = transform.Find(CanvasRootName);

            if (existing != null)
            {
                return existing as RectTransform;
            }

            var canvasGo = new GameObject(CanvasRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvasTransform = (RectTransform)canvasGo.transform;
            canvasTransform.SetParent(transform, false);
            canvasTransform.anchorMin = Vector2.zero;
            canvasTransform.anchorMax = Vector2.one;
            canvasTransform.offsetMin = Vector2.zero;
            canvasTransform.offsetMax = Vector2.zero;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            return canvasTransform;
        }

        private void BuildBackground(RectTransform canvasRoot)
        {
            var background = GetOrCreateChild(canvasRoot, "Background", typeof(RectTransform), typeof(Image));
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var image = background.GetComponent<Image>();
            image.color = _backgroundColor;
            image.raycastTarget = false;
        }

        private void BuildShowcase(RectTransform canvasRoot)
        {
            var showcase = GetOrCreateChild(canvasRoot, ShowcaseRootName, typeof(RectTransform));
            var showcaseRect = (RectTransform)showcase.transform;
            showcaseRect.anchorMin = new Vector2(0.5f, 0.5f);
            showcaseRect.anchorMax = new Vector2(0.5f, 0.5f);
            showcaseRect.pivot = new Vector2(0.5f, 0.5f);
            showcaseRect.sizeDelta = new Vector2(_cardSize.x, _cardSize.y * 3f + _verticalSpacing * 2f);
            showcaseRect.anchoredPosition = Vector2.zero;

            BuildCard(showcaseRect, 0, "Neutral Ambient", AsymmetricInnerWashState.CreateNeutralAmbient());
            BuildCard(showcaseRect, 1, "Medium Ambient", AsymmetricInnerWashState.CreateMediumAmbient());
            BuildCard(showcaseRect, 2, "Warm Strong", AsymmetricInnerWashState.CreateWarmStrong());
        }

        private void BuildCard(RectTransform parent, int index, string title, AsymmetricInnerWashState state)
        {
            var card = GetOrCreateChild(parent, title, typeof(RectTransform), typeof(Image));
            var cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = _cardSize;
            cardRect.anchoredPosition = new Vector2(0f, ((_cardSize.y + _verticalSpacing) * (1 - index)));

            var cardImage = card.GetComponent<Image>();
            cardImage.color = _cardBackgroundColor;
            cardImage.raycastTarget = false;

            var washOverlay = GetOrCreateChild(cardRect, "Wash", typeof(RectTransform), typeof(RoundedRectAsymmetricInnerWashGraphic));
            var washRect = (RectTransform)washOverlay.transform;
            washRect.anchorMin = Vector2.zero;
            washRect.anchorMax = Vector2.one;
            washRect.offsetMin = Vector2.zero;
            washRect.offsetMax = Vector2.zero;

            var wash = washOverlay.GetComponent<RoundedRectAsymmetricInnerWashGraphic>();
            wash.color = Color.white;
            wash.raycastTarget = false;
            wash.SetState(state);

            BuildLabel(cardRect, "Title", title, new Vector2(24f, -20f), TextAnchor.UpperLeft, 34);
            BuildLabel(cardRect, "CenterText", "Readable center", Vector2.zero, TextAnchor.MiddleCenter, 28);
        }

        private void BuildLabel(RectTransform parent, string objectName, string textValue, Vector2 anchoredPosition, TextAnchor alignment, int fontSize)
        {
            var label = GetOrCreateChild(parent, objectName, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)label.transform;
            rect.anchorMin = alignment == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = alignment == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            rect.sizeDelta = alignment == TextAnchor.MiddleCenter ? new Vector2(400f, 60f) : new Vector2(420f, 50f);
            rect.anchoredPosition = anchoredPosition;

            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = textValue;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.color = new Color(0.93f, 0.95f, 0.98f, alignment == TextAnchor.MiddleCenter ? 0.88f : 0.94f);
            text.raycastTarget = false;
        }

        private static GameObject GetOrCreateChild(Transform parent, string childName, params System.Type[] components)
        {
            var child = parent.Find(childName);

            if (child != null)
            {
                return child.gameObject;
            }

            var childGo = new GameObject(childName, components);
            childGo.transform.SetParent(parent, false);
            return childGo;
        }
    }
}
