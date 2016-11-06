using System;
using System.IO;
using System.Text;

namespace Simple1C.AnalysisHost
{
    public static class StreamExtenstions
    {
        public static byte[] ReadToEnd(this Stream stream)
        {
            var buffer = new byte[1024 * 64];
            var offset = 0;
            while (true)
            {
                var read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read > 0)
                {
                    offset += read;
                    var newBuffer = new byte[buffer.Length * 2];
                    Buffer.BlockCopy(buffer, 0, newBuffer, 0, offset);
                    buffer = newBuffer;
                }
                else break;
            }
            if (offset >= buffer.Length)
                return buffer;
            var result = new byte[offset];
            Buffer.BlockCopy(buffer, 0, result, 0, offset);
            return result;
        }

        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static string ReadString(this Stream stream)
        {
            var bytes = stream.ReadToEnd();
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static void WriteString(this Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}