using System;
using System.Collections.Generic;
using System.Reflection;
using NBox.Utils;

namespace NBox.Loader
{
    /// <summary>
    /// Provides access to loaded assemblies cache.
    /// </summary>
    internal static class AssembliesCachedRegistry
    {
        private static readonly Dictionary<string, Assembly> cachedAssembliesIdentifiers = new Dictionary<string, Assembly>();

        public static bool TryGetCachedAssembly(string assemblyID, out Assembly assembly) {
            ArgumentChecker.NotNullOrEmpty(assemblyID, "assemblyID");
            //
            if (cachedAssembliesIdentifiers.ContainsKey(assemblyID)) {
                assembly = cachedAssembliesIdentifiers[assemblyID];
                return (true);
            }
            //
            assembly = null;
            return (false);
        }

        public static void AddAssemblyToCache(string assemblyID, Assembly assembly) {
            ArgumentChecker.NotNull(assembly, "assembly");
            ArgumentChecker.NotNullOrEmpty(assemblyID, "assemblyID");
            //
            if (cachedAssembliesIdentifiers.ContainsKey(assemblyID)) {
                throw new InvalidOperationException("Assembly with this ID has been cached already.");
            }
            cachedAssembliesIdentifiers.Add(assemblyID, assembly);
        }
    }
}
