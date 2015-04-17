// UploadService.cs
// 
// Author:
//       Ventsislav Mladenov
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2015 Ventsislav Mladenov
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Core.Services;
using MonoDevelop.VersionControl.TFS.Core.Services.Resolvers;
using MonoDevelop.VersionControl.TFS.VersionControl.Infrastructure;
using MonoDevelop.VersionControl.TFS.VersionControl.Services.Resolvers;

namespace MonoDevelop.VersionControl.TFS.VersionControl.Services
{
    [ServiceResolver(typeof(UploadServiceResolver))]
    internal sealed class UploadService: TFSService
    {
        private UploadService(Uri baseUri, string servicePath)
            : base(baseUri, servicePath)
        {
            
        }

        const string NewLine = "\r\n";
        const string Boundary = "----------------------------8e5m2D6l5Q4h6";
        const int ChunkSize = 512 * 1024; //Chunk Size 512 K
        private static readonly string uncompressedContentType = "application/octet-stream";
//        private static readonly string compressedContentType = "application/gzip";



        #region implemented abstract members of TfsService

        public override XNamespace MessageNs
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IServiceResolver ServiceResolver
        {
            get
            {
                return new UploadServiceResolver();
            }
        }

        #endregion

        private void CopyStream(Stream source, Stream destination)
        {
            byte[] buffer = new byte[ChunkSize];
            int cnt;
            while((cnt = source.Read(buffer, 0, ChunkSize)) > 0)
            {
                destination.Write(buffer, 0, cnt);
            }
            destination.Flush();
        }

        private void CopyBytes(byte[] source, Stream destination)
        {
            using (var memorySource = new MemoryStream(source))
            {
                CopyStream(memorySource, destination);
            }
        }

        public async Task UploadFileAsync(string workspaceName, string workspaceOwner, CommitItem item)
        {
            var fileContent = File.ReadAllBytes(item.LocalPath);
            var fileHash = Hash(fileContent);
            string contentType = uncompressedContentType;

            using (var memory = new MemoryStream(fileContent))
            {
                byte[] buffer = new byte[ChunkSize];
                int cnt;
                while((cnt = memory.Read(buffer, 0, ChunkSize)) > 0)
                {
                    var range = GetRange(memory.Position - cnt, memory.Position, fileContent.Length);
                    await UploadPart(item.RepositoryPath, workspaceName, workspaceOwner, fileContent.Length, fileHash, range, contentType, buffer, cnt);
                }
            }
        }

        private async Task<HttpResponseMessage> UploadPart(string fileName, string workspaceName, string workspaceOwner, int fileSize, string fileHash, string range, string contentType, byte[] bytes, int copyBytes)
        {
            var handler = new HttpClientHandler { PreAuthenticate = true };
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, this.Url);
            this.Server.Authorization.Authorize(handler, message);

            HttpClient client = new HttpClient(handler);
            
            
            var content = new MultipartFormDataContent(Boundary);
            content.Add(new StringContent(fileName), "item");
            content.Add(new StringContent(workspaceName), "wsname");
            content.Add(new StringContent(workspaceOwner), "wsowner");
            content.Add(new StringContent(fileSize.ToString()), "filelength");
            content.Add(new StringContent(fileHash), "hash");
            content.Add(new StringContent(range), "range");
            content.Add(new StringContent("Content-Type: " + contentType), "content", "item");
            content.Add(new ByteArrayContent(bytes, 0, copyBytes));

            message.Content = content;
            return await client.SendAsync(message);


//            var request = (HttpWebRequest)WebRequest.Create(this.Url);
//            request.Method = "POST";
//            this.Server.Authorization.Authorize(request);
//            request.AllowWriteStreamBuffering = true;
//            request.ContentType = "multipart/form-data; boundary=" + Boundary.Substring(2);
//
//            var template = GetTemplate();
//            var content = string.Format(template, fileName, workspaceName, workspaceOwner, fileSize, fileHash, range, "item", contentType);
//            var contentBytes = Encoding.UTF8.GetBytes(content.Replace(Environment.NewLine, NewLine));
//
//            using (var stream = new MemoryStream())
//            {
//                stream.Write(contentBytes, 0, contentBytes.Length);
//                stream.Write(bytes, 0, copyBytes);
//                var footContent = Encoding.UTF8.GetBytes(NewLine + Boundary + "--" + NewLine);
//                stream.Write(footContent, 0, footContent.Length);
//                stream.Flush();
//                contentBytes = stream.ToArray();
//            }
//
//            //request.ContentLength = contentBytes.Length;
//
//            using (var requestStream = request.GetRequestStream())
//            {
//                CopyBytes(contentBytes, requestStream);
//            }
//                           
//            request.GetResponse();
        }

        private string GetTemplate()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("item");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{0}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("wsname");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{1}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("wsowner");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{2}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("filelength");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{3}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("hash");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{4}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("range");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{5}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("content");
            builder.Append("\"; filename=\"");
            builder.Append("{6}");
            builder.AppendLine("\"");
            builder.Append("Content-Type: ");
            builder.Append("{7}");
            builder.AppendLine();
            builder.AppendLine();
//			builder.Append("{8}");
//			builder.AppendLine();
//			builder.Append("----------------------------8e5m2D6l5Q4h6");
//			builder.AppendLine("--");
            return builder.ToString();
        }

        private byte[] Compress(byte[] input)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (GZipStream stream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    stream.Write(input, 0, input.Length);
                    stream.Flush();
                }
                return memoryStream.ToArray();
            }
        }

        private string Hash(byte[] input)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return Convert.ToBase64String(md5.ComputeHash(input));
            }

        }

        private string GetRange(long start, long end, long length)
        {
            var builder = new StringBuilder(100);
            builder.Append("bytes=");
            builder.Append(start);
            builder.Append('-');
            builder.Append(end - 1);
            builder.Append('/');
            builder.Append(length);
            builder.AppendLine();
            return builder.ToString();
        }
    }
}

