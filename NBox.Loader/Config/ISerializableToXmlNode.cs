using System.Xml;

namespace NBox.Config
{
    public interface ISerializableToXmlNode
    {
        void XmlExport(XmlNode xmlNode);

        void XmlImport(XmlNode xmlNode);

        string GetXmlNodeDefaultName();
    }
}
