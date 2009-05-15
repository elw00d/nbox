using System;
using System.IO;
using NBox.Config;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace NBox.Utils
{
    public static class LzmaHelper
    {
#if !(LOADER)

        public static byte[] Encode(CompressionConfig compressionConfig, byte[] data, long dataLength, out long encodedDataLength) {
            ArgumentChecker.NotNull(compressionConfig, "compressionConfig");
            ArgumentChecker.NotNull(data, "data");
            //
            using (MemoryStream outStream = new MemoryStream()) {
                using (Stream inStream = new MemoryStream(data)) {
                    const Int32 dictionary = 1 << 21;
                    const string mf = "bt4";
                    const bool eos = false;
                    const Int32 posStateBits = 2;
                    const Int32 litContextBits = 3;
                    const Int32 litPosBits = 0;
                    const Int32 algorithm = 2;
                    const Int32 numFastBytes = 128;

                    CoderPropID[] propIDs = new CoderPropID[] {
                        CoderPropID.DictionarySize,
                        CoderPropID.PosStateBits,
                        CoderPropID.LitContextBits,
                        CoderPropID.LitPosBits,
                        CoderPropID.Algorithm,
                        CoderPropID.NumFastBytes,
                        CoderPropID.MatchFinder,
                        CoderPropID.EndMarker
                    };
                    object[] properties = new object[] {
                        dictionary,
                        posStateBits,
                        litContextBits,
                        litPosBits,
                        algorithm,
                        numFastBytes,
                        mf,
                        eos
                    };
                    Encoder encoder = new Encoder();
                    encoder.SetCoderProperties(propIDs, properties);
                    encoder.WriteCoderProperties(outStream);
                    //
                    for (int i = 0; i < 8; i++) {
                        outStream.WriteByte((Byte) (dataLength >> (8 * i)));
                    }
                    //

                    encoder.Code(inStream, outStream, dataLength, -1, null);
                }
                //
                encodedDataLength = outStream.Length;
                return (outStream.GetBuffer());
            }
        }

#endif

        public static byte[] Decode(byte[] data, long dataLength, out long decodedDataLength) {
            ArgumentChecker.NotNullOrEmpty(data, "data");
            //
            using (MemoryStream inStream = new MemoryStream(data)) {
                byte[] propertiesSignature = new byte[5];
                if (5 != inStream.Read(propertiesSignature, 0, 5)) {
                    throw new InvalidOperationException("Cannot read LZMA encoder properties stamp.");
                }
                //
                Decoder decoder = new Decoder();
                decoder.SetDecoderProperties(propertiesSignature);
                long _decodedDataLength = 0;
                for (int i = 0; i < 8; i++) {
                    Int32 byteReaded;
                    if (0 > (byteReaded = inStream.ReadByte())) {
                        throw new InvalidOperationException("Cannot read length stamp.");
                    }
                    _decodedDataLength |= ((long) byteReaded) << (8 * i);
                }
                //
                decodedDataLength = _decodedDataLength;
                using (MemoryStream outStream = new MemoryStream()) {
                    decoder.Code(inStream, outStream, data.Length, _decodedDataLength, null);
                    //
                    return (outStream.GetBuffer());
                }
            }
        }
    }
}