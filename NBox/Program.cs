using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Common.Logging;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.CSharp;
using NBox.Config;
using NBox.Utils;

namespace NBox
{
    class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));

        private static bool addStringToTheListIfNotAlready(ICollection<string> strList, string @string) {
            foreach (string s in strList) {
                if (s == @string) {
                    return (false);
                }
            }
            //
            strList.Add(@string);
            return (true);
        }

        static void _Main(string[] args) {
            if (args.Length == 0) {
                printUsage();
                return;
            }

            string configFilePath = args[0];
            if (!File.Exists(args[0])) {
                if (logger.IsErrorEnabled) {
                    logger.Error(String.Format("File {0} was not found.", configFilePath));
                }
                //
                printFailureMessage();
                return;
            }

            // Reading configuration
            BuildConfiguration configuration;
            try {
                configuration = new BuildConfiguration(Path.GetFullPath(Path.GetFullPath(configFilePath)));
            } catch (XmlException exc) {
                if (logger.IsErrorEnabled) {
                    logger.Error(String.Format("XML configuration is not correct. Message : {0}", exc.Message), exc);
                }
                printFailureMessage();
                return;
            } catch (InvalidOperationException exc) {
                if (logger.IsErrorEnabled) {
                    logger.Error(String.Format("Error during loading configuration. Message : {0}.", exc.Message), exc);
                }
                printFailureMessage();
                return;
            }

            Dictionary<IncludedObjectConfigBase, string> includedObjectsPackedFiles = new Dictionary<IncludedObjectConfigBase, string>();

            // Create temp directory for store packed files and generated attached configuration
            string tempBaseDirectoryName = Path.Combine(configuration.Variables.ConfigFileDirectory, "Temp");
            string tempDirectoryName = Path.Combine(tempBaseDirectoryName, Guid.NewGuid().ToString());
            try {
                if (!Directory.Exists(tempBaseDirectoryName)) {
                    Directory.CreateDirectory(tempBaseDirectoryName);
                }
                //
                Directory.CreateDirectory(tempDirectoryName);
            } catch (IOException exc) {
                if (logger.IsErrorEnabled) {
                    logger.Error(String.Format("Cannot create temporary directory. Message : {0}.", exc.Message), exc);
                }
                printFailureMessage();
                return;
            }

            // Packing the data to the temp directory
            IList<IncludedObjectConfigBase> includedObjects = new List<IncludedObjectConfigBase>(configuration.IncludedObjects);
            includedObjects.Add(configuration.MainAssembly);
            try {
                foreach (IncludedObjectConfigBase configBase in includedObjects) {
                    string sourceFilePath = configurePathByConfigurationVariables(configBase.Path, configuration);
                    //
                    string packedFilePath = Path.Combine(tempDirectoryName, Guid.NewGuid() + ".packed");
                    //
                    if (logger.IsInfoEnabled) {
                        logger.Info(String.Format("Reading {0} file.", sourceFilePath));
                    }
                    byte[] bytes = File.ReadAllBytes(sourceFilePath);
                    //
                    if (logger.IsInfoEnabled) {
                        logger.Info(String.Format("Readed {0} bytes.", bytes.Length));
                    }
                    //
                    if (logger.IsInfoEnabled) {
                        logger.Info(String.Format("Compressing file and storing to {0}.", packedFilePath));
                    }
                    //
                    long encodedLength;
                    byte[] compressed = LzmaHelper.Encode(configBase.CompressionConfig, bytes, bytes.LongLength, out encodedLength);
                    using (FileStream fileStream = File.OpenWrite(packedFilePath)) {
                        fileStream.Write(compressed, 0, unchecked((int) encodedLength));
                    }
                    //
                    if (logger.IsInfoEnabled) {
                        logger.Info(String.Format("Compressing OK. Compressed to {0} bytes.", encodedLength));
                    }
                    //
                    includedObjectsPackedFiles.Add(configBase, packedFilePath);
                }
            } catch (IOException exc) {
                if (logger.IsErrorEnabled) {
                    logger.Error(String.Format("Error during packing source files. Message : {0}.", exc.Message), exc);
                }
                printFailureMessage();
                return;
            }

            // Write actual values of resource names and overlay placement
            List<IncludedObjectConfigBase> overlaysOrdered = new List<IncludedObjectConfigBase>();
            foreach (IncludedObjectConfigBase configBase in includedObjects) {
                if (configBase.IncludeMethod.IncludeMethodKind == IncludeMethodKind.Resource) {
                    configBase.IncludeMethod.ResourceName = Path.GetFileName(includedObjectsPackedFiles[configBase]);
                    //
                } else if (configBase.IncludeMethod.IncludeMethodKind == IncludeMethodKind.Overlay) {
                    configBase.IncludeMethod.OverlayOffset =
                        0 != overlaysOrdered.Count ? (overlaysOrdered[overlaysOrdered.Count - 1].IncludeMethod.OverlayOffset + overlaysOrdered[overlaysOrdered.Count - 1].IncludeMethod.OverlayLength)
                            : 0;
                    try {
                        configBase.IncludeMethod.OverlayLength = unchecked((int) new FileInfo(includedObjectsPackedFiles[configBase]).Length);
                    } catch (IOException exc) {
                        if (logger.IsErrorEnabled) {
                            logger.Error(String.Format("Error while calculating overlays placement. Message : {0}.", exc.Message), exc);
                        }
                        printFailureMessage();
                        return;
                    }
                    //
                    overlaysOrdered.Add(configBase);
                }
            }

            List<string> resourcesReflectedPaths = new List<string>();

            // Get assemblies full names and put it into aliases
            AppDomain tempAppDomain = null;
            try {
                if (logger.IsTraceEnabled) {
                    logger.Trace("Creating temporary AppDomain for resolving assemblies names.");
                }
                tempAppDomain = AppDomain.CreateDomain("tempAppDomain");
                //
                foreach (IncludedObjectConfigBase configBase in includedObjects) {
                    if (configBase is IncludedAssemblyConfig) {
                        string configuredAssemblyPath = configurePathByConfigurationVariables(configBase.Path, configuration);
                        //
                        if (logger.IsTraceEnabled) {
                            logger.Trace(String.Format("Loading {0} assembly.", configuredAssemblyPath));
                        }
                        Assembly assembly = Assembly.ReflectionOnlyLoadFrom(configuredAssemblyPath);

                        if (configBase == configuration.MainAssembly) {
                            string[] manifestResourceNames = assembly.GetManifestResourceNames();
                            foreach (string resourceName in manifestResourceNames) {
                                string resourceFilePath = Path.Combine(tempDirectoryName, resourceName.Replace(assembly.GetName().Name, Path.GetFileNameWithoutExtension(configuration.OutputPath)));
                                //
                                if (File.Exists(resourceFilePath)) {
                                    throw new InvalidOperationException(String.Format("File for resource with name {0} already exists. May be resources naming conflict ?", resourceName));
                                }
                                //
                                using (FileStream fileStream = File.Create(resourceFilePath)) {
                                    using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                                        if (stream == null) {
                                            throw new InvalidOperationException("Cannot read one of manifest resource stream from assembly.");
                                        }
                                        const int bufferSize = 32*1024;
                                        byte[] buffer = new byte[bufferSize];
                                        int totalBytesReaded = 0;
                                        while (stream.CanRead && (totalBytesReaded < stream.Length)) {
                                            int readedBytes = stream.Read(buffer, 0, bufferSize);
                                            fileStream.Write(buffer, 0, readedBytes);
                                            totalBytesReaded += readedBytes;
                                        }
                                    }
                                }
                                //
                                resourcesReflectedPaths.Add(resourceFilePath);
                            }
                        }

                        string fullName = assembly.FullName;
                        if (fullName == null) {
                            throw new InvalidOperationException("Cannot resolve full name of assembly.");
                        }
                        IncludedAssemblyConfig assemblyConfig = ((IncludedAssemblyConfig) configBase);
                        //
                        if (addStringToTheListIfNotAlready(assemblyConfig.Aliases, fullName)) {
                            if (logger.IsTraceEnabled) {
                                logger.Trace(String.Format("Added full name to aliases : '{0}'.", fullName));
                            }
                        }
                        //
                        if (assemblyConfig.GeneratePartiallyAliases) {
                            string[] fullNameParts = fullName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            //
                            string partiallyName = fullNameParts[0];
                            for (int i = 1; i < fullNameParts.Length; i++) {
                                if (addStringToTheListIfNotAlready(assemblyConfig.Aliases, partiallyName)) {
                                    if (logger.IsTraceEnabled) {
                                        logger.Trace(String.Format("Added partially name to aliases : '{0}'.", partiallyName));
                                    }
                                }
                                partiallyName = partiallyName + "," + fullNameParts[i];
                            }
                        }
                    }
                }
            } catch (BadImageFormatException exc) {
                if (logger.IsErrorEnabled) {
                    logger.Error(String.Format("Error while assemblies names resolving. Message : {0}.", exc.Message), exc);
                }
                //
                printFailureMessage();
                return;
            } finally {
                if (tempAppDomain != null) {
                    if (logger.IsTraceEnabled) {
                        logger.Trace("Unloading temporary AppDomain.");
                    }
                    AppDomain.Unload(tempAppDomain);
                }
            }

            // Saving configuration
            XmlDocument document = configuration.ExportConfigurationXML();
            try {
                document.Save(Path.Combine(tempDirectoryName, "attached-configuration.xml"));
            } catch (IOException exc) {
                if (logger.IsErrorEnabled) {
                    logger.Error(String.Format("Cannot write attached-configuration.xml file. Message : {0}.", exc.Message), exc);
                }
                //
                printFailureMessage();
                return;
            }

            // Compiling a Loader

            IDictionary<string, string> providerOptions = new Dictionary<string, string>();
            //providerOptions.Add("CompilerVersion", "v3.5");

            CSharpCodeProvider codeProvider = new CSharpCodeProvider(providerOptions);
            CompilerParameters compilerParameters = new CompilerParameters();
            compilerParameters.ReferencedAssemblies.Add("System.dll");
            compilerParameters.ReferencedAssemblies.Add("System.Xml.dll");
            compilerParameters.ReferencedAssemblies.Add("System.Data.dll");

            string outputAssemblyPath = configurePathByConfigurationVariables(configuration.OutputPath, configuration);
            compilerParameters.OutputAssembly = outputAssemblyPath;

            //
            foreach (IncludedObjectConfigBase configBase in includedObjects) {
                if (configBase.IncludeMethod.IncludeMethodKind == IncludeMethodKind.Resource) {
                    compilerParameters.EmbeddedResources.Add(includedObjectsPackedFiles[configBase]);
                }
            }
            compilerParameters.EmbeddedResources.Add(Path.Combine(tempDirectoryName, "attached-configuration.xml"));
            //
            compilerParameters.CompilerOptions = configuration.OutputAppType == OutputAppType.Console ? "/target:exe" : "/target:winexe";

            string platformOption = null;
            switch (configuration.OutputMachine) {
                case OutputMachine.Any: {
                    platformOption = " /platform:anycpu";
                    break;
                }
                case OutputMachine.x86: {
                    platformOption = " /platform:x86";
                    break;
                }
                case OutputMachine.x64: {
                    platformOption = " /platform:x64";
                    break;
                }
                case OutputMachine.Itanium: {
                    platformOption = " /platform:Itanium";
                    break;
                }
            }
            compilerParameters.CompilerOptions = compilerParameters.CompilerOptions + " " + platformOption;

            compilerParameters.CompilerOptions = compilerParameters.CompilerOptions + " " + "/define:LOADER";

            if (!String.IsNullOrEmpty(configuration.OutputWin32IconPath)) {
                string win32iconOption = String.Format("/win32icon:\"{0}\"", configurePathByConfigurationVariables(configuration.OutputWin32IconPath, configuration));
                compilerParameters.CompilerOptions = compilerParameters.CompilerOptions + " " + win32iconOption;
            }

            foreach (string resourceReflectedPath in resourcesReflectedPaths) {
                compilerParameters.EmbeddedResources.Add(resourceReflectedPath);
            }

#if _BUILD_FROM_FILES
            compilerParameters.EmbeddedResources.Add(@"..\..\..\NBox.Loader\config-file.xsd");

            CompilerResults compilerResults = codeProvider.CompileAssemblyFromFile(compilerParameters,
                @"..\..\..\NBox.Loader\Loader.cs",
                @"..\..\..\NBox.Loader\AssembliesCachedRegistry.cs",
                @"..\..\..\NBox.Loader\Utils\Annotations.cs",
                @"..\..\..\NBox.Loader\Utils\ArgumentChecker.cs",
                @"..\..\..\NBox.Loader\Utils\LzmaHelper.cs",
                @"..\..\..\NBox.Loader\Config\BuildConfiguration.cs",
                @"..\..\..\NBox.Loader\Config\BuildConfigurationVariables.cs",
                @"..\..\..\NBox.Loader\Config\CompressionConfig.cs",
                @"..\..\..\NBox.Loader\Config\IncludedAssemblyConfig.cs",
                @"..\..\..\NBox.Loader\Config\IncludedFileConfig.cs",
                @"..\..\..\NBox.Loader\Config\IncludedObjectConfigBase.cs",
                @"..\..\..\NBox.Loader\Config\IncludeMethod.cs",
                @"..\..\..\NBox.Loader\Config\ISerializableToXmlNode.cs",
                @"..\..\..\NBox.Loader\Lzma\ICoder.cs",
                @"..\..\..\NBox.Loader\Lzma\Common\CommandLineParser.cs",
                @"..\..\..\NBox.Loader\Lzma\Common\CRC.cs",
                @"..\..\..\NBox.Loader\Lzma\Common\InBuffer.cs",
                @"..\..\..\NBox.Loader\Lzma\Common\OutBuffer.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\LZ\IMatchFinder.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\LZ\LzBinTree.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\LZ\LzInWindow.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\LZ\LzOutWindow.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\LZMA\LzmaBase.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\LZMA\LzmaEncoder.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\LZMA\LzmaDecoder.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\RangeCoder\RangeCoder.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\RangeCoder\RangeCoderBit.cs",
                @"..\..\..\NBox.Loader\Lzma\Compress\RangeCoder\RangeCoderBitTree.cs"
                );
#else
            loadLoaderSourcesFromZip();

            foreach (EmbeddedResourceInfo embeddedResource in embeddedResources) {
                string[] zipEntryPathParts = embeddedResource.ZipEntryName.Split('/');
                string tempFileName = zipEntryPathParts[zipEntryPathParts.Length - 1];
                string tempFilePath = Path.Combine(tempDirectoryName, tempFileName);
                try {
                    using (FileStream fileStream = File.Create(tempFilePath)) {
                        fileStream.Write(embeddedResource.Data, 0, embeddedResource.Data.Length);
                    }
                } catch (IOException exc) {
                    if (logger.IsErrorEnabled) {
                        logger.Error(String.Format("Error while writing temporary file {0}.", tempFilePath), exc);
                    }
                    //
                    printFailureMessage();
                    return;
                }
                //
                compilerParameters.EmbeddedResources.Add(tempFilePath);
            }

            CompilerResults compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, sources.ToArray());
#endif

            if (logger.IsInfoEnabled) {
                logger.Info("Compiler output :");
                foreach (string s in compilerResults.Output) {
                    logger.Info(s);
                }
            }

            // Copying all required files
            foreach (IncludedObjectConfigBase configBase in includedObjects) {
                if (!String.IsNullOrEmpty(configBase.CopyCompressedTo)) {
                    string sourceFilePath = includedObjectsPackedFiles[configBase];
                    string destFilePath = configurePathByConfigurationVariables(configBase.CopyCompressedTo, configuration);
                    //
                    try {
                        File.Copy(sourceFilePath, destFilePath, true);
                    } catch (IOException exc) {
                        if (logger.IsErrorEnabled) {
                            logger.Error(String.Format("Error while copying file {0} to {1}. Message : {2}.", sourceFilePath, destFilePath, exc.Message), exc);
                        }
                        //
                        printFailureMessage();
                        return;
                    }
                }
            }

            if (compilerResults.Errors.HasErrors) {
                if (logger.IsErrorEnabled) {
                    logger.Error("Errors occured during building project.");
                }
                //
                printFailureMessage();
                return;
            }

            // Appending overlays
            if (0 != overlaysOrdered.Count) {
                if (logger.IsTraceEnabled) {
                    logger.Trace("Starting appending overlays.");
                }
                //
                try {
                    Int32 outputAssemblySize = unchecked((int) new FileInfo(outputAssemblyPath).Length);
                    using (FileStream stream = File.OpenWrite(outputAssemblyPath)) {
                        stream.Seek(0, SeekOrigin.End);
                        //
                        for (int i = 0; i < overlaysOrdered.Count; i++) {
                            byte[] overlayRawData = File.ReadAllBytes(includedObjectsPackedFiles[overlaysOrdered[i]]);
                            stream.Write(overlayRawData, 0, overlayRawData.Length);
                        }
                        //
                        byte[] initialOffsetStamp = new byte[4];
                        initialOffsetStamp[3] = (byte) (outputAssemblySize & 0xFF);
                        initialOffsetStamp[2] = (byte) ((outputAssemblySize >> 8) & 0xFF);
                        initialOffsetStamp[1] = (byte) ((outputAssemblySize >> 16) & 0xFF);
                        initialOffsetStamp[0] = (byte) ((outputAssemblySize >> 24) & 0xFF);
                        //
                        if (logger.IsTraceEnabled) {
                            logger.Trace(String.Format("Writing initial offset stamp : {0} bytes.", outputAssemblySize));
                        }
                        stream.Write(initialOffsetStamp, 0, initialOffsetStamp.Length);
                    }
                } catch (IOException exc) {
                    if (logger.IsErrorEnabled) {
                        logger.Error(String.Format("Error while appending overlays to file. Message : {0}.", exc.Message), exc);
                    }
                    //
                    printFailureMessage();
                    return;
                }
            }

            printSuccessMessage();
        }

        private struct EmbeddedResourceInfo
        {
            public readonly byte[] Data;
            public readonly string ZipEntryName;

            public EmbeddedResourceInfo(byte[] data, string zipEntryName) {
                this.Data = data;
                this.ZipEntryName = zipEntryName;
            }
        }

        static readonly List<string> sources = new List<string>();
        static readonly List<EmbeddedResourceInfo> embeddedResources = new List<EmbeddedResourceInfo>();

        private static void loadLoaderSourcesFromZip() {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Stream zipSourcesStream = executingAssembly.GetManifestResourceStream(executingAssembly.GetName().Name + ".NBox.Loader.zip");
            if (zipSourcesStream == null) {
                zipSourcesStream = executingAssembly.GetManifestResourceStream("NBox.Loader.zip");
                if (zipSourcesStream == null) {
                    throw new InvalidOperationException("Cannot load loader's sources zip file.");
                }
            }
            //
            using (zipSourcesStream) {
                using (ZipFile zipFile = new ZipFile(zipSourcesStream)) {
                    foreach (ZipEntry zipEntry in zipFile) {
                        if ((zipEntry.IsFile) && (!zipEntry.IsDirectory) && (zipEntry.CanDecompress)) {
                            byte[] buffer = new byte[zipEntry.Size];
                            StreamUtils.ReadFully(zipFile.GetInputStream(zipEntry), buffer);
                            //
                            if (zipEntry.Name.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase)) {
                                sources.Add(Encoding.UTF8.GetString(buffer));
                            } else {
                                embeddedResources.Add(new EmbeddedResourceInfo(buffer, zipEntry.Name));
                            }
                        }
                    }
                }
            }
        }

        private static void printSuccessMessage() {
            Console.WriteLine("BUILD SUCCEEDED.");
            Console.WriteLine();
        }

        private static void printFailureMessage() {
            Console.WriteLine("BUILD FAILED.");
            Console.WriteLine();
        }

        private static void printUsage() {
            Console.WriteLine("NBox 0.1 (c) 2009, Elw00d");
            Console.WriteLine("Usage : NBox.exe <config-file>");
            Console.WriteLine();
            Console.WriteLine("<config-file> - path to XML configuration file (see samples and schemas)");
            Console.WriteLine();
        }

        static void Main(string[] args) {
            try {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                //
                _Main(args);
            } catch (Exception exc) {
                if (logger.IsFatalEnabled) {
                    logger.Fatal("Unhandled exception intercepted in the Main method.");
                }
                //
                handleUnhandledExceptionObject(exc);
            }
        }

        private static void handleUnhandledExceptionObject(object exceptionObject) {
            if (logger.IsFatalEnabled) {
                if (exceptionObject != null) {
                    if (exceptionObject is Exception) {
                        logger.Fatal("An unhandled exception occured. Program is terminating.", (Exception) exceptionObject);
                    } else {
                        logger.Fatal(String.Format("An unhandled exception occured. Program is terminating. Exception object : {0}", exceptionObject));
                    }
                }
            }
            //
            Environment.Exit(-1);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args) {
            if (logger.IsFatalEnabled) {
                logger.Fatal("An unhandled exception has been intercepted in CurrentDomainOnUnhandledException handler.");
            }
            //
            handleUnhandledExceptionObject(args.ExceptionObject);
        }

        private static string configurePathByConfigurationVariables(string configurablePath, BuildConfiguration configuration) {
            if (configurablePath.Contains("%configdir%")) {
                return (configurePath(configurablePath, "%configdir%", configuration.Variables.ConfigFileDirectory));
            }
            if (configurablePath.Contains("%root%")) {
                return (configurePath(configurablePath, "%rootdir%", configuration.Variables.RootDirectory));
            }
            //
            return (configurablePath);
        }

        private static string configurePath(string configurablePath, string variable, string variableName) {
            configurablePath = configurablePath.Replace('/', Path.DirectorySeparatorChar);
            if (configurablePath.Contains(variable)) {
                string endOfPath = configurablePath.Replace(variable, String.Empty);
                if (!String.IsNullOrEmpty(endOfPath) && endOfPath[0] == Path.DirectorySeparatorChar) {
                    endOfPath = endOfPath.Substring(1);
                }
                if (!String.IsNullOrEmpty(endOfPath)) {
                    return (Path.Combine(variableName, endOfPath));
                }
                return (variableName);
            }
            return (Path.GetFullPath(configurablePath));
        }
    }
}
