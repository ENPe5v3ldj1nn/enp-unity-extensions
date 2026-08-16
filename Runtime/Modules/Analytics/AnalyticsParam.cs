namespace ENP.UnityExtensions.Analytics
{
    public readonly struct AnalyticsParam
    {
        public string Key { get; }
        public AnalyticsParamType Type { get; }
        public string StringValue { get; }
        public long LongValue { get; }
        public double DoubleValue { get; }

        private AnalyticsParam(string key, AnalyticsParamType type, string stringValue, long longValue,
            double doubleValue)
        {
            Key = key;
            Type = type;
            StringValue = stringValue;
            LongValue = longValue;
            DoubleValue = doubleValue;
        }

        public AnalyticsParam(string key, string value)
            : this(key, AnalyticsParamType.String, value ?? string.Empty, 0L, 0d)
        {
        }

        public AnalyticsParam(string key, long value)
            : this(key, AnalyticsParamType.Long, string.Empty, value, 0d)
        {
        }

        public AnalyticsParam(string key, int value)
            : this(key, (long)value)
        {
        }

        public AnalyticsParam(string key, bool value)
            : this(key, value ? 1L : 0L)
        {
        }

        public AnalyticsParam(string key, double value)
            : this(key, AnalyticsParamType.Double, string.Empty, 0L, value)
        {
        }

        public AnalyticsParam(string key, float value)
            : this(key, (double)value)
        {
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Key);
    }
}
