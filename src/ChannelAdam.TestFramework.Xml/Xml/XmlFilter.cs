//-----------------------------------------------------------------------
// <copyright file="XmlFilter.cs">
//     Copyright (c) 2016-2018 Adam Craven. All rights reserved.
// </copyright>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

namespace ChannelAdam.TestFramework.Xml
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Abstractions;

    public class XmlFilter : IXmlFilter
    {
        #region Public Constructors

        public XmlFilter() : this(new List<string>(), new List<string>())
        {
        }

        public XmlFilter(IList<string> elementLocalNamesToIgnore)
            : this(elementLocalNamesToIgnore, new List<string>())
        {
        }

        public XmlFilter(IList<string> elementLocalNamesToIgnore, IList<string> xpathsToIgnore)
        {
            this.ElementLocalNamesToIgnore = elementLocalNamesToIgnore;
            this.XPathsToIgnore = xpathsToIgnore;
        }

        #endregion Public Constructors

        #region Public Properties

        public IList<string> ElementLocalNamesToIgnore { get; }

        public IList<string> XPathsToIgnore { get; }

        #endregion Public Properties

        #region Public Methods

        public XElement ApplyFilterTo(XElement xmlElement)
        {
            var result = new XElement(xmlElement);

            if (this.ElementLocalNamesToIgnore.Count > 0)
            {
                result.DescendantsAndSelf().Where(p => this.ElementLocalNamesToIgnore.Contains(p.Name.LocalName)).Remove();
            }

            if (this.XPathsToIgnore.Count > 0)
            {
                foreach (var xpath in this.XPathsToIgnore)
                {
                    result.XPathSelectElements(xpath)?.Remove();
                }
            }

            return result;
        }

        public bool HasFilters()
        {
            return this.ElementLocalNamesToIgnore.Count > 0 || this.XPathsToIgnore.Count > 0;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (this.ElementLocalNamesToIgnore.Count > 0)
            {
                sb.Append("XML elements with the following Local Names will be ignored: ");
                sb.AppendLine(string.Join(", ", this.ElementLocalNamesToIgnore));
            }

            if (this.XPathsToIgnore.Count > 0)
            {
                sb.Append("XML nodes satisfying the following XPath expressions will be ignored: ");
                sb.AppendLine(string.Join(", ", this.XPathsToIgnore));
            }

            return sb.ToString();
        }

        #endregion Public Methods
    }
}