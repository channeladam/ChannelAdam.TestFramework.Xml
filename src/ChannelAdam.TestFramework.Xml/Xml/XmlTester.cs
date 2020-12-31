//-----------------------------------------------------------------------
// <copyright file="XmlTester.cs">
//     Copyright (c) 2016-2021 Adam Craven. All rights reserved.
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
    using System;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    using Abstractions;
    using ChannelAdam.Logging;
    using ChannelAdam.Logging.Abstractions;
    using ChannelAdam.Reflection;
    using ChannelAdam.TestFramework.Abstractions;
    using ChannelAdam.Xml;
    using Org.XmlUnit.Builder;
    using Org.XmlUnit.Diff;

    /// <summary>
    /// A helper class for testing differences between two XML sources.
    /// </summary>
    public class XmlTester
    {
        #region Fields

        private readonly ISimpleLogger logger;
        private readonly ILogAsserter logAssert;
        private readonly IComparisonFormatter comparisonFormatter;

        private XElement? actualXml;
        private XElement? expectedXml;
        private Diff? differences;

        #endregion

        #region Constructors

        public XmlTester(ILogAsserter logAsserter) : this(new SimpleConsoleLogger(), logAsserter)
        {
        }

        public XmlTester(ISimpleLogger logger, ILogAsserter logAsserter) : this(logger, logAsserter, new DefaultComparisonFormatter())
        {
        }

        public XmlTester(ILogAsserter logAsserter, IComparisonFormatter comparisonFormatter) : this(new SimpleConsoleLogger(), logAsserter, comparisonFormatter)
        {
        }

        public XmlTester(ISimpleLogger logger, ILogAsserter logAsserter, IComparisonFormatter comparisonFormatter)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.logAssert = logAsserter ?? throw new ArgumentNullException(nameof(logAsserter));
            this.comparisonFormatter = comparisonFormatter ?? throw new ArgumentNullException(nameof(comparisonFormatter));
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the actual XML property is changed.
        /// </summary>
        public event EventHandler<XmlChangedEventArgs>? ActualXmlChangedEvent;

        /// <summary>
        /// Occurs when expected XML property is changed.
        /// </summary>
        public event EventHandler<XmlChangedEventArgs>? ExpectedXmlChangedEvent;

        #endregion

        #region Properties

        public XElement? ActualXml
        {
            get
            {
                return this.actualXml;
            }

            private set
            {
                this.actualXml = value;
                this.OnActualXmlChanged(value);
            }
        }

        public XElement? ExpectedXml
        {
            get
            {
                return this.expectedXml;
            }

            private set
            {
                this.expectedXml = value;
                this.OnExpectedXmlChanged(value);
            }
        }

        public Diff? Differences
        {
            get { return this.differences; }
        }

        #endregion

        #region Public Methods

        #region Arrange Actual XML

        /// <summary>
        /// Arrange the actual XML from an embedded resource in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly that contains the resource.</param>
        /// <param name="resourceName">The name of the resource.</param>
        public void ArrangeActualXml(Assembly assembly, string resourceName)
        {
            this.ArrangeActualXml(EmbeddedResource.GetAsString(assembly, resourceName));
        }

        /// <summary>
        /// Arrange the actual XML from the given XElement.
        /// </summary>
        /// <param name="xmlElement">The XElement to set as the input.</param>
        public void ArrangeActualXml(XElement? xmlElement)
        {
            if (xmlElement == null)
            {
                throw new ArgumentNullException(nameof(xmlElement));
            }

            this.ArrangeActualXml(xmlElement.ToString());      // Clone it...
        }

        /// <summary>
        /// Arrange the actual XML by serialising the given object into XML.
        /// </summary>
        /// <param name="valueToSerialise">The object to serialise as the actual XML.</param>
        public void ArrangeActualXml(object valueToSerialise)
        {
            this.ArrangeActualXml(valueToSerialise.SerialiseToXml());
        }

        /// <summary>
        /// Arrange the actual XML by serialising the given object into XML.
        /// </summary>
        /// <param name="valueToSerialise">The object to serialise as the actual XML.</param>
        /// <param name="xmlRootAttribute">The XML root attribute.</param>
        public void ArrangeActualXml(object valueToSerialise, XmlRootAttribute xmlRootAttribute)
        {
            this.ArrangeActualXml(valueToSerialise.SerialiseToXml(xmlRootAttribute));
        }

        /// <summary>
        /// Arrange the actual XML by serialising the given object into XML.
        /// </summary>
        /// <param name="valueToSerialise">The object to serialise as the actual XML.</param>
        /// <param name="equalityKeyOfXmlAttributeOverridesToAvoidXmlSerializerMemoryLeak">Key of the XmlSerializer cache, unique for the given XmlAttributeOverrides, used to avoid XmlSerializer memory leaks. CAUTION: XmlAttributeOverrides.GetHashCode() returns a different value for each instance, even if each instance has the exact same objects - so consider making your own equality key based on what you added to the XmlAttributeOverrides.</param>
        /// <param name="xmlAttributeOverrides">The XML attribute overrides.</param>
        /// <remarks>
        /// <para>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer?view=net-5.0#dynamically-generated-assemblies
        /// Dynamically Generated Assemblies
        /// To increase performance, the XML serialization infrastructure dynamically generates assemblies to serialize and deserialize specified types.
        /// The infrastructure finds and reuses those assemblies.
        /// This behavior occurs only when using the following constructors:
        ///   XmlSerializer.XmlSerializer(Type)
        ///   XmlSerializer.XmlSerializer(Type, String)
        /// If you use any of the other constructors, multiple versions of the same assembly are generated and never unloaded, which results in a memory leak and poor performance.
        /// The easiest solution is to use one of the previously mentioned two constructors.
        /// Otherwise, you must cache the assemblies...
        /// </para>
        /// <para>
        /// ChannelAdam.Xml does the caching for you, but it requires you to specify the key to use in the cache.
        /// CAUTION: XmlAttributeOverrides.GetHashCode() returns a different value for each instance, even if each instance has the exact same objects
        ///   - so consider making your own equality key based on what you added to the XmlAttributeOverrides.
        /// </para>
        /// </remarks>
        public void ArrangeActualXml(object valueToSerialise, string equalityKeyOfXmlAttributeOverridesToAvoidXmlSerializerMemoryLeak, XmlAttributeOverrides xmlAttributeOverrides)
        {
            this.ArrangeActualXml(valueToSerialise.SerialiseToXml(equalityKeyOfXmlAttributeOverridesToAvoidXmlSerializerMemoryLeak, xmlAttributeOverrides));
        }

        /// <summary>
        /// Arrange the actual XML from the given XML string.
        /// </summary>
        /// <param name="xmlValue">The XML string.</param>
        public void ArrangeActualXml(string xmlValue)
        {
            this.ActualXml = xmlValue.ToXElement();
        }

        #endregion

        #region Arrange Expected XML

        /// <summary>
        /// Arrange the expected XML from an embedded resource in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="resourceName">Name of the resource.</param>
        public void ArrangeExpectedXml(Assembly assembly, string resourceName)
        {
            this.ArrangeExpectedXml(EmbeddedResource.GetAsString(assembly, resourceName));
        }

        /// <summary>
        /// Arrange the expected XML from the given XElement.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        public void ArrangeExpectedXml(XElement? xmlElement)
        {
            if (xmlElement == null)
            {
                throw new ArgumentNullException(nameof(xmlElement));
            }

            this.ArrangeExpectedXml(xmlElement.ToString());     // Clone it...
        }

        /// <summary>
        /// Arrange the expected XML by serialising the given object into XML.
        /// </summary>
        /// <param name="valueToSerialise">The value to serialise as the expected XML.</param>
        public void ArrangeExpectedXml(object valueToSerialise)
        {
            this.ArrangeExpectedXml(valueToSerialise.SerialiseToXml());
        }

        /// <summary>
        /// Arrange the expected XML by serialising the given object into XML.
        /// </summary>
        /// <param name="valueToSerialise">The value to serialise as the expected XML.</param>
        /// <param name="xmlRootAttribute">The XML root attribute.</param>
        public void ArrangeExpectedXml(object valueToSerialise, XmlRootAttribute xmlRootAttribute)
        {
            this.ArrangeExpectedXml(valueToSerialise.SerialiseToXml(xmlRootAttribute));
        }

        /// <summary>
        /// Arrange the expected XML by serialising the given object into XML.
        /// </summary>
        /// <param name="valueToSerialise">The value to serialise as the expected XML.</param>
        /// <param name="equalityKeyOfXmlAttributeOverridesToAvoidXmlSerializerMemoryLeak">Key of the XmlSerializer cache, unique for the given XmlAttributeOverrides, used to avoid XmlSerializer memory leaks. CAUTION: XmlAttributeOverrides.GetHashCode() returns a different value for each instance, even if each instance has the exact same objects - so consider making your own equality key based on what you added to the XmlAttributeOverrides.</param>
        /// <param name="xmlAttributeOverrides">The XML attribute overrides.</param>
        /// <remarks>
        /// <para>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer?view=net-5.0#dynamically-generated-assemblies
        /// Dynamically Generated Assemblies
        /// To increase performance, the XML serialization infrastructure dynamically generates assemblies to serialize and deserialize specified types.
        /// The infrastructure finds and reuses those assemblies.
        /// This behavior occurs only when using the following constructors:
        ///   XmlSerializer.XmlSerializer(Type)
        ///   XmlSerializer.XmlSerializer(Type, String)
        /// If you use any of the other constructors, multiple versions of the same assembly are generated and never unloaded, which results in a memory leak and poor performance.
        /// The easiest solution is to use one of the previously mentioned two constructors.
        /// Otherwise, you must cache the assemblies...
        /// </para>
        /// <para>
        /// ChannelAdam.Xml does the caching for you, but it requires you to specify the key to use in the cache.
        /// CAUTION: XmlAttributeOverrides.GetHashCode() returns a different value for each instance, even if each instance has the exact same objects
        ///   - so consider making your own equality key based on what you added to the XmlAttributeOverrides.
        /// </para>
        /// </remarks>
        public void ArrangeExpectedXml(object valueToSerialise, string equalityKeyOfXmlAttributeOverridesToAvoidXmlSerializerMemoryLeak, XmlAttributeOverrides xmlAttributeOverrides)
        {
            this.ArrangeExpectedXml(valueToSerialise.SerialiseToXml(equalityKeyOfXmlAttributeOverridesToAvoidXmlSerializerMemoryLeak, xmlAttributeOverrides));
        }

        /// <summary>
        /// Arrange the expected XML from the given XML string.
        /// </summary>
        /// <param name="xmlValue">The XML string.</param>
        public void ArrangeExpectedXml(string xmlValue)
        {
            this.ExpectedXml = xmlValue.ToXElement();
        }

        #endregion

        #region Assertions

        /// <summary>
        /// Assert the actual XML against the expected XML.
        /// </summary>
        public void AssertActualXmlEqualsExpectedXml()
        {
            this.AssertActualXmlEqualsExpectedXml(null);
        }

        /// <summary>
        /// Assert the actual XML against the expected XML, ignoring the elements specified by the given XML filter.
        /// </summary>
        /// <param name="xmlFilter">The XML filter to be applied to ignore specified elements from the assertion.</param>
        public virtual void AssertActualXmlEqualsExpectedXml(IXmlFilter? xmlFilter)
        {
            this.logger.Log("Asserting actual and expected XML are equal");

            var filteredExpectedXml = this.ExpectedXml;
            var filteredActualXml = this.ActualXml;

            if (filteredExpectedXml is null)
            {
                throw new InvalidOperationException("ExpectedXml must be specified before calling AssertActualXmlEqualsExpectedXml()");
            }

            if (filteredActualXml is null)
            {
                throw new InvalidOperationException("ActualXml must be specified before calling AssertActualXmlEqualsExpectedXml()");
            }

            if (xmlFilter?.HasFilters() == true)
            {
                this.logger.Log(xmlFilter.ToString());

                filteredExpectedXml = xmlFilter.ApplyFilterTo(filteredExpectedXml);
                filteredActualXml = xmlFilter.ApplyFilterTo(filteredActualXml);
            }

            var isEqual = this.IsEqual(filteredExpectedXml, filteredActualXml);
            if (!isEqual)
            {
                // IsEqual() sets this.differences... so it cannot be null
                var report = string.Join("." + Environment.NewLine, this.differences!.Differences);
                this.logger.Log("The differences are: " + Environment.NewLine + report);
            }

            this.logAssert.IsTrue("The XML is as expected", isEqual);
            this.logger.Log("The XML is as expected");
        }

        #endregion

        #region Utility Methods

        public bool IsEqual()
        {
            return this.IsEqual(this.ExpectedXml, this.ActualXml);
        }

        public bool IsEqual(XNode? expected, XNode? actual)
        {
            if (expected is null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            if (actual is null)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            var expectedXmlNode = expected.ToXmlNode();
            var actualXmlNode = actual.ToXmlNode();

            if (expectedXmlNode is null)
            {
                throw new InvalidOperationException("Expected XML Node must be specified before calling IsEqual()");
            }

            if (actualXmlNode is null)
            {
                throw new InvalidOperationException("Actual XML Node must be specified before calling IsEqual()");
            }

            return this.IsEqual(expectedXmlNode, actualXmlNode);
        }

        /// <summary>
        /// Determines if the given actual and expected XML is equivalent.
        /// </summary>
        /// <param name="expected">The expected node.</param>
        /// <param name="actual">The actual node.</param>
        /// <returns>
        /// The XML differences.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "As designed.")]
        public virtual bool IsEqual(XmlNode expected, XmlNode actual)
        {
            // XMLUnit's DiffBuilder - https://github.com/xmlunit/user-guide/wiki/DiffBuilder
            // Do NOT ignore whitespace because that trims text node values which is destructive and not equivalent in enterprise systems.
            this.differences = DiffBuilder.Compare(Input.FromNode(expected))
                                    .WithTest(Input.FromNode(actual))
                                    .IgnoreComments()
                                    .CheckForSimilar()                  // ignore child order, namespace prefixes etc - https://github.com/xmlunit/user-guide/wiki/DifferenceEvaluator#default-differenceevaluator
                                    .WithNodeMatcher(new DefaultNodeMatcher(ElementSelectors.ByName)) // allows for comparisons of nodes in different order
                                    .WithDifferenceEvaluator(DifferenceEvaluators.Chain(DifferenceEvaluators.Default, IgnoreElementTagNameDifferenceEvaluator.Evaluate))
                                    .WithComparisonFormatter(this.comparisonFormatter)
                                    .Build();

            return !this.differences.HasDifferences();
        }

        #endregion

        #endregion

        #region Protected Change Methods

        protected virtual void OnExpectedXmlChanged(XNode? value)
        {
            if (this.ExpectedXmlChangedEvent == null)
            {
                // Only log the details if the event has not been subscribed to - because it is expected that the subscriber instead will log with more contextual detail
                this.logger.Log();
                this.logger.Log($"The expected XML is: {Environment.NewLine}{value?.ToString()}");
            }
            else
            {
                this.ExpectedXmlChangedEvent.Invoke(this, new XmlChangedEventArgs(value));
            }
        }

        protected virtual void OnActualXmlChanged(XNode? value)
        {
            if (this.ActualXmlChangedEvent == null)
            {
                // Only log the details if the event has not been subscribed to - because it is expected that the subscriber instead will log with more contextual detail
                this.logger.Log();
                this.logger.Log($"The actual XML is: {Environment.NewLine}{value?.ToString()}");
            }
            else
            {
                this.ActualXmlChangedEvent.Invoke(this, new XmlChangedEventArgs(value));
            }
        }

        #endregion
    }
}
