using System;
using System.Xml;
using NBox.Utils;

namespace NBox.Config
{
    public enum IncludeMethodKind
    {
        File = 1,
        Overlay = 2,
        Resource = 3
    }

    public sealed class IncludeMethod
    {
        private readonly IncludeMethodKind includeMethodKind;

        public IncludeMethodKind IncludeMethodKind {
            get {
                return (includeMethodKind);
            }
        }

        private string fileLoadFromPath;

        public string FileLoadFromPath {
            get {
                if (includeMethodKind != IncludeMethodKind.File) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                return (fileLoadFromPath);
            }
            set {
                if (includeMethodKind != IncludeMethodKind.File) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                fileLoadFromPath = value;
            }
        }

        private int overlayOffset;

        public int OverlayOffset {
            get {
                if (includeMethodKind != IncludeMethodKind.Overlay) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                return (overlayOffset);
            }
            set {
                if (includeMethodKind != IncludeMethodKind.Overlay) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                overlayOffset = value;
            }
        }

        private int overlayLength;

        public int OverlayLength {
            get {
                if (includeMethodKind != IncludeMethodKind.Overlay) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                return (overlayLength);
            }
            set {
                if (includeMethodKind != IncludeMethodKind.Overlay) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                overlayLength = value;
            }
        }

        private string resourceName;

        public string ResourceName {
            get {
                if (includeMethodKind != IncludeMethodKind.Resource) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                return (resourceName);
            }
            set {
                if (includeMethodKind != IncludeMethodKind.Resource) {
                    throw new InvalidOperationException("Type mismatch.");
                }
                //
                resourceName = value;
            }
        }

        public IncludeMethod(IncludeMethodKind includeMethodKind) {
            this.includeMethodKind = includeMethodKind;
        }

        public IncludeMethod(IncludeMethodKind includeMethodKind, string fileLoadFromPathOrResourceName) {
            if ((includeMethodKind != IncludeMethodKind.File) && (includeMethodKind != IncludeMethodKind.Resource)) {
                throw new InvalidOperationException("Invalid include method for this arguments set. Use alternative constructor.");
            }
            if (fileLoadFromPathOrResourceName == null) {
                throw new ArgumentNullException("fileLoadFromPathOrResourceName");
            }
            if (includeMethodKind == IncludeMethodKind.File && fileLoadFromPathOrResourceName.Length == 0) {
                throw new ArgumentException("String is empty.", "fileLoadFromPathOrResourceName");
            }
            //
            this.includeMethodKind = includeMethodKind;
            if (includeMethodKind == IncludeMethodKind.File) {
                fileLoadFromPath = fileLoadFromPathOrResourceName;
            } else {
                resourceName = fileLoadFromPathOrResourceName;
            }
        }

        public IncludeMethod(IncludeMethodKind includeMethodKind, int overlayOffset, int overlayLength) {
            if (includeMethodKind != IncludeMethodKind.Overlay) {
                throw new InvalidOperationException("Invalid include method for this arguments set. Use alternative constructor.");
            }
            //
            this.includeMethodKind = IncludeMethodKind.Overlay;
            this.overlayOffset = overlayOffset;
            this.overlayLength = overlayLength;
        }

        public static IncludeMethod Parse(XmlNode includedObjectXmlNode) {
            ArgumentChecker.NotNull(includedObjectXmlNode, "includedObjectXmlNode");
            //
            IncludeMethod includeMethod;
            string includeMethodAttributeValue = includedObjectXmlNode.Attributes["include-method"].Value;
            IncludeMethodKind includeMethodKind = (IncludeMethodKind) Enum.Parse(typeof(IncludeMethodKind), includeMethodAttributeValue, true);
            //
            switch (includeMethodKind) {
                case IncludeMethodKind.File: {
                    XmlAttribute fileLoadFromPathAttribute = includedObjectXmlNode.Attributes["file-load-from-path"];
                    if (fileLoadFromPathAttribute == null) {
                        throw new InvalidOperationException("You have to specify file-load-from-path attribute for selected include method.");
                    }
                    includeMethod = new IncludeMethod(IncludeMethodKind.File, fileLoadFromPathAttribute.Value);
                    break;
                }
                case IncludeMethodKind.Resource: {
                    XmlAttribute resourceNameAttribute = includedObjectXmlNode.Attributes["resource-name"];
                    if (resourceNameAttribute == null) {
                        includeMethod = new IncludeMethod(IncludeMethodKind.Resource, String.Empty);
                    } else {
                        includeMethod = new IncludeMethod(IncludeMethodKind.Resource, resourceNameAttribute.Value);
                    }
                    break;
                }
                case IncludeMethodKind.Overlay: {
                    XmlAttribute overlayOffsetAttribute = includedObjectXmlNode.Attributes["overlay-offset"];
                    XmlAttribute overlayLengthAttribute = includedObjectXmlNode.Attributes["overlay-length"];
                    includeMethod = new IncludeMethod(IncludeMethodKind.Overlay,
                        overlayOffsetAttribute != null ? int.Parse(overlayOffsetAttribute.Value) : 0,
                        overlayLengthAttribute != null ? int.Parse(overlayLengthAttribute.Value) : 0);
                    break;
                }
                default: {
                    throw new NotSupportedException("This value of enumeration is not supported.");
                }
            }
            //
            return (includeMethod);
        }

#if !LOADER
        public void ExportXMLAttributes(XmlNode xmlNode) {
            ArgumentChecker.NotNull(xmlNode, "xmlNode");
            //
            XmlDocument ownerDocument = xmlNode.OwnerDocument;
            XmlAttribute includeMethodKindAttribute = ownerDocument.CreateAttribute("include-method");
            includeMethodKindAttribute.Value = Convert.ToString(this.includeMethodKind);
            xmlNode.Attributes.Append(includeMethodKindAttribute);
            //
            switch (this.includeMethodKind) {
                case IncludeMethodKind.File: {
                    XmlAttribute fileLoadFromPathAttribute = ownerDocument.CreateAttribute("file-load-from-path");
                    fileLoadFromPathAttribute.Value = this.fileLoadFromPath;
                    xmlNode.Attributes.Append(fileLoadFromPathAttribute);
                    break;
                }
                case IncludeMethodKind.Overlay: {
                    XmlAttribute overlayOffsetAttribute = ownerDocument.CreateAttribute("overlay-offset");
                    overlayOffsetAttribute.Value = this.overlayOffset.ToString();
                    xmlNode.Attributes.Append(overlayOffsetAttribute);
                    //
                    XmlAttribute overlayLengthAttribute = ownerDocument.CreateAttribute("overlay-length");
                    overlayLengthAttribute.Value = this.overlayLength.ToString();
                    xmlNode.Attributes.Append(overlayLengthAttribute);
                    break;
                }
                case IncludeMethodKind.Resource: {
                    XmlAttribute resourceNameAttribute = ownerDocument.CreateAttribute("resource-name");
                    resourceNameAttribute.Value = this.resourceName;
                    xmlNode.Attributes.Append(resourceNameAttribute);
                    break;
                }
            }
        }

        public override string ToString() {
            if ((includeMethodKind == IncludeMethodKind.File) || (includeMethodKind == IncludeMethodKind.Resource)) {
                return (String.Format("{0} Kind={1} Source={2}", typeof (IncludeMethod), includeMethodKind,
                    includeMethodKind == IncludeMethodKind.File ? fileLoadFromPath : resourceName));
            }
            //
            return (String.Format("{0} Kind={1} Offset={2} Length={3}", typeof (IncludeMethod), includeMethodKind,
                overlayOffset, overlayLength));
        }
#else
        public void ExportXMLAttributes(XmlNode xmlNode) {
            throw new NotImplementedException();
        }
#endif
    }
}