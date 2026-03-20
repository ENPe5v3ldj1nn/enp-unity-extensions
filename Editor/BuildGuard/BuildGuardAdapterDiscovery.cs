using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;

namespace BuildGuard.Editor
{
    public static class BuildGuardAdapterDiscovery
    {
        public static IReadOnlyList<IBuildGuardProjectAdapter> CreateAdapters()
        {
            return TypeCache.GetTypesDerivedFrom<IBuildGuardProjectAdapter>()
                .Where(IsSupportedAdapterType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(CreateAdapterInstance)
                .OrderBy(adapter => adapter.Order)
                .ToArray();
        }

        private static bool IsSupportedAdapterType(Type type)
        {
            return type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition && !type.ContainsGenericParameters;
        }

        private static IBuildGuardProjectAdapter CreateAdapterInstance(Type type)
        {
            var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (constructor == null)
                throw new BuildFailedException($"Build Guard adapter '{type.FullName}' must declare a public parameterless constructor.");

            try
            {
                return (IBuildGuardProjectAdapter)constructor.Invoke(null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new BuildFailedException($"Failed to create Build Guard adapter '{type.FullName}': {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                throw new BuildFailedException($"Failed to create Build Guard adapter '{type.FullName}': {ex.Message}");
            }
        }
    }
}
