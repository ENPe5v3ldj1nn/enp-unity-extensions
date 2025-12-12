using System;

namespace enp_unity_extensions.Runtime.Scripts.Utilities
{
    /// <summary>
    /// Cached enum values and names to avoid allocations when enumerating.
    /// </summary>
    public static class EnumCache<T> where T : Enum
    {
        public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
        public static readonly string[] Names = Enum.GetNames(typeof(T));
    }
}
