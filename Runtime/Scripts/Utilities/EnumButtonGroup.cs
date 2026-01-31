using System;
using System.Collections.Generic;
using enp_unity_extensions.Scripts.UI.Button;

namespace enp_unity_extensions.Runtime.Scripts.Utilities
{
    public sealed class EnumButtonGroup<TEnum> where TEnum : struct, Enum
    {
        private readonly (TEnum Value, AnimatedButtonWithRounded Button)[] _items;
        private readonly Dictionary<TEnum, int> _index;
        private readonly bool _strict;

        public EnumButtonGroup(bool strict, params (TEnum Value, AnimatedButtonWithRounded Button)[] items)
        {
            if (items == null || items.Length == 0) throw new ArgumentException(nameof(items));

            _strict = strict;
            _items = items;
            _index = new Dictionary<TEnum, int>(items.Length);

            for (int i = 0; i < items.Length; i++)
            {
                var v = items[i].Value;
                if (_index.ContainsKey(v)) throw new ArgumentException($"Duplicate mapping for {typeof(TEnum).Name}: {v}");
                _index.Add(v, i);
            }

            if (_strict)
            {
                var values = EnumCache<TEnum>.Values;
                for (int i = 0; i < values.Length; i++)
                {
                    if (!_index.ContainsKey(values[i]))
                        throw new ArgumentException($"Missing button mapping for {typeof(TEnum).Name}: {values[i]}");
                }
            }
        }

        public void Apply(TEnum activeValue)
        {
            if (_strict && !_index.ContainsKey(activeValue))
                throw new ArgumentException($"No button mapping for {typeof(TEnum).Name}: {activeValue}");

            for (int i = 0; i < _items.Length; i++)
            {
                var b = _items[i].Button;
                if (b == null) continue;

                if (EqualityComparer<TEnum>.Default.Equals(_items[i].Value, activeValue))
                    b.SetActive();
                else
                    b.SetInactive();
            }
        }
    }

}