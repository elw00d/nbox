using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using NBox.Config;
using NBox.Utils;

namespace NBox.Loader
{
    internal class Loader
    {
        private static readonly BuildConfiguration configuration;

        private static readonly string mainAssemblyDirectoryName;

        private static readonly string system32DirectoryName;

        private static readonly Dictionary<string, IncludedAssemblyConfig> assembliesByAliases =
            new Dictionary<string, IncludedAssemblyConfig>();

        static Loader() {
            try {
                // Configuration initialization
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                Stream configResourceStream = executingAssembly.GetManifestResourceStream("attached-configuration.xml");
                if (configResourceStream == null) {
                    configResourceStream = executingAssembly.GetManifestResourceStream(executingAssembly.GetName().Name + ".attached-configuration.xml");
                    if (configResourceStream == null) {
                        throw new InvalidOperationException("Cannot find attached configuration.");
                    }
                }
                using (configResourceStream) {
                    XmlDocument document = new XmlDocument();
                    document.Load(configResourceStream);
                    configuration = new BuildConfiguration(document, new BuildConfigurationVariables());
                }
                // Initialization of variables
                mainAssemblyDirectoryName = Path.GetDirectoryName(executingAssembly.Location);
                system32DirectoryName = Environment.SystemDirectory;
                // Initialization dictionary of aliases
                IList<IncludedObjectConfigBase> includedObjects = configuration.IncludedObjects;
                includedObjects.Add(configuration.MainAssembly);
                //
                foreach (IncludedObjectConfigBase configBase in includedObjects) {
                    if (configBase is IncludedAssemblyConfig) {
                        foreach (string alias in (configBase as IncludedAssemblyConfig).Aliases) {
                            if (assembliesByAliases.ContainsKey(alias)) {
                                throw new InvalidOperationException("Assembly aliases conflict.");
                            }
                            assembliesByAliases.Add(alias, (IncludedAssemblyConfig) configBase);
                        }
                    }
                }
            } catch (Exception exc) {
                File.AppendAllText("rolling-fatal.log", exc.ToString());
                throw;
            }
        }

        static int Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            //
            try {
                // Initial extracting files
                foreach (IncludedObjectConfigBase configBase in configuration.IncludedObjects) {
                    if (configBase is IncludedFileConfig) {
                        IncludedFileConfig fileConfig = configBase as IncludedFileConfig;
                        string configuredExtractingPath = configurePath(fileConfig.ExtractToPath);
                        //
                        long decodedDataLength = 0;
                        byte[] bytes = null;
                        //
                        bool extracting = true;
                        if (fileConfig.OverwriteOnExtract == OverwritingOptions.CheckExist) {
                            extracting = !File.Exists(configuredExtractingPath);
                        } else if (fileConfig.OverwriteOnExtract == OverwritingOptions.CheckSize) {
                            bytes = loadRawData(fileConfig, out decodedDataLength);
                            extracting = !File.Exists(configuredExtractingPath) || new FileInfo(configuredExtractingPath).Length != bytes.Length;
                        }
                        //
                        if (extracting) {
                            if (bytes == null) {
                                bytes = loadRawData(fileConfig, out decodedDataLength);
                            }
                            //
                            using (FileStream stream = File.Create(configuredExtractingPath)) {
                                stream.Write(bytes, 0, unchecked((int) decodedDataLength));
                            }
                        }
                    }
                }
                // Initial loading assemblies with lazy-load = false attribute
                foreach (IncludedObjectConfigBase configBase in configuration.IncludedObjects) {
                    if (configBase is IncludedAssemblyConfig) {
                        IncludedAssemblyConfig assemblyConfig = configBase as IncludedAssemblyConfig;
                        if (!assemblyConfig.LazyLoad) {
                            loadAssembly(assemblyConfig);
                        }
                    }
                }
                // Starting application
                Assembly assembly = loadAssembly(configuration.MainAssembly);
                if (assembly == null) {
                    throw new InvalidOperationException("Cannot load main module.");
                }
                MethodInfo entryPoint = assembly.EntryPoint;
                if (entryPoint != null) {
                    ParameterInfo[] parameters = entryPoint.GetParameters();
                    //
                    int length = parameters.Length;
                    parametersArr = new object[length];
                    if (parametersArr.Length > 0) {
                        parametersArr[0] = args;
                    }
                    // Starting thread with selected ThreadApartment
                    Thread threadWithApartment = new Thread(start);
                    threadWithApartment.IsBackground = false;
                    threadWithApartment.SetApartmentState(configuration.OutputApartmentState);
                    threadWithApartment.Start(entryPoint);
                    threadWithApartment.Join();
                }
            } catch (Exception exc) {
                File.AppendAllText("rolling-fatal.log", exc.ToString());
                throw;
            }
            //
            return (returnedValue != null) && (returnedValue is int) ? (int) returnedValue : (0);
        }

        private static object returnedValue = null;

        private static void start(object o) {
            try {
                returnedValue = ((MethodInfo) o).Invoke(null, parametersArr);
            } catch (Exception exc) {
                File.AppendAllText("rolling-fatal.log", exc.ToString());
                throw;
            }
        }

        private static int inititalOverlayOffset = 0;
        
        private static object[] parametersArr;

        private static byte[] loadRawData(IncludedObjectConfigBase configBase, out long rawDataLength) {
            byte[] bytes;
            //
            switch (configBase.IncludeMethod.IncludeMethodKind) {
                case IncludeMethodKind.File: {
                    string fullPath = configurePath(configBase.IncludeMethod.FileLoadFromPath);
                    bytes = File.ReadAllBytes(fullPath);
                    break;
                }
                case IncludeMethodKind.Overlay: {
                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    if (executingAssembly.Location == null) {
                        throw new InvalidOperationException("Cannot get location of executing assembly.");
                    }
                    //
                    using (FileStream stream = File.OpenRead(executingAssembly.Location)) {
                        if (inititalOverlayOffset == 0) {
                            stream.Seek(-4, SeekOrigin.End);
                            for (int i = 0; i < 3; i++) {
                                inititalOverlayOffset |= (stream.ReadByte() << (8 * (3 - i)));
                            }
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                        //
                        stream.Seek(inititalOverlayOffset + configBase.IncludeMethod.OverlayOffset, SeekOrigin.Begin);
                        bytes = new byte[configBase.IncludeMethod.OverlayLength];
                        if (stream.Read(bytes, 0, bytes.Length) != configBase.IncludeMethod.OverlayLength) {
                            throw new InvalidOperationException("Cannot read overlay. Image may be corrupted.");
                        }
                    }
                    break;
                }
                case IncludeMethodKind.Resource: {
                    string resourceName = configBase.IncludeMethod.ResourceName;
                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    Stream stream = executingAssembly.GetManifestResourceStream(resourceName);
                    if (stream == null) {
                        stream = executingAssembly.GetManifestResourceStream(executingAssembly.GetName().Name + "." + resourceName);
                        if (stream == null) {
                            throw new InvalidOperationException("Cannot load resource by name.");
                        }
                    }
                    using (stream) {
                        bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, unchecked((int) stream.Length));
                    }
                    break;
                }
                default: {
                    throw new InvalidOperationException("Unknown type.");
                }
            }
            return (LzmaHelper.Decode(bytes, bytes.LongLength, out rawDataLength));
        }

        private static Assembly loadAssembly(IncludedObjectConfigBase assemblyConfig) {
            Assembly alreadyCachedAssembly;
            if (AssembliesCachedRegistry.TryGetCachedAssembly(assemblyConfig.ID, out alreadyCachedAssembly)) {
                return (alreadyCachedAssembly);
            }
            //
            long decodedDataLength;
            Assembly assembly = Assembly.Load(loadRawData(assemblyConfig, out decodedDataLength));
            //
            AssembliesCachedRegistry.AddAssemblyToCache(assemblyConfig.ID, assembly);
            return (assembly);
        }

        private static string configurePath(string configurablePath) {
            configurablePath = configurablePath.Replace('/', Path.DirectorySeparatorChar);
            if (configurablePath.Contains("%mainassemblydir%")) {
                string endOfPath = configurablePath.Replace("%mainassemblydir%", String.Empty);
                if (!String.IsNullOrEmpty(endOfPath) && endOfPath[0] == Path.DirectorySeparatorChar) {
                    endOfPath = endOfPath.Substring(1);
                }
                if (!String.IsNullOrEmpty(endOfPath)) {
                    return (Path.Combine(mainAssemblyDirectoryName, endOfPath));
                }
                return (mainAssemblyDirectoryName);
            }
            if (configurablePath.Contains("%system32dir%")) {
                string endOfPath = configurablePath.Replace("%system32dir%", String.Empty);
                if (!String.IsNullOrEmpty(endOfPath) && endOfPath[0] == Path.DirectorySeparatorChar) {
                    endOfPath = endOfPath.Substring(1);
                }
                if (!String.IsNullOrEmpty(endOfPath)) {
                    return (Path.Combine(system32DirectoryName, endOfPath));
                }
                return (system32DirectoryName);
            }
            return (Path.GetFullPath(configurablePath));
        }

        private static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args) {
            //
            try {
                IncludedAssemblyConfig config;
                if (assembliesByAliases.TryGetValue(args.Name, out config)) {
                    Assembly assembly = loadAssembly(config);
                    return (assembly);
                }
                //
                return (null);
            } catch (Exception exc) {
                File.AppendAllText("rolling-error.log", exc.ToString());
                //
                return (null);
            }
        }
    }
}
