using System;
using System.Xml;
using NBox.Utils;

namespace NBox.Config
{
    public enum OverwritingOptions
    {
        Always = 1,
        CheckExist = 2,
        CheckSize = 3,
        Never = 4
    }

    public sealed class IncludedFileConfig : IncludedObjectConfigBase
    {
        public const OverwritingOptions OVERWRITE_ON_EXTRACT_DEFAULT = OverwritingOptions.Always;

        private readonly string extractToPath;

        public string ExtractToPath {
            get {
                return (extractToPath);
            }
        }

        private readonly OverwritingOptions overwriteOnExtract;

        public OverwritingOptions OverwriteOnExtract {
            get {
                return (overwriteOnExtract);
            }
        }

        public IncludedFileConfig(string id, IncludeMethod includeMethod, string path, CompressionConfig compressionConfig, string copyCompressedTo,
            string extractToPath, OverwritingOptions overwriteOnExtract)
            : base(id, includeMethod, path, compressionConfig, copyCompressedTo) {
            ArgumentChecker.NotNullOrEmpty(extractToPath, "extractToPath");
            //
            this.extractToPath = extractToPath;
            this.overwriteOnExtract = overwriteOnExtract;
        }

        public override void XmlImport(XmlNode xmlNode) {
            throw new NotImplementedException();
        }

#if !LOADER
        public override void XmlExport(XmlNode xmlNode) {
            base.XmlExport(xmlNode);
            //
            XmlDocument ownerDocument = xmlNode.OwnerDocument;

            XmlAttribute extractToPathAttribute = ownerDocument.CreateAttribute("extract-to-path");
            extractToPathAttribute.Value = this.extractToPath;
            xmlNode.Attributes.Append(extractToPathAttribute);

            XmlAttribute overwriteOnExtractAttribute = ownerDocument.CreateAttribute("overwrite-on-extracting");
            overwriteOnExtractAttribute.Value = Convert.ToString(this.overwriteOnExtract);
            xmlNode.Attributes.Append(overwriteOnExtractAttribute);
        }
#else
        public override void XmlExport(XmlNode xmlNode) {
            throw new NotImplementedException();
        }
#endif

        public override string GetXmlNodeDefaultName() {
            return ("file");
        }
    }
}