using System;
using System.Xml;
using NBox.Utils;

namespace NBox.Config
{
    public abstract class IncludedObjectConfigBase : ISerializableToXmlNode
    {
        private readonly string id;

        public string ID {
            get {
                return (id);
            }
        }

        private readonly IncludeMethod includeMethod;

        public IncludeMethod IncludeMethod {
            get {
                return (includeMethod);
            }
        }

        private readonly string path;

        public string Path {
            get {
                return (path);
            }
        }

        private readonly CompressionConfig compressionConfig;

        public CompressionConfig CompressionConfig {
            get {
                return (compressionConfig);
            }
        }

        private readonly string copyCompressedTo;

        public string CopyCompressedTo {
            get {
                return (copyCompressedTo);
            }
        }

        protected IncludedObjectConfigBase(string id, IncludeMethod includeMethod, string path, CompressionConfig compressionConfig, string copyCompressedTo) {
            ArgumentChecker.NotNullOrEmpty(id, "id");
            ArgumentChecker.NotNullOrEmpty(path, "path");
            ArgumentChecker.NotNull(compressionConfig, "compressionConfig");
            ArgumentChecker.NotNull(copyCompressedTo, "copyCompressedTo");
            //
            this.id = id;
            this.includeMethod = includeMethod;
            this.path = path;
            this.compressionConfig = compressionConfig;
            this.copyCompressedTo = copyCompressedTo;
        }

        #region ISerializableToXmlNode Members


#if !LOADER
        public virtual void XmlExport(XmlNode xmlNode) {
            ArgumentChecker.NotNull(xmlNode, "xmlNode");
            //
            XmlDocument ownerDocument = xmlNode.OwnerDocument;

            XmlAttribute idAttribute = ownerDocument.CreateAttribute("id");
            idAttribute.Value = this.id;
            xmlNode.Attributes.Append(idAttribute);

            XmlAttribute pathAttribute = ownerDocument.CreateAttribute("path");
            pathAttribute.Value = this.path;
            xmlNode.Attributes.Append(pathAttribute);

            XmlAttribute compressionRefAttribute = ownerDocument.CreateAttribute("compression-ref");
            compressionRefAttribute.Value = this.compressionConfig.ID;
            xmlNode.Attributes.Append(compressionRefAttribute);

            if (!String.IsNullOrEmpty(this.copyCompressedTo)) {
                XmlAttribute copyCompressedToAttribute = ownerDocument.CreateAttribute("copy-compressed-to");
                copyCompressedToAttribute.Value = this.copyCompressedTo;
                xmlNode.Attributes.Append(copyCompressedToAttribute);
            }

            this.includeMethod.ExportXMLAttributes(xmlNode);
        }
#else
        public virtual void XmlExport(XmlNode xmlNode) {
            throw new NotImplementedException();
        }
#endif

        public abstract void XmlImport(XmlNode xmlNode);

        public abstract string GetXmlNodeDefaultName();

        #endregion
    }
}
