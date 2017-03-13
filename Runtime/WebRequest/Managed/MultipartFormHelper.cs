// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Networking
{
    public interface IMultipartFormSection
    {
        string sectionName { get; }
        byte[] sectionData { get; }
        string fileName { get; } // return null if not a file section
        string contentType { get; }
    }

    public class MultipartFormDataSection : IMultipartFormSection
    {
        private string name;
        private byte[] data;
        private string content;

        public MultipartFormDataSection(string name, byte[] data, string contentType)
        {
            if (data == null || data.Length < 1)
            {
                throw new ArgumentException("Cannot create a multipart form data section without body data");
            }

            this.name = name;
            this.data = data;
            this.content = contentType;
        }

        public MultipartFormDataSection(string name, byte[] data) : this(name, data, null)
        {}

        public MultipartFormDataSection(byte[] data) : this(null, data)
        {}

        public MultipartFormDataSection(string name, string data, System.Text.Encoding encoding, string contentType)
        {
            if (data == null || data.Length < 1)
            {
                throw new ArgumentException("Cannot create a multipart form data section without body data");
            }

            byte[] dataBytes = encoding.GetBytes(data);

            this.name = name;
            this.data = dataBytes;

            if (contentType != null && !contentType.Contains("encoding="))
            {
                contentType = contentType.Trim() + "; encoding=" + encoding.WebName;
            }

            this.content = contentType;
        }

        public MultipartFormDataSection(string name, string data, string contentType) : this(name, data, System.Text.Encoding.UTF8, contentType)
        {}

        public MultipartFormDataSection(string name, string data) : this(name, data, "text/plain")
        {}

        public MultipartFormDataSection(string data) : this(null, data)
        {}

        public string sectionName { get { return this.name; } }
        public byte[] sectionData { get { return this.data; } }
        public string fileName { get { return null; } }
        public string contentType { get { return this.content; } }
    }

    public class MultipartFormFileSection : IMultipartFormSection
    {
        private string name;
        private byte[] data;
        private string file;
        private string content;

        private void Init(string name, byte[] data, string fileName, string contentType)
        {
            this.name = name;
            this.data = data;
            this.file = fileName;
            this.content = contentType;
        }

        public MultipartFormFileSection(string name, byte[] data, string fileName, string contentType)
        {
            if (data == null || data.Length < 1)
            {
                throw new ArgumentException("Cannot create a multipart form file section without body data");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "file.dat";
            }

            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/octet-stream";
            }

            Init(name, data, fileName, contentType);
        }

        public MultipartFormFileSection(byte[] data) : this(null, data, null, null)
        {}

        public MultipartFormFileSection(string fileName, byte[] data) : this(null, data, fileName, null)
        {}

        // String upload functions, for convenience
        public MultipartFormFileSection(string name, string data, System.Text.Encoding dataEncoding, string fileName)
        {
            if (data == null || data.Length < 1)
            {
                throw new ArgumentException("Cannot create a multipart form file section without body data");
            }

            if (dataEncoding == null)
            {
                dataEncoding = System.Text.Encoding.UTF8;
            }

            byte[] dataBytes = dataEncoding.GetBytes(data);

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "file.txt";
            }

            if (string.IsNullOrEmpty(this.content))
            {
                this.content = "text/plain; charset=" + dataEncoding.WebName;
            }

            Init(name, dataBytes, fileName, this.content);
        }

        public MultipartFormFileSection(string data, System.Text.Encoding dataEncoding, string fileName) : this(null, data, dataEncoding, fileName)
        {}

        public MultipartFormFileSection(string data, string fileName) : this(data, null, fileName)
        {}

        public string sectionName { get { return this.name; } }
        public byte[] sectionData { get { return this.data; } }
        public string fileName { get { return this.file; } }
        public string contentType { get { return this.content; } }
    }
}
