using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [CreateAssetMenu(fileName = "WindowConfig", menuName = "ENP/UI/Window Config")]
    public class WindowConfig : ScriptableObject
    {
        public static WindowConfig Default { get; set; }

        [Serializable]
        public struct Recipe
        {
            public float duration;
            public float delay;
            public Ease ease;
            [Tooltip("Optional. When it has keys it overrides Ease.")]
            public AnimationCurve curve;

            [Tooltip("Hidden-state offset from base, as a fraction of the window's own rect size (1 = one full width/height). Enter: offset->base. Exit: base->offset.")]
            public Vector2 offset;
            [Tooltip("Hidden-state local scale. Zero = keep base scale (no scaling).")]
            public Vector3 scale;
            [Tooltip("Hidden-state extra Z rotation (degrees). 0 = none.")]
            public float rotation;
            [Range(0f, 1f)]
            [Tooltip("Hidden-state alpha. Shown alpha is always 1.")]
            public float alpha;
        }

        [Serializable]
        public struct Entry
        {
            public WindowTransition transition;
            public Recipe enter;
            public Recipe exit;
        }

        [SerializeField] private Entry[] _entries;

        private Dictionary<WindowTransition, Entry> _lookup;

        public Entry Get(WindowTransition transition)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<WindowTransition, Entry>(_entries.Length);
                for (int i = 0; i < _entries.Length; i++)
                    _lookup[_entries[i].transition] = _entries[i];
            }

            if (!_lookup.TryGetValue(transition, out var entry))
                throw new KeyNotFoundException($"{name}: no recipe for '{transition}'. Add it to the config.");

            return entry;
        }

        // Tuned for high-frequency UI navigation (lots of screen switches): short durations, no
        // heavy overshoot. Slower/bouncier presets read fine once; at high frequency they read as lag.
        private void Reset()
        {
            const float slide = 1f; // one full width of the window's own rect
            const float popupRise = 0.06f; // small fraction of the window's own rect height
            var one = Vector3.one;

            _entries = new[]
            {
                E(WindowTransition.Fade,
                    enter: R(0.16f, Ease.OutQuad, Vector2.zero, one, 0f, 0f),
                    exit:  R(0.13f, Ease.InQuad,  Vector2.zero, one, 0f, 0f)),

                E(WindowTransition.SlideLeft,
                    enter: R(0.18f, Ease.OutQuad, new Vector2(slide, 0),  one, 0f, 0f),
                    exit:  R(0.14f, Ease.InQuad,  new Vector2(-slide, 0), one, 0f, 0f)),

                E(WindowTransition.SlideRight,
                    enter: R(0.18f, Ease.OutQuad, new Vector2(-slide, 0), one, 0f, 0f),
                    exit:  R(0.14f, Ease.InQuad,  new Vector2(slide, 0),  one, 0f, 0f)),

                E(WindowTransition.SmoothLeft,
                    enter: R(0.22f, Ease.OutBack, new Vector2(-slide, 0), new Vector3(0.99f, 0.99f, 1f), 0f, 0f),
                    exit:  R(0.18f, Ease.InBack,  new Vector2(slide, 0),  new Vector3(0.99f, 0.99f, 1f), 0f, 0f)),

                E(WindowTransition.SmoothRight,
                    enter: R(0.22f, Ease.OutBack, new Vector2(slide, 0),  new Vector3(0.99f, 0.99f, 1f), 0f, 0f),
                    exit:  R(0.18f, Ease.InBack,  new Vector2(-slide, 0), new Vector3(0.99f, 0.99f, 1f), 0f, 0f)),

                E(WindowTransition.PopupCard,
                    enter: R(0.20f, Ease.OutBack, new Vector2(0, -popupRise), new Vector3(0.94f, 0.94f, 1f), 0f, 0f),
                    exit:  R(0.16f, Ease.InBack,  new Vector2(0, -popupRise), new Vector3(0.94f, 0.94f, 1f), 0f, 0f)),
            };
        }

        private static Entry E(WindowTransition transition, Recipe enter, Recipe exit) =>
            new() { transition = transition, enter = enter, exit = exit };

        private static Recipe R(float duration, Ease ease, Vector2 offset, Vector3 scale, float rotation, float alpha) =>
            new()
            {
                duration = duration,
                delay = 0f,
                ease = ease,
                curve = null,
                offset = offset,
                scale = scale,
                rotation = rotation,
                alpha = alpha
            };
    }
}
