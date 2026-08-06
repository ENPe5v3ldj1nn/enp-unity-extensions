using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Resolves TMP rich-text &lt;font=Name&gt; tags against Addressable font assets.
    /// TMP's own tag handler falls back to Resources.Load when a font isn't already
    /// cached in its MaterialReferenceManager; projects that dropped Resources/ in favor
    /// of Addressables need this hook (TMP_Text.OnFontAssetRequest) instead.
    /// </summary>
    public sealed class TmpFontRegistrationService : IDisposable
    {
        private readonly string _addressPrefix;
        private readonly Dictionary<string, TMP_FontAsset> _cache = new Dictionary<string, TMP_FontAsset>();
        private readonly List<AsyncOperationHandle<TMP_FontAsset>> _handles = new List<AsyncOperationHandle<TMP_FontAsset>>();
        private bool _subscribed;

        /// <param name="addressPrefix">
        /// Prepended to the tag name to build the Addressable address, e.g. "Fonts/" turns
        /// &lt;font=Nunito-Bold&gt; into a lookup for "Fonts/Nunito-Bold" (mirrors the PopupController convention).
        /// </param>
        public TmpFontRegistrationService(string addressPrefix)
        {
            _addressPrefix = addressPrefix;
        }

        public void Initialize()
        {
            if (_subscribed)
                return;

            _subscribed = true;
            TMP_Text.OnFontAssetRequest += OnFontAssetRequest;
        }

        public void Dispose()
        {
            if (!_subscribed)
                return;

            _subscribed = false;
            TMP_Text.OnFontAssetRequest -= OnFontAssetRequest;

            foreach (var handle in _handles)
                Addressables.Release(handle);

            _handles.Clear();
            _cache.Clear();
        }

        private TMP_FontAsset OnFontAssetRequest(int hash, string name)
        {
            if (_cache.TryGetValue(name, out var cached))
                return cached;

            var handle = Addressables.LoadAssetAsync<TMP_FontAsset>(_addressPrefix + name);
            _handles.Add(handle);
            var fontAsset = handle.WaitForCompletion();

            if (fontAsset == null)
                return null;

            _cache[name] = fontAsset;
            return fontAsset;
        }
    }
}
