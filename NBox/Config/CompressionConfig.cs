using System;
using System.Xml;

namespace NBox.Config
{
    public enum CompressionLevel
    {
        Store,
        Fastest,
        Fast,
        Normal,
        Maximum,
        Ultra
    }

    public class CompressionConfig : ISerializableToXmlNode
    {
        private readonly string id;

        public string ID {
            get {
                return (id);
            }
        }

        private readonly CompressionLevel compressionLevel;

        public CompressionLevel CompressionLevel {
            get {
                return (compressionLevel);
            }
        }

        public CompressionConfig(string id, CompressionLevel compressionLevel) {
            if (id == null) {
                throw new ArgumentNullException("id");
            }
            if (id.Length == 0) {
                throw new ArgumentException("String is empty.", "id");
            }
            //
            this.id = id;
            this.compressionLevel = compressionLevel;
        }

        #region ISerializableToXmlNode Members

        public void XmlImport(XmlNode xmlNode) {
            throw new NotImplementedException();
        }

        public void XmlExport(XmlNode xmlNode) {
            XmlDocument ownerDocument = xmlNode.OwnerDocument;
            //
            XmlAttribute idAttribute = ownerDocument.CreateAttribute("id");
            idAttribute.Value = this.id;
            xmlNode.Attributes.Append(idAttribute);
            //
            XmlElement levelElement = ownerDocument.CreateElement("level");
            XmlAttribute levelValueAttribute = ownerDocument.CreateAttribute("value");
            levelValueAttribute.Value = Convert.ToString(this.compressionLevel);
            levelElement.Attributes.Append(levelValueAttribute);
            //
            xmlNode.AppendChild(levelElement);
        }

        public string GetXmlNodeDefaultName() {
            return ("compression-option");
        }

        #endregion
    }
}