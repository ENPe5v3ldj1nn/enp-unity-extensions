using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Data-driven enter/exit recipes for <see cref="AnimatedWindow"/>, keyed by
    /// <see cref="AnimatedWindowAnimation"/>. Replaces the Animator/AnimationClip based
    /// motion. Values are seeded from the legacy .anim clips; tune per project in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "WindowAnimationConfig", menuName = "ENP/UI/Window Animation Config")]
    public class WindowAnimationConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public AnimatedWindowAnimation anim;

            [Tooltip("Anchored-position offset (relative to base) at the START of the animation.")]
            public Vector2 startOffset;

            [Tooltip("Anchored-position offset (relative to base) at the END of the animation.")]
            public Vector2 endOffset;

            [Range(0f, 1f)] public float fromAlpha;
            [Range(0f, 1f)] public float toAlpha;

            public float duration;
            public Ease ease;
        }

        [SerializeField] private Entry[] _entries;

        private Dictionary<AnimatedWindowAnimation, Entry> _lookup;

        public Entry Get(AnimatedWindowAnimation anim)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<AnimatedWindowAnimation, Entry>(_entries.Length);
                for (int i = 0; i < _entries.Length; i++)
                    _lookup[_entries[i].anim] = _entries[i];
            }

            if (!_lookup.TryGetValue(anim, out var entry))
                throw new KeyNotFoundException($"{name}: no recipe for '{anim}'. Add it to the config.");

            return entry;
        }

        // Seeds defaults matching the legacy .anim clips when the asset is created.
        private void Reset()
        {
            const float slide = 1472f;

            _entries = new[]
            {
                // --- Open (offset -> base, fade in) ---
                Recipe(AnimatedWindowAnimation.OpenLeft,   new Vector2(slide, 0),  Vector2.zero, 0f, 1f, 0.30f, Ease.OutQuad),
                Recipe(AnimatedWindowAnimation.OpenRight,  new Vector2(-slide, 0), Vector2.zero, 0f, 1f, 0.30f, Ease.OutQuad),
                Recipe(AnimatedWindowAnimation.OpenMiddle, Vector2.zero,           Vector2.zero, 0f, 1f, 0.28f, Ease.OutQuad),
                Recipe(AnimatedWindowAnimation.OpenSmoothLeft,  new Vector2(-slide, 0), Vector2.zero, 0f, 1f, 0.40f, Ease.OutBack),
                Recipe(AnimatedWindowAnimation.OpenSmoothRight, new Vector2(slide, 0),  Vector2.zero, 0f, 1f, 0.40f, Ease.OutBack),
                Recipe(AnimatedWindowAnimation.OpenPopupCard,   new Vector2(0, -180),   Vector2.zero, 0f, 1f, 0.38f, Ease.OutBack),

                // --- Close (base -> offset, fade out) ---
                Recipe(AnimatedWindowAnimation.CloseLeft,   Vector2.zero, new Vector2(-slide, 0), 1f, 0f, 0.25f, Ease.InQuad),
                Recipe(AnimatedWindowAnimation.CloseRight,  Vector2.zero, new Vector2(slide, 0),  1f, 0f, 0.25f, Ease.InQuad),
                Recipe(AnimatedWindowAnimation.CloseMiddle, Vector2.zero, Vector2.zero,           1f, 0f, 0.25f, Ease.InQuad),
                Recipe(AnimatedWindowAnimation.CloseSmoothLeft,  Vector2.zero, new Vector2(slide, 0),  1f, 0f, 0.35f, Ease.InBack),
                Recipe(AnimatedWindowAnimation.CloseSmoothRight, Vector2.zero, new Vector2(-slide, 0), 1f, 0f, 0.35f, Ease.InBack),
                Recipe(AnimatedWindowAnimation.ClosePopupCard,   Vector2.zero, new Vector2(0, -180),   1f, 0f, 0.30f, Ease.InBack),
            };
        }

        private static Entry Recipe(AnimatedWindowAnimation anim, Vector2 startOffset, Vector2 endOffset,
            float fromAlpha, float toAlpha, float duration, Ease ease)
        {
            return new Entry
            {
                anim = anim,
                startOffset = startOffset,
                endOffset = endOffset,
                fromAlpha = fromAlpha,
                toAlpha = toAlpha,
                duration = duration,
                ease = ease
            };
        }
    }
}
