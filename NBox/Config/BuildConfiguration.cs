using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using NBox.Utils;

namespace NBox.Config
{
    public sealed class BuildConfiguration
    {
        private const string ERROR_ID_ALREADY_EXISTS = "Another instance with same ID exisis already.";

        private readonly BuildConfigurationVariables variables;

        public BuildConfigurationVariables Variables {
            get {
                return (variables);
            }
        }

        private readonly List<CompressionConfig> compressionConfigs = new List<CompressionConfig>();

        private readonly List<IncludedAssemblyConfig> assemblyConfigs = new List<IncludedAssemblyConfig>();

        private readonly List<IncludedFileConfig> fileConfigs = new List<IncludedFileConfig>();

        private readonly OutputConfig outputConfig;

        public OutputConfig OutputConfig {
            get {
                return (outputConfig);
            }
        }

        private readonly string targetNamespace;

        private void addCompressionConfigToList(CompressionConfig compressionConfig) {
            if (GetCompressionConfigByID(compressionConfig.ID) != null) {
                throw new InvalidOperationException(ERROR_ID_ALREADY_EXISTS);
            }
            this.compressionConfigs.Add(compressionConfig);
        }

        private void addAssemblyConfigToList(IncludedAssemblyConfig includedAssemblyConfig) {
            if (GetAssemblyConfigByID(includedAssemblyConfig.ID) != null) {
                throw new InvalidOperationException(ERROR_ID_ALREADY_EXISTS);
            }
            this.assemblyConfigs.Add(includedAssemblyConfig);
        }

        private void addFileConfigToList(IncludedFileConfig includedFileConfig) {
            if (GetFileConfigByID(includedFileConfig.ID) != null) {
                throw new InvalidOperationException(ERROR_ID_ALREADY_EXISTS);
            }
            this.fileConfigs.Add(includedFileConfig);
        }

        [CanBeNull]
        public CompressionConfig GetCompressionConfigByID(string id) {
            foreach (CompressionConfig compressionConfig in this.compressionConfigs) {
                if (compressionConfig.ID == id) {
                    return (compressionConfig);
                }
            }
            //
            return (null);
        }

        [CanBeNull]
        public IncludedAssemblyConfig GetAssemblyConfigByID(string id) {
            foreach (IncludedAssemblyConfig includedAssemblyConfig in this.assemblyConfigs) {
                if (includedAssemblyConfig.ID == id) {
                    return (includedAssemblyConfig);
                }
            }
            //
            return (null);
        }

        [CanBeNull]
        public IncludedFileConfig GetFileConfigByID(string id) {
            foreach (IncludedFileConfig includedFileConfig in this.fileConfigs) {
                if (includedFileConfig.ID == id) {
                    return (includedFileConfig);
                }
            }
            //
            return (null);
        }

        private readonly string defaultCompressionConfigRefForAssemblies = null;

        private readonly IncludeMethodKind defaultIncludeMethodKindForAssemblies = IncludeMethodKind.Resource;

        private readonly bool defaultGeneratePartialAliasesForAssemblies = true;

        private readonly bool defaultLazyLoadForAssemblies = true;

        private readonly string defaultCompressionConfigRefForFiles = null;

        private readonly IncludeMethodKind defaultIncludeMethodKindForFiles = IncludeMethodKind.Overlay;

        private readonly OverwritingOptions defaultOverwritingOptionsForFiles = OverwritingOptions.Always;

        public BuildConfiguration(XmlDocument document, BuildConfigurationVariables variables) {
            ArgumentChecker.NotNull(document, "document");
            //
            this.variables = variables;
            //
            XmlSchema schema;
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Stream schemaStream = executingAssembly.GetManifestResourceStream(executingAssembly.GetName().Name + ".config-file.xsd");
            if (schemaStream == null) {
                schemaStream = executingAssembly.GetManifestResourceStream("config-file.xsd");
                if (schemaStream == null) {
                    throw new InvalidOperationException("Cannot load XSD schema for XML configuration.");
                }
            }
            using (schemaStream) {
                schema = XmlSchema.Read(schemaStream, schemaValidationEventHandler);
            }
            //
            document.Schemas.Add(schema);
            document.Validate(documentValidationEventHandler);
            //
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("def", targetNamespace = schema.TargetNamespace);
            // Parsing compression options
            XmlNode compressionOptionsSetNode = document.SelectSingleNode("def:configuration/def:compression-options-set", namespaceManager);
            if (compressionOptionsSetNode == null) {
                throw new InvalidOperationException("Unable to find any compression option.");
            }
            //
            XmlNodeList compressionOptionNodes = compressionOptionsSetNode.SelectNodes("def:compression-option", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode compressionOptionNode in compressionOptionNodes) {
                // ReSharper restore PossibleNullReferenceException
                string id = compressionOptionNode.Attributes["id"].Value;
                XmlNode levelNode = compressionOptionNode.SelectSingleNode("def:level", namespaceManager);
                if (levelNode == null) {
                    throw new InvalidOperationException("Cannot determine the level of compression.");
                }
                CompressionLevel compressionLevel = (CompressionLevel)Enum.Parse(typeof(CompressionLevel), levelNode.Attributes["value"].Value, true);
                CompressionConfig compressionConfig = new CompressionConfig(id, compressionLevel);
                addCompressionConfigToList(compressionConfig);
            }
            // Parsing included assemblies options
            XmlNode assembliesNode = document.SelectSingleNode("def:configuration/def:assemblies", namespaceManager);
            if (assembliesNode != null) {
                if (assembliesNode.Attributes["default-compression-ref"] != null) {
                    this.defaultCompressionConfigRefForAssemblies = assembliesNode.Attributes["default-compression-ref"].Value;
                }
                if (assembliesNode.Attributes["default-include-method"] != null) {
                    this.defaultIncludeMethodKindForAssemblies = (IncludeMethodKind) Enum.Parse(typeof (IncludeMethodKind),
                        assembliesNode.Attributes["default-include-method"].Value, true);
                }
                if (assembliesNode.Attributes["default-generate-partial-aliases"] != null) {
                    this.defaultGeneratePartialAliasesForAssemblies = bool.Parse(assembliesNode.Attributes["default-generate-partial-aliases"].Value);
                }
                if (assembliesNode.Attributes["default-lazy-load"] != null) {
                    this.defaultLazyLoadForAssemblies = bool.Parse(assembliesNode.Attributes["default-lazy-load"].Value);
                }
            }
            //
            XmlNodeList assemblyNodes = document.SelectNodes("def:configuration/def:assemblies/def:assembly", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode assemblyNode in assemblyNodes) {
                // ReSharper restore PossibleNullReferenceException
                string idAttributeValue = assemblyNode.Attributes["id"].Value;
                string pathAttributeValue = assemblyNode.Attributes["path"].Value;
                string compressionRefAttributeValue;
                if (assemblyNode.Attributes["compression-ref"] != null) {
                    compressionRefAttributeValue = assemblyNode.Attributes["compression-ref"].Value;
                } else {
                    compressionRefAttributeValue = this.defaultCompressionConfigRefForAssemblies;
                }
                //
                string copyCompressedToAttributeValue = String.Empty;
                if (assemblyNode.Attributes["copy-compressed-to"] != null) {
                    copyCompressedToAttributeValue = assemblyNode.Attributes["copy-compressed-to"].Value;
                }
                CompressionConfig compressionConfigByRef = GetCompressionConfigByID(compressionRefAttributeValue);
                if (compressionConfigByRef == null) {
                    throw new InvalidOperationException("Requested compression option was not found.");
                }
                //
                IncludeMethod includeMethod = (assemblyNode.Attributes["include-method"] != null)
                    ? IncludeMethod.Parse(assemblyNode)
                    : new IncludeMethod(this.defaultIncludeMethodKindForAssemblies);
                // Specially for assembly attributes
                bool lazyLoadAttributeValue = defaultLazyLoadForAssemblies;
                if (assemblyNode.Attributes["lazy-load"] != null) {
                    lazyLoadAttributeValue = bool.Parse(assemblyNode.Attributes["lazy-load"].Value);
                }
                //
                bool generatePartialAliasesAttributeValue = defaultGeneratePartialAliasesForAssemblies;
                XmlAttribute generatePartialAliasesAttribute = assemblyNode.Attributes["generate-partial-aliases"];
                if (generatePartialAliasesAttribute != null) {
                    generatePartialAliasesAttributeValue = bool.Parse(generatePartialAliasesAttribute.Value);
                }
                //
                List<string> aliases = new List<string>();
                XmlNodeList aliasNodes = assemblyNode.SelectNodes("def:aliases/def:alias", namespaceManager);
                // ReSharper disable PossibleNullReferenceException
                foreach (XmlNode aliasNode in aliasNodes) {
                    // ReSharper restore PossibleNullReferenceException
                    XmlAttribute aliasValueAttribute = aliasNode.Attributes["value"];
                    if (aliasValueAttribute == null) {
                        throw new InvalidOperationException("Required attribute 'value' has been skipped in alias definition.");
                    }
                    aliases.Add(aliasValueAttribute.Value);
                }
                //
                addAssemblyConfigToList(new IncludedAssemblyConfig(idAttributeValue,
                    includeMethod, pathAttributeValue, compressionConfigByRef,
                    copyCompressedToAttributeValue, lazyLoadAttributeValue, aliases,
                    generatePartialAliasesAttributeValue));
            }
            // Parsing included files options
            XmlNode filesNode = document.SelectSingleNode("def:configuration/def:files", namespaceManager);
            if (filesNode != null) {
                if (filesNode.Attributes["default-compression-ref"] != null) {
                    this.defaultCompressionConfigRefForFiles = filesNode.Attributes["default-compression-ref"].Value;
                }
                if (filesNode.Attributes["default-include-method"] != null) {
                    this.defaultIncludeMethodKindForFiles = (IncludeMethodKind)Enum.Parse(typeof(IncludeMethodKind),
                        filesNode.Attributes["default-include-method"].Value, true);
                }
                if (filesNode.Attributes["default-overwrite-on-extracting"] != null) {
                    this.defaultOverwritingOptionsForFiles = (OverwritingOptions) Enum.Parse(typeof (OverwritingOptions),
                        filesNode.Attributes["default-overwrite-on-extracting"].Value, true);
                }
            }
            //
            XmlNodeList fileNodes = document.SelectNodes("def:configuration/def:files/def:file", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode fileNode in fileNodes) {
                // ReSharper restore PossibleNullReferenceException
                string idAttributeValue = fileNode.Attributes["id"].Value;
                string pathAttributeValue = fileNode.Attributes["path"].Value;
                string compressionRefAttributeValue;
                if (fileNode.Attributes["compression-ref"] != null) {
                    compressionRefAttributeValue = fileNode.Attributes["compression-ref"].Value;
                } else {
                    compressionRefAttributeValue = this.defaultCompressionConfigRefForFiles;
                }
                //
                string copyCompressedToAttributeValue = String.Empty;
                if (fileNode.Attributes["copy-compressed-to"] != null) {
                    copyCompressedToAttributeValue = fileNode.Attributes["copy-compressed-to"].Value;
                }
                CompressionConfig compressionConfigByRef = GetCompressionConfigByID(compressionRefAttributeValue);
                if (compressionConfigByRef == null) {
                    throw new InvalidOperationException("Requested compression option was not found.");
                }
                //
                IncludeMethod includeMethod;
                if (fileNode.Attributes["include-method"] != null) {
                    includeMethod = IncludeMethod.Parse(fileNode);
                } else {
                    includeMethod = new IncludeMethod(this.defaultIncludeMethodKindForFiles);
                }
                // Speciallied for file attributes
                string extractToPathAttributeValue = String.Empty;
                XmlAttribute extractToPathAttribute = fileNode.Attributes["extract-to-path"];
                if (extractToPathAttribute != null) {
                    extractToPathAttributeValue = extractToPathAttribute.Value;
                }
                //
                OverwritingOptions overwritingOptionsAttributeValue = defaultOverwritingOptionsForFiles;
                XmlAttribute overwritingOptionsAttribute = fileNode.Attributes["overwrite-on-extracting"];
                if (overwritingOptionsAttribute != null) {
                    overwritingOptionsAttributeValue = (OverwritingOptions)Enum.Parse(typeof(OverwritingOptions), overwritingOptionsAttribute.Value, true);
                }
                //
                addFileConfigToList(new IncludedFileConfig(idAttributeValue,
                    includeMethod, pathAttributeValue, compressionConfigByRef,
                    copyCompressedToAttributeValue, extractToPathAttributeValue, overwritingOptionsAttributeValue));
            }
            // Parsing output configuration
            ApartmentState outputApartmentState;
            string outputPath;
            string assemblyName = null;
            OutputAppType outputAppType;
            OutputMachine outputMachine;
            IncludedAssemblyConfig mainAssembly;
            bool outputGrabResources = false;
            string outputWin32IconPath = null;
            string compilerOptions = null;
            CompilerVersionRequired compilerVersionRequired = CompilerVersionRequired.v2_0;
            string appConfigFileId = null;
            bool useShadowCopying = false;

            //
            XmlNode outputNode = document.SelectSingleNode("def:configuration/def:output", namespaceManager);
            if (outputNode == null) {
                throw new InvalidOperationException("Unable to find an output exe configuration.");
            }
            //
            outputApartmentState = (ApartmentState)Enum.Parse(typeof(ApartmentState), outputNode.Attributes["apartment"].Value, true);
            outputPath = outputNode.Attributes["path"].Value;
            if (outputNode.Attributes["assembly-name"] != null) {
                assemblyName = outputNode.Attributes["assembly-name"].Value;
            }
            outputAppType = (OutputAppType)Enum.Parse(typeof(OutputAppType), outputNode.Attributes["apptype"].Value, true);
            outputMachine = (OutputMachine)Enum.Parse(typeof(OutputMachine), outputNode.Attributes["machine"].Value, true);
            if (outputNode.Attributes["grab-resources"] != null) {
                outputGrabResources = bool.Parse(outputNode.Attributes["grab-resources"].Value);
            }
            //
            XmlNode compilerOptionsNode = outputNode.SelectSingleNode("def:compiler-options/def:options", namespaceManager);
            if (compilerOptionsNode != null) {
                compilerOptions = compilerOptionsNode.InnerText;
            }

            XmlNode compilerVersionRequiredNode = outputNode.SelectSingleNode("def:compiler-options", namespaceManager);
            if (null != compilerVersionRequiredNode)
            {
                XmlAttribute compilerVerRequiredAttr = compilerVersionRequiredNode.Attributes["version-required"];
                if (null != compilerVerRequiredAttr)
                {
                    string compilerVerRaw = compilerVerRequiredAttr.Value;
                    switch (compilerVerRaw)
                    {
                        case "v2.0":
                            {
                                compilerVersionRequired = CompilerVersionRequired.v2_0;
                                break;
                            }
                        case "v3.0":
                            {
                                compilerVersionRequired = CompilerVersionRequired.v3_0;
                                break;
                            }
                        case "v3.5":
                            {
                                compilerVersionRequired = CompilerVersionRequired.v3_5;
                                break;
                            }
                        case "v4.0":
                            {
                                compilerVersionRequired = CompilerVersionRequired.v4_0;
                                break;
                            }
                    }
                }
            }

            //
            mainAssembly = GetAssemblyConfigByID(outputNode.Attributes["main-assembly-ref"].Value);
            if (mainAssembly == null) {
                throw new InvalidOperationException("Main assembly specified with incorrect ID.");
            }
            //
            XmlAttribute outputWin32IconAttribute = outputNode.Attributes["win32icon"];
            if (outputWin32IconAttribute != null) {
                outputWin32IconPath = outputWin32IconAttribute.Value;
            }
            //

            XmlNode appConfigNode = outputNode.SelectSingleNode("def:includes/def:files/def:app-config", namespaceManager);
            if (null != appConfigNode)
            {
                XmlAttribute appConfigFileIdAttribute = appConfigNode.Attributes["ref"];
                if (null == appConfigFileIdAttribute)
                {
                    throw new InvalidOperationException("Required attribute ref not specidied for app-config.");
                }
                appConfigFileId = appConfigFileIdAttribute.Value;
                XmlAttribute useShahowCopyingAttribute = appConfigNode.Attributes["use-shadow-copying"];
                if (null != useShahowCopyingAttribute)
                {
                    useShadowCopying = bool.Parse(useShahowCopyingAttribute.Value);
                }
            }

            this.outputConfig = new OutputConfig(outputAppType, outputMachine, outputPath, assemblyName,
                outputWin32IconPath, mainAssembly, outputApartmentState, outputGrabResources,
                compilerOptions, compilerVersionRequired, appConfigFileId, useShadowCopying);
            //
            XmlNodeList includesAssemblyNodes = outputNode.SelectNodes("def:includes/def:assemblies/def:assembly", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode assemblyNode in includesAssemblyNodes) {
                // ReSharper restore PossibleNullReferenceException
                IncludedAssemblyConfig assemblyConfigByRef = GetAssemblyConfigByID(assemblyNode.Attributes["ref"].Value);
                if (assemblyConfigByRef == null) {
                    throw new InvalidOperationException(String.Format("Cannot find assembly to include by ID='{0}'.", assemblyNode.Attributes["ref"].Value));
                }
                outputConfig.IncludedObjects.Add(assemblyConfigByRef);
            }
            //
            XmlNodeList includesFileNodes = outputNode.SelectNodes("def:includes/def:files/def:file", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode includesFileNode in includesFileNodes) {
                // ReSharper restore PossibleNullReferenceException
                string id = includesFileNode.Attributes["ref"].Value;

                // Avoid duplicating the app.config file.
                if (null == appConfigFileId || appConfigFileId != id)
                {
                    IncludedFileConfig fileConfigByRef = GetFileConfigByID(id);
                    if (fileConfigByRef == null)
                    {
                        throw new InvalidOperationException(String.Format("Cannot find file to include by ID='{0}'.",
                                                                          includesFileNode.Attributes["ref"]));
                    }
                    outputConfig.IncludedObjects.Add(fileConfigByRef);
                }
            }
            //

            if (null != appConfigFileId)
            {
                outputConfig.IncludedObjects.Add(GetFileConfigByID(appConfigFileId));
            }

        }

        private static void schemaValidationEventHandler(object sender, ValidationEventArgs args) {
            if (args.Exception != null) {
                throw new InvalidOperationException("Error validating XSD schema.", args.Exception);
            }
        }

        private static void documentValidationEventHandler(object sender, ValidationEventArgs args) {
            if (args.Exception != null) {
                throw new InvalidOperationException("Error validating XML configuration file.", args.Exception);
            }
        }

#if !LOADER
        public BuildConfiguration(string configFilePath) :
            this(getXmlDocument(configFilePath), getVariables(configFilePath)) {
        }

        private static BuildConfigurationVariables getVariables(string configFilePath) {
            ArgumentChecker.NotNullOrEmptyExistsFilePath(configFilePath, "configFilePath");
            //
            if (!Path.IsPathRooted(configFilePath)) {
                throw new ArgumentException("Full path required.", "configFilePath");
            }
            //
            string rootDirectoryName = Path.GetPathRoot(configFilePath);
            string directoryName = Path.GetDirectoryName(configFilePath);
            return (new BuildConfigurationVariables(directoryName, rootDirectoryName));
        }

        private static XmlDocument getXmlDocument(string configFilePath) {
            ArgumentChecker.NotNullOrEmptyExistsFilePath(configFilePath, "configFilePath");
            //
            XmlDocument document = new XmlDocument();
            document.Load(configFilePath);
            return (document);
        }

        public XmlDocument ExportConfigurationXML() {
            XmlDocument document = new XmlDocument();
            XmlDeclaration xmlDeclaration = document.CreateXmlDeclaration("1.0", "utf-8", null);
            document.AppendChild(xmlDeclaration);
            //
            XmlElement configurationElement = document.CreateElement("configuration");

            XmlAttribute targetNamespaceAttribute = document.CreateAttribute("xmlns");
            targetNamespaceAttribute.Value = targetNamespace;
            configurationElement.Attributes.Append(targetNamespaceAttribute);

            // Compression options serialization
            XmlElement compressionOptionsSetElement = document.CreateElement("compression-options-set");
            foreach (CompressionConfig compressionConfig in compressionConfigs) {
                XmlElement compressionOptionElement = document.CreateElement(compressionConfig.GetXmlNodeDefaultName());
                compressionConfig.XmlExport(compressionOptionElement);
                compressionOptionsSetElement.AppendChild(compressionOptionElement);
            }
            configurationElement.AppendChild(compressionOptionsSetElement);

            // Assemblies
            XmlElement assembliesElement = document.CreateElement("assemblies");
            foreach (IncludedAssemblyConfig assemblyConfig in this.assemblyConfigs) {
                XmlElement includedObjectConfigElement = document.CreateElement(assemblyConfig.GetXmlNodeDefaultName());
                assemblyConfig.XmlExport(includedObjectConfigElement);
                assembliesElement.AppendChild(includedObjectConfigElement);
            }
            configurationElement.AppendChild(assembliesElement);

            // Files
            XmlElement filesElement = document.CreateElement("files");
            foreach (IncludedFileConfig fileConfig in this.fileConfigs) {
                XmlElement includedObjectConfigElement = document.CreateElement(fileConfig.GetXmlNodeDefaultName());
                fileConfig.XmlExport(includedObjectConfigElement);
                filesElement.AppendChild(includedObjectConfigElement);
            }
            configurationElement.AppendChild(filesElement);

            // Output options serialization
            XmlElement outputElement = document.CreateElement("output");
            XmlAttribute outputPathAttribute = document.CreateAttribute("path");
            outputPathAttribute.Value = this.outputConfig.Path;
            outputElement.Attributes.Append(outputPathAttribute);

            XmlAttribute outputAppTypeAttribute = document.CreateAttribute("apptype");
            outputAppTypeAttribute.Value = Convert.ToString(this.outputConfig.AppType);
            outputElement.Attributes.Append(outputAppTypeAttribute);

            XmlAttribute outputMachineAttribute = document.CreateAttribute("machine");
            outputMachineAttribute.Value = Convert.ToString(this.outputConfig.Machine);
            outputElement.Attributes.Append(outputMachineAttribute);

            XmlAttribute outputMainAssemblyRefAttribute = document.CreateAttribute("main-assembly-ref");
            outputMainAssemblyRefAttribute.Value = this.outputConfig.MainAssembly.ID;
            outputElement.Attributes.Append(outputMainAssemblyRefAttribute);

            XmlAttribute outputApartmentAttribute = document.CreateAttribute("apartment");
            outputApartmentAttribute.Value = Convert.ToString(this.outputConfig.ApartmentState);
            outputElement.Attributes.Append(outputApartmentAttribute);

            //if (!String.IsNullOrEmpty(this.outputConfig.Win32IconPath)) {
            //    XmlAttribute outputWin32IconAttribute = document.CreateAttribute("win32icon");
            //    outputWin32IconAttribute.Value = this.outputConfig.Win32IconPath;
            //    outputElement.Attributes.Append(outputWin32IconAttribute);
            //}

            XmlElement includesElement = document.CreateElement("includes");

            XmlElement includesAssembliesElement = document.CreateElement("assemblies");
            XmlElement includesFilesElement = document.CreateElement("files");
            //

            if (!string.IsNullOrEmpty(outputConfig.AppConfigFileID))
            {
                XmlElement appConfigElement = document.CreateElement("app-config");
                XmlAttribute appConfigFileIdAttribute = document.CreateAttribute("ref");
                appConfigFileIdAttribute.Value = outputConfig.AppConfigFileID;
                appConfigElement.Attributes.Append(appConfigFileIdAttribute);

                if (false != outputConfig.UseShadowCopying )
                {
                    XmlAttribute useShadowCopyAttribute = document.CreateAttribute("use-shadow-copying");
                    useShadowCopyAttribute.Value = outputConfig.UseShadowCopying.ToString().ToLower();
                    appConfigElement.Attributes.Append(useShadowCopyAttribute);
                }
                includesFilesElement.AppendChild(appConfigElement);
            }

            foreach (IncludedObjectConfigBase configBase in outputConfig.IncludedObjects) {
                // will create element with name "assembly" or "file"
                XmlElement includedObjectElement = document.CreateElement(configBase.GetXmlNodeDefaultName());
                XmlAttribute includedObjectRefAttribute = document.CreateAttribute("ref");
                includedObjectRefAttribute.Value = configBase.ID;
                includedObjectElement.Attributes.Append(includedObjectRefAttribute);
                //
                if (configBase is IncludedAssemblyConfig) {
                    includesAssembliesElement.AppendChild(includedObjectElement);
                } else if (configBase is IncludedFileConfig) {
                    includesFilesElement.AppendChild(includedObjectElement);
                }
            }
            //
            includesElement.AppendChild(includesAssembliesElement);
            includesElement.AppendChild(includesFilesElement);

            outputElement.AppendChild(includesElement);

            configurationElement.AppendChild(outputElement);

            document.AppendChild(configurationElement);
            //
            return (document);
        }
#endif
    }
}