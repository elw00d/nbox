#region

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace NBox.Utils
{
    public static class ArgumentChecker
    {
        private const string CSValueCantBeNull = "Value can't be null.";
        private const string CSValueCantBeNullOrEmpty = "Value can't be null or empty string.";

        public static void NotNull(Object value, [InvokerParameterName] string parameterName) {
            if (value == null) {
                throw new ArgumentNullException(CSValueCantBeNull, parameterName);
            }
        }

        public static void NotNullOrEmpty(String value, [InvokerParameterName] string parameterName) {
            if (value == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (value.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
        }

        public static void NotNullOrEmpty(Array array, [InvokerParameterName] string parameterName) {
            if (array == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (array.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
        }

#if !LOADER

        private const string CSStreamIsEmpty = "Stream is empty.";
        private const string CSStreamIsEof = "Stream at the end.";
        private const string CSStreamIsNotReadable = "Stream is not readable.";
        private const string CSStreamIsNotWriteable = "Stream is not writeable.";

        private const string CSStringDoesNotMatchValidationExpression =
            "String is invalid and does not match validation expression.";

        private const string CSTypeValueCannotBeAbstract = "Type '{0}' is abstract. Abstract types is not allowed here.";
        private const string CSTypeValueIsNotGenericTypeDefinition = "Type '{0}' is not an generic type definition.";
        private const string CSTypeValueShouldBeAnInterface = "Type value should be an interface.";

        private const string CSTypeValueShouldInheritFrom =
            "Type '{0}' should inherit from '{1}'. Types with another base types is not allowed.";

        private const string CSTypeValueIsNotGenericTypeDefinitionAndNotInterface =
            "Type '{0}' is not an generic interface definition.";

        
        private const string CSValueIsNotSubclassOf = "Value is not an subclass of {0} type.";


        public static void NotNullAndSubclassOf(Type typeValue, Type baseType, string message,
            [InvokerParameterName] string parameterName) {
            if (typeValue == null) {
                throw new ArgumentNullException(message, parameterName);
            }
            if (!typeValue.IsSubclassOf(baseType)) {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static void NotNullAndSubclassOf(Type typeValue, Type baseType, [InvokerParameterName] string parameterName) {
            if (typeValue == null) {
                throw new ArgumentNullException(CSValueCantBeNull, parameterName);
            }
            if (!typeValue.IsSubclassOf(baseType)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        public static void NotNullAndSubclassOf(object instance, Type baseType, [InvokerParameterName] string parameterName) {
            if (instance == null) {
                throw new ArgumentNullException(CSValueCantBeNull, parameterName);
            }
            Type type = instance.GetType();
            if (!type.IsSubclassOf(baseType)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2208")]
        public static void NotNullAndSubclassOfGeneric(object instance, Type baseType,
            [InvokerParameterName] string parameterName) {
            if (instance == null) {
                throw new ArgumentNullException(CSValueCantBeNull, parameterName);
            }
            Type type = instance.GetType();
            if (!isBaseGenericType(type, baseType)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2208")]
        public static void NotNullAndSubclassOfGeneric(Type instanceType, Type baseType,
            [InvokerParameterName] string parameterName) {
            if (instanceType == null) {
                throw new ArgumentNullException(CSValueCantBeNull, parameterName);
            }
            Type type = instanceType;
            if (!isBaseGenericType(type, baseType)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        public static void NotNullAndImplementsGenericInterface(object instance, Type baseType,
            [InvokerParameterName] string parameterName) {
            if (instance == null) {
                throw new ArgumentNullException(CSValueCantBeNull, parameterName);
            }
            Type type = instance.GetType();
            if (!isBaseGenericInterface(type, baseType)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        public static void NotNullAndImplementsGenericInterface(Type instanceType, Type baseType,
            [InvokerParameterName] string parameterName) {
            if (instanceType == null) {
                throw new ArgumentNullException(CSValueCantBeNull, parameterName);
            }
            Type type = instanceType;
            if (!isBaseGenericInterface(type, baseType)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        private static bool isBaseGenericType(Type type, Type baseType) {
            Type objectType = typeof(Object);
            while (type != objectType && type != null) {
                if (type == baseType) {
                    return (true);
                }
                if (baseType.IsGenericTypeDefinition && type.IsGenericType) {
                    Type definition = type.GetGenericTypeDefinition();
                    if (definition == baseType) {
                        return (true);
                    }
                }
                if (type.Module == baseType.Module && (type.Namespace + type.Name) == (baseType.Namespace + baseType.Name) &&
                    (type.IsGenericType && baseType.IsGenericTypeDefinition)) {
                    return (true);
                }
                type = type.BaseType;
            }
            return (false);
        }

        private static bool isBaseGenericInterface(Type type, Type baseType) {
            if (baseType.IsAssignableFrom(type)) {
                return (true);
            }
            Type objectType = typeof(Object);
            while (type != objectType) {
                if (type == baseType) {
                    return (true);
                }
                foreach (Type interfaceType in type.GetInterfaces()) {
                    if (isBaseGenericType(interfaceType, baseType)) {
                        return (true);
                    }
                }
                if (type.Module == baseType.Module && (type.Namespace + type.Name) == (baseType.Namespace + baseType.Name) &&
                    (type.IsGenericType && baseType.IsGenericTypeDefinition)) {
                    return (true);
                }
                type = type.BaseType;
            }
            return (false);
        }

        public static void NotNull(Object value, string message, [InvokerParameterName] string parameterName) {
            if (value == null) {
                throw new ArgumentNullException(message, parameterName);
            }
        }

        public static void NotLessThanZero(int value, [InvokerParameterName] string parameterName) {
            if (value < 0)
                throw new ArgumentOutOfRangeException(parameterName, "Value cannot be less that zero.");
        }

        public static void NotLessThanZero(long value, [InvokerParameterName] string parameterName) {
            if (value < 0L)
                throw new ArgumentOutOfRangeException(parameterName, "Value cannot be less that zero.");
        }

        public static void NotLessThanZero(float value, [InvokerParameterName] string parameterName) {
            if (value < 0f)
                throw new ArgumentOutOfRangeException(parameterName, "Value cannot be less that zero.");
        }

        public static void NotLessThanZero(double value, [InvokerParameterName] string parameterName) {
            if (value < 0f)
                throw new ArgumentOutOfRangeException(parameterName, "Value cannot be less that zero.");
        }

        

        public static void NotNullOrEmpty(String value, string message, [InvokerParameterName] string parameterName) {
            if (value == null) {
                throw new ArgumentNullException(message, parameterName);
            }
            if (value.Length == 0) {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static void NotNullOrEmptyAndMatches(String value, string regex, [InvokerParameterName] string parameterName) {
            if (value == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (value.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (!Regex.IsMatch(value, regex)) {
                throw new ArgumentException(CSStringDoesNotMatchValidationExpression, parameterName);
            }
        }

        public static void NotNullOrEmptyAndMatches(String value, Regex regex, [InvokerParameterName] string parameterName) {
            if (value == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (value.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (!regex.IsMatch(value)) {
                throw new ArgumentException(CSStringDoesNotMatchValidationExpression, parameterName);
            }
        }

        

        public static void NotNullAndLengthNotLessThan(Array array, int minimumLength, string message,
            [InvokerParameterName] string parameterName) {
            if (array == null) {
                throw new ArgumentNullException(message, parameterName);
            }
            if (array.Length < minimumLength) {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static void NotNullOrEmpty(Array array, string message, [InvokerParameterName] string parameterName) {
            if (array == null) {
                throw new ArgumentNullException(message, parameterName);
            }
            if (array.Length == 0) {
                throw new ArgumentException(message, parameterName);
            }
        }

        private static string VisualizeInvalidCharacterPosition(string where, int position) {
            //
            StringBuilder xBuilder = new StringBuilder(2 + where.Length + 2 + position + 2);
            xBuilder.Append("\r\n");
            xBuilder.Append(where);
            xBuilder.Append("\r\n");
            for (int xI = 0; xI < position; xI++) {
                xBuilder.Append("_");
            }
            xBuilder.Append("^");
            //
            return (xBuilder.ToString());
        }

        private static string VisualizeInvalidStringPosition(string where, int position, int length) {
            //
            StringBuilder xBuilder = new StringBuilder(2 + where.Length + 2 + position + length + 2);
            xBuilder.Append("\r\n");
            xBuilder.Append(where);
            xBuilder.Append("\r\n");
            for (int xI = 0; xI < position; xI++) {
                xBuilder.Append("_");
            }
            for (int xI = 0; xI < length; xI++) {
                xBuilder.Append("^");
            }
            //
            return (xBuilder.ToString());
        }

        public static void NotNullOrEmptyFileName(String fileName, [InvokerParameterName] string parameterName) {
            if (fileName == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (fileName.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
            char[] xInvalidChars = Path.GetInvalidFileNameChars();
            foreach (char xChar in xInvalidChars) {
                int xIndex = fileName.IndexOf(xChar);
                if (xIndex >= 0) {
                    string xFormattedMessage =
                        string.Format(CultureInfo.InvariantCulture, "Invalid file name. " + "\r\n" + "Path: {0}",
                            /*string.Join(", ", Array.ConvertAll<char, string>(xInvalidChars, CharToStringConverter)),*/
                            VisualizeInvalidCharacterPosition(fileName, xIndex));
                    throw new ArgumentException(xFormattedMessage, parameterName);
                }
            }
        }

        public static void NotNullOrEmptyFilePath(string filePath, [InvokerParameterName] string parameterName) {
            if (filePath == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (filePath.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
            char[] xInvalidChars = Path.GetInvalidPathChars();
            foreach (char xChar in xInvalidChars) {
                int xIndex = filePath.IndexOf(xChar);
                if (xIndex >= 0) {
                    string xFormattedMessage =
                        string.Format(CultureInfo.InvariantCulture, "Invalid path. " + "\r\n" + "Path: {0}",
                            /*string.Join(", ", Array.ConvertAll<char, string>(xInvalidChars, CharToStringConverter)),*/
                            VisualizeInvalidCharacterPosition(filePath, xIndex));
                    throw new ArgumentException(xFormattedMessage, parameterName);
                }
            }
        }

        public static void NotNullOrEmptyFilePath(string filePath) {
            if (filePath == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty);
            }
            if (filePath.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty);
            }
            char[] xInvalidChars = Path.GetInvalidPathChars();
            foreach (char xChar in xInvalidChars) {
                int xIndex = filePath.IndexOf(xChar);
                if (xIndex >= 0) {
                    string xFormattedMessage =
                        string.Format(CultureInfo.InvariantCulture, "Invalid path. " + "\r\n" + "Path: {0}",
                            /*string.Join(", ", Array.ConvertAll<char, string>(xInvalidChars, CharToStringConverter)),*/
                            VisualizeInvalidCharacterPosition(filePath, xIndex));
                    throw new ArgumentException(xFormattedMessage);
                }
            }
        }

        public static void NotNullOrEmptyExistsFilePath(string filePath, [InvokerParameterName] string parameterName) {
            if (filePath == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (filePath.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
            char[] xInvalidChars = Path.GetInvalidPathChars();
            foreach (char xChar in xInvalidChars) {
                int xIndex = filePath.IndexOf(xChar);
                if (xIndex >= 0) {
                    string xFormattedMessage =
                        string.Format(CultureInfo.InvariantCulture, "Invalid path. " + "\r\n" + "Path: {0}",
                            /*string.Join(", ", Array.ConvertAll<char, string>(xInvalidChars, CharToStringConverter)),*/
                            VisualizeInvalidCharacterPosition(filePath, xIndex));
                    throw new ArgumentException(xFormattedMessage, parameterName);
                }
            }
            try {
                if (!File.Exists(filePath)) {
                    throw new FileNotFoundException("File not found. File path : '" + filePath + "'");
                }
            } catch (ArgumentException) {
                // invalid file name!
            }
        }

        public static void NotNullOrEmptyExistsFilePath(string filePath) {
            if (filePath == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty);
            }
            if (filePath.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty);
            }
            char[] xInvalidChars = Path.GetInvalidPathChars();
            foreach (char xChar in xInvalidChars) {
                int xIndex = filePath.IndexOf(xChar);
                if (xIndex >= 0) {
                    string xFormattedMessage =
                        string.Format(CultureInfo.InvariantCulture, "Invalid path. " + "\r\n" + "Path: {0}",
                            /*string.Join(", ", Array.ConvertAll<char, string>(xInvalidChars, CharToStringConverter)),*/
                            VisualizeInvalidCharacterPosition(filePath, xIndex));
                    throw new ArgumentException(xFormattedMessage);
                }
            }
            try {
                if (!File.Exists(filePath)) {
                    throw new FileNotFoundException("File not found. File path : '" + filePath + "'");
                }
            } catch (ArgumentException) {
                // invalid file name!
            }
        }

        public static void NotNullOrEmptyExistsDirectoryPath(string directoryPath,
            [InvokerParameterName] string parameterName) {
            if (directoryPath == null) {
                throw new ArgumentNullException(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (directoryPath.Length == 0) {
                throw new ArgumentException(CSValueCantBeNullOrEmpty, parameterName);
            }
            char[] xInvalidChars = Path.GetInvalidPathChars();
            foreach (char xChar in xInvalidChars) {
                int xIndex = directoryPath.IndexOf(xChar);
                if (xIndex >= 0) {
                    string xFormattedMessage =
                        string.Format(CultureInfo.InvariantCulture, "Invalid path. " + "\r\n" + "Path: {0}",
                            /*string.Join(", ", Array.ConvertAll<char, string>(xInvalidChars, CharToStringConverter)),*/
                            VisualizeInvalidCharacterPosition(directoryPath, xIndex));
                    throw new ArgumentException(xFormattedMessage, parameterName);
                }
            }
            if (!Directory.Exists(directoryPath)) {
                //
                bool xPathExists = false;
                string xPath = directoryPath;
                try {
                    while (!xPathExists) {
                        xPath = Path.GetDirectoryName(xPath);
                        xPathExists = Directory.Exists(xPath);
                    }
                } catch (ArgumentException) {
                }
                //
                int xIndex = -1;
                try {
                    xIndex = directoryPath.IndexOf(Path.DirectorySeparatorChar, xPath.Length + 1);
                } catch (ArgumentOutOfRangeException) {
                }
                if (xIndex < 0) {
                    xIndex = directoryPath.Length - xPath.Length;
                } else {
                    xIndex = directoryPath.Length - xIndex - 3;
                }
                //
                throw new DirectoryNotFoundException(
                    string.Format(CultureInfo.InvariantCulture, "Directory not found. Directory: {0}",
                        VisualizeInvalidStringPosition(directoryPath, xPath.Length + 1, xIndex)));
            }
        }

        public static void ValidEnumerationValue<TType>(TType value, [InvokerParameterName] string parameterName)
            where TType : struct {
            if (!Enum.IsDefined(value.GetType(), value)) {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value of enumeration is out of range.");
            }
        }

        public static void NotNullWritableStream(Stream document, [InvokerParameterName] string parameterName) {
            NotNull(document, parameterName);
            if (!document.CanWrite) {
                throw new ArgumentException(CSStreamIsNotWriteable, parameterName);
            }
        }

        public static void NotNullReadableNotEmptyStream(Stream document, [InvokerParameterName] string parameterName) {
            NotNull(document, parameterName);
            if (!document.CanRead) {
                throw new ArgumentException(CSStreamIsNotReadable, parameterName);
            }
            if (document.Length == 0) {
                throw new ArgumentException(CSStreamIsEmpty, parameterName);
            }
            if ((document.Length - document.Position) == 0) {
                throw new ArgumentException(CSStreamIsEof, parameterName);
            }
        }

        public static void NotNullInterface(Type type, [InvokerParameterName] string parameterName) {
            NotNull(type, parameterName);
            if (!type.IsInterface) {
                throw new ArgumentException(CSTypeValueShouldBeAnInterface, parameterName);
            }
        }

        public static void NotNullInterface(Type type, string message, [InvokerParameterName] string parameterName) {
            NotNull(type, message, parameterName);
            if (!type.IsInterface) {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static void NotNullInterfaceAssignableTo(Type type, Type assignableTo,
            [InvokerParameterName] string parameterName) {
            NotNull(type, parameterName);
            if (!type.IsInterface) {
                throw new ArgumentException(CSTypeValueShouldBeAnInterface, parameterName);
            }
            if (!assignableTo.IsAssignableFrom(type)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    CSTypeValueShouldInheritFrom, type, assignableTo), parameterName);
            }
        }

        public static void NotNullAssignableTo(Object instance, Type assignableTo, [InvokerParameterName] string parameterName) {
            NotNull(instance, parameterName);
            Type type = instance.GetType();
            if (!assignableTo.IsAssignableFrom(type)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    CSTypeValueShouldInheritFrom, type, assignableTo), parameterName);
            }
        }

        public static void NotNullAssignableTo(Type instanceType, Type assignableTo,
            [InvokerParameterName] string parameterName) {
            NotNull(instanceType, parameterName);
            Type type = instanceType;
            if (!assignableTo.IsAssignableFrom(type)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    CSTypeValueShouldInheritFrom, type, assignableTo), parameterName);
            }
        }

        public static void NotNullInterfaceAssignableTo(Type type, Type assignableFrom, string message,
            [InvokerParameterName] string parameterName) {
            NotNull(type, message, parameterName);
            if (!type.IsInterface) {
                throw new ArgumentException(message, parameterName);
            }
            if (!assignableFrom.IsAssignableFrom(type)) {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static void NotNullNonAbstract(Type type, [InvokerParameterName] string parameterName) {
            NotNull(type, parameterName);
            if (type.IsAbstract) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CSTypeValueCannotBeAbstract,
                    type), parameterName);
            }
        }

        public static void NotNullGenericTypeDefinition(Type type, [InvokerParameterName] string parameterName) {
            NotNull(type, "type");
            if (!type.IsGenericTypeDefinition) {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, CSTypeValueIsNotGenericTypeDefinition, type), parameterName);
            }
        }

        public static void NotNullGenericInterfaceDefinition(Type type, [InvokerParameterName] string parameterName) {
            NotNull(type, "type");
            if (!type.IsGenericTypeDefinition || !type.IsInterface) {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, CSTypeValueIsNotGenericTypeDefinitionAndNotInterface, type), parameterName);
            }
        }

#endif
    }
}