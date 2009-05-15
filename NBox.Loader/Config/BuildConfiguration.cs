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
    public enum OutputAppType
    {
        Console = 1,
        WinExe = 2
    }

    public enum OutputMachine
    {
        Any = 1,
        x86 = 2,
        x64 = 3,
        Itanium = 4
    }

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

        private void addCompressionConfigToList(CompressionConfig compressionConfig) {
            if (GetCompressionConfigByID(compressionConfig.ID) != null) {
                throw new InvalidOperationException(ERROR_ID_ALREADY_EXISTS);
            }
            compressionConfigs.Add(compressionConfig);
        }

        [CanBeNull]
        public CompressionConfig GetCompressionConfigByID(string id) {
            foreach (CompressionConfig compressionConfig in compressionConfigs) {
                if (compressionConfig.ID == id) {
                    return (compressionConfig);
                }
            }
            //
            return (null);
        }

        private readonly List<IncludedAssemblyConfig> includedAssemblyConfigs = new List<IncludedAssemblyConfig>();

        private void addIncludedAssemblyConfigToList(IncludedAssemblyConfig includedAssemblyConfig) {
            if (GetIncludedAssemblyConfigByID(includedAssemblyConfig.ID) != null) {
                throw new InvalidOperationException(ERROR_ID_ALREADY_EXISTS);
            }
            includedAssemblyConfigs.Add(includedAssemblyConfig);
        }

        [CanBeNull]
        public IncludedAssemblyConfig GetIncludedAssemblyConfigByID(string id) {
            foreach (IncludedAssemblyConfig includedAssemblyConfig in includedAssemblyConfigs) {
                if (includedAssemblyConfig.ID == id) {
                    return (includedAssemblyConfig);
                }
            }
            //
            return (null);
        }

        private readonly List<IncludedFileConfig> includedFileConfigs = new List<IncludedFileConfig>();

        private void addIncludedFileConfigToList(IncludedFileConfig includedFileConfig) {
            if (GetIncludedFileConfigByID(includedFileConfig.ID) != null) {
                throw new InvalidOperationException(ERROR_ID_ALREADY_EXISTS);
            }
            includedFileConfigs.Add(includedFileConfig);
        }

        [CanBeNull]
        public IncludedFileConfig GetIncludedFileConfigByID(string id) {
            foreach (IncludedFileConfig includedFileConfig in includedFileConfigs) {
                if (includedFileConfig.ID == id) {
                    return (includedFileConfig);
                }
            }
            //
            return (null);
        }

        private readonly OutputAppType outputAppType;

        public OutputAppType OutputAppType {
            get {
                return (outputAppType);
            }
        }

        private readonly OutputMachine outputMachine;

        public OutputMachine OutputMachine {
            get {
                return (outputMachine);
            }
        }

        private readonly string outputPath;

        public string OutputPath {
            get {
                return (outputPath);
            }
        }

        private readonly string outputWin32IconPath;

        public string OutputWin32IconPath {
            get {
                return (outputWin32IconPath);
            }
        }

        private readonly IncludedAssemblyConfig mainAssembly;

        public IncludedAssemblyConfig MainAssembly {
            get {
                return (mainAssembly);
            }
        }

        private readonly List<IncludedObjectConfigBase> includedObjects = new List<IncludedObjectConfigBase>();

        public IList<IncludedObjectConfigBase> IncludedObjects {
            get {
                return (includedObjects);
            }
        }

        private readonly ApartmentState outputApartmentState;

        public ApartmentState OutputApartmentState {
            get {
                return (outputApartmentState);
            }
        }

        //private readonly List<string> references = new List<string>();

        //public IList<string> References {
        //    get {
        //        return (references);
        //    }
        //}

        private readonly string targetNamespace;

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
                CompressionLevel compressionLevel = (CompressionLevel) Enum.Parse(typeof (CompressionLevel), levelNode.Attributes["value"].Value, true);
                CompressionConfig compressionConfig = new CompressionConfig(id, compressionLevel);
                addCompressionConfigToList(compressionConfig);
            }
            // Parsing included assemblies options
            XmlNodeList assemblyNodes = document.SelectNodes("def:configuration/def:assemblies/def:assembly", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode assemblyNode in assemblyNodes) {
                // ReSharper restore PossibleNullReferenceException
                string idAttributeValue = assemblyNode.Attributes["id"].Value;
                string pathAttributeValue = assemblyNode.Attributes["path"].Value;
                string compressionRefAttributeValue = assemblyNode.Attributes["compression-ref"].Value;
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
                IncludeMethod includeMethod = IncludeMethod.Parse(assemblyNode);
                // Specially for assembly attributes
                bool lazyLoadAttributeValue = true;
                if (assemblyNode.Attributes["lazy-load"] != null) {
                    lazyLoadAttributeValue = bool.Parse(assemblyNode.Attributes["lazy-load"].Value);
                }
                //
                bool generatePartiallyAliasesAttributeValue = IncludedAssemblyConfig.GENERATE_PARTIALLY_ALIASES_DEFAULT;
                XmlNode aliasesNode = assemblyNode.SelectSingleNode("def:aliases", namespaceManager);
                if (aliasesNode != null) {
                    XmlAttribute generatePartiallyAliasesAttribute = aliasesNode.Attributes["generate-partially-aliases"];
                    if (generatePartiallyAliasesAttribute != null) {
                        generatePartiallyAliasesAttributeValue = bool.Parse(generatePartiallyAliasesAttribute.Value);
                    }
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
                addIncludedAssemblyConfigToList(new IncludedAssemblyConfig(idAttributeValue,
                    includeMethod, pathAttributeValue, compressionConfigByRef,
                    copyCompressedToAttributeValue, lazyLoadAttributeValue, aliases,
                    generatePartiallyAliasesAttributeValue));
            }
            // Parsing included files options
            XmlNodeList fileNodes = document.SelectNodes("def:configuration/def:files/def:file", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode fileNode in fileNodes) {
                // ReSharper restore PossibleNullReferenceException
                string idAttributeValue = fileNode.Attributes["id"].Value;
                string pathAttributeValue = fileNode.Attributes["path"].Value;
                string compressionRefAttributeValue = fileNode.Attributes["compression-ref"].Value;
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
                IncludeMethod includeMethod = IncludeMethod.Parse(fileNode);
                // Speciallied for file attributes
                string extractToPathAttributeValue = String.Empty;
                XmlAttribute extractToPathAttribute = fileNode.Attributes["extract-to-path"];
                if (extractToPathAttribute != null) {
                    extractToPathAttributeValue = extractToPathAttribute.Value;
                }
                //
                OverwritingOptions overwritingOptionsAttributeValue = OverwritingOptions.Always;
                XmlAttribute overwritingOptionsAttribute = fileNode.Attributes["overwrite-on-extracting"];
                if (overwritingOptionsAttribute != null) {
                    overwritingOptionsAttributeValue = (OverwritingOptions) Enum.Parse(typeof (OverwritingOptions), overwritingOptionsAttribute.Value, true);
                }
                //
                addIncludedFileConfigToList(new IncludedFileConfig(idAttributeValue,
                    includeMethod, pathAttributeValue, compressionConfigByRef,
                    copyCompressedToAttributeValue, extractToPathAttributeValue, overwritingOptionsAttributeValue));
            }
            // Parsing output configuration
            XmlNode outputNode = document.SelectSingleNode("def:configuration/def:output", namespaceManager);
            if (outputNode == null) {
                throw new InvalidOperationException("Unable to find an output exe configuration.");
            }
            //
            this.outputApartmentState = (ApartmentState) Enum.Parse(typeof (ApartmentState), outputNode.Attributes["apartment"].Value, true);
            this.outputPath = outputNode.Attributes["path"].Value;
            outputAppType = (OutputAppType) Enum.Parse(typeof (OutputAppType), outputNode.Attributes["apptype"].Value, true);
            outputMachine = (OutputMachine) Enum.Parse(typeof (OutputMachine), outputNode.Attributes["machine"].Value, true);
            mainAssembly = GetIncludedAssemblyConfigByID(outputNode.Attributes["main-assembly-ref"].Value);
            if (mainAssembly == null) {
                throw new InvalidOperationException("Main assembly specified with incorrect ID.");
            }
            //
            XmlAttribute outputWin32IconAttribute = outputNode.Attributes["win32icon"];
            if (outputWin32IconAttribute != null) {
                outputWin32IconPath = outputWin32IconAttribute.Value;
            }
            //
            XmlNodeList includesAssemblyNodes = outputNode.SelectNodes("def:includes/def:assembly", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode assemblyNode in includesAssemblyNodes) {
                // ReSharper restore PossibleNullReferenceException
                IncludedAssemblyConfig assemblyConfigByRef = GetIncludedAssemblyConfigByID(assemblyNode.Attributes["ref"].Value);
                if (assemblyConfigByRef == null) {
                    throw new InvalidOperationException(String.Format("Cannot find assembly to include by ID='{0}'.", assemblyNode.Attributes["ref"].Value));
                }
                includedObjects.Add(assemblyConfigByRef);
            }
            //
            XmlNodeList includesFileNodes = outputNode.SelectNodes("def:includes/def:file", namespaceManager);
            // ReSharper disable PossibleNullReferenceException
            foreach (XmlNode includesFileNode in includesFileNodes) {
                // ReSharper restore PossibleNullReferenceException
                IncludedFileConfig fileConfigByRef = GetIncludedFileConfigByID(includesFileNode.Attributes["ref"].Value);
                if (fileConfigByRef == null) {
                    throw new InvalidOperationException(String.Format("Cannot find file to include by ID='{0}'.", includesFileNode.Attributes["ref"]));
                }
                includedObjects.Add(fileConfigByRef);
            }
//            //
//            XmlNodeList referencesNodes = outputNode.SelectNodes("def:references/def:reference", namespaceManager);
//// ReSharper disable PossibleNullReferenceException
//            foreach (XmlNode referenceNode in referencesNodes) {
//// ReSharper restore PossibleNullReferenceException
//                references.Add(referenceNode.Attributes["path"].Value);
//            }
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
            foreach (IncludedAssemblyConfig assemblyConfig in includedAssemblyConfigs) {
                XmlElement includedObjectConfigElement = document.CreateElement(assemblyConfig.GetXmlNodeDefaultName());
                assemblyConfig.XmlExport(includedObjectConfigElement);
                assembliesElement.AppendChild(includedObjectConfigElement);
            }
            configurationElement.AppendChild(assembliesElement);

            // Files
            XmlElement filesElement = document.CreateElement("files");
            foreach (IncludedFileConfig fileConfig in includedFileConfigs) {
                XmlElement includedObjectConfigElement = document.CreateElement(fileConfig.GetXmlNodeDefaultName());
                fileConfig.XmlExport(includedObjectConfigElement);
                filesElement.AppendChild(includedObjectConfigElement);
            }
            configurationElement.AppendChild(filesElement);

            // Output options serialization
            XmlElement outputElement = document.CreateElement("output");
            XmlAttribute outputPathAttribute = document.CreateAttribute("path");
            outputPathAttribute.Value = this.outputPath;
            outputElement.Attributes.Append(outputPathAttribute);

            XmlAttribute outputAppTypeAttribute = document.CreateAttribute("apptype");
            outputAppTypeAttribute.Value = Convert.ToString(this.outputAppType);
            outputElement.Attributes.Append(outputAppTypeAttribute);

            XmlAttribute outputMachineAttribute = document.CreateAttribute("machine");
            outputMachineAttribute.Value = Convert.ToString(this.outputMachine);
            outputElement.Attributes.Append(outputMachineAttribute);

            XmlAttribute outputMainAssemblyRefAttribute = document.CreateAttribute("main-assembly-ref");
            outputMainAssemblyRefAttribute.Value = this.mainAssembly.ID;
            outputElement.Attributes.Append(outputMainAssemblyRefAttribute);

            XmlAttribute outputApartmentAttribute = document.CreateAttribute("apartment");
            outputApartmentAttribute.Value = Convert.ToString(this.outputApartmentState);
            outputElement.Attributes.Append(outputApartmentAttribute);

            if (!String.IsNullOrEmpty(outputWin32IconPath)) {
                XmlAttribute outputWin32IconAttribute = document.CreateAttribute("win32icon");
                outputWin32IconAttribute.Value = outputWin32IconPath;
                outputElement.Attributes.Append(outputWin32IconAttribute);
            }

            XmlElement includesElement = document.CreateElement("includes");
            foreach (IncludedObjectConfigBase configBase in includedObjects) {
                XmlElement includedObjectElement = document.CreateElement(configBase.GetXmlNodeDefaultName());
                XmlAttribute includedObjectRefAttribute = document.CreateAttribute("ref");
                includedObjectRefAttribute.Value = configBase.ID;
                includedObjectElement.Attributes.Append(includedObjectRefAttribute);
                //
                includesElement.AppendChild(includedObjectElement);
            }
            outputElement.AppendChild(includesElement);

            configurationElement.AppendChild(outputElement);

            document.AppendChild(configurationElement);
            //
            return (document);
        }
#endif
    }
}