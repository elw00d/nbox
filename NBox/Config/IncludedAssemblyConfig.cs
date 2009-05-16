using System;
using System.Collections.Generic;
using System.Xml;
using NBox.Utils;

namespace NBox.Config
{
    public sealed class IncludedAssemblyConfig : IncludedObjectConfigBase
    {
        private readonly bool lazyLoad;

        public bool LazyLoad {
            get {
                return (lazyLoad);
            }
        }

        private readonly bool generatePartialAliases;

        public bool GeneratePartialAliases {
            get {
                return (this.generatePartialAliases);
            }
        }

        private readonly List<string> aliases;

        public IList<string> Aliases {
            get {
                return (aliases);
            }
        }

        public IncludedAssemblyConfig(string id, IncludeMethod includeMethod, string path, CompressionConfig compressionConfig, string copyCompressedTo, bool lazyLoad, IEnumerable<string> aliases, bool generatePartiallyAliases)
            : base(id, includeMethod, path, compressionConfig, copyCompressedTo) {
            //
            ArgumentChecker.NotNull(aliases, "aliases");
            //
            this.lazyLoad = lazyLoad;
            this.aliases = new List<string>(aliases);
            this.generatePartialAliases = generatePartiallyAliases;
        }

#if !LOADER
        public override void XmlExport(XmlNode xmlNode) {
            base.XmlExport(xmlNode);
            //
            XmlDocument ownerDocument = xmlNode.OwnerDocument;

            XmlAttribute lazyLoadAttribute = ownerDocument.CreateAttribute("lazy-load");
            lazyLoadAttribute.Value = this.lazyLoad.ToString().ToLower();
            xmlNode.Attributes.Append(lazyLoadAttribute);

            XmlElement aliasesElement = ownerDocument.CreateElement("aliases");
            foreach (string alias in aliases) {
                XmlElement aliasElement = ownerDocument.CreateElement("alias");
                XmlAttribute aliasValueAttribute = ownerDocument.CreateAttribute("value");
                aliasValueAttribute.Value = alias;
                aliasElement.Attributes.Append(aliasValueAttribute);
                //
                aliasesElement.AppendChild(aliasElement);
            }
            XmlAttribute generatePartialAliasesAttribute = ownerDocument.CreateAttribute("generate-partial-aliases");
            generatePartialAliasesAttribute.Value = this.generatePartialAliases.ToString().ToLower();
            xmlNode.Attributes.Append(generatePartialAliasesAttribute);

            xmlNode.AppendChild(aliasesElement);
        }
#else
        public override void XmlExport(XmlNode xmlNode) {
            throw new NotImplementedException();
        }
#endif

        public override void XmlImport(XmlNode xmlNode) {
            throw new NotImplementedException();
        }

        public override string GetXmlNodeDefaultName() {
            return ("assembly");
        }
    }
}