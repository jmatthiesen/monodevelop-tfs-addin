// UpdateLocalVersion.cs
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

using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.VersionControl.Infrastructure;

namespace MonoDevelop.VersionControl.TFS.VersionControl.Models
{
    internal sealed class UpdateLocalVersion
    {
        public UpdateLocalVersion(int itemId, LocalPath targetLocalItem, int localVersion)
        {
            this.ItemId = itemId;
            this.TargetLocalItem = targetLocalItem;
            this.LocalVersion = localVersion;
        }

        public int ItemId { get; private set; }

        public LocalPath TargetLocalItem { get; private set; }

        public int LocalVersion { get; private set; }

        public XElement ToXml()
        {
            var el = new XElement("LocalVersionUpdate",
                         new XAttribute("itemid", ItemId),
                         new XAttribute("lver", LocalVersion));
            if (!TargetLocalItem.IsEmpty)
                el.Add(new XAttribute("tlocal", TargetLocalItem.ToRepositoryLocalPath()));
            return el;
        }
    }
    
}