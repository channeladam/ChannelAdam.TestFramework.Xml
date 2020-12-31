#nullable disable

namespace ChannelAdam.TestFramework.BehaviourSpecs
{
    using TechTalk.SpecFlow;
    using ChannelAdam.TestFramework.Xml;
    using ChannelAdam.TestFramework.NUnit.Abstractions;
    using System;
    using ChannelAdam.TestFramework.Abstractions;
    using global::Moq;

    [Binding]
    [Scope(Feature = "XmlTesting")]
    public class XmlTestingUnitSteps : MoqTestFixture
    {
        private const string NotEqualExceptionMessage = "NOT EQUAL - from mock";

        #region Fields

        private readonly ScenarioContext _scenarioContext;
        private XmlTester xmlTester;
        private bool isEqual;

        #endregion

        public XmlTestingUnitSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        #region Before/After

        [BeforeScenario]
        public void BeforeScenario()
        {
            Logger.Log("---------------------------------------------------------------------------");
            Logger.Log(_scenarioContext.ScenarioInfo.Title);
            Logger.Log("---------------------------------------------------------------------------");

            // We have to mock out the LogAssert.IsTrue() because
            // NUnit tells the test provider to fail the test on an assertion failure - regardless of whether we catch its AssertionException.
            var mockLogAssert = MyMockRepository.Create<ILogAsserter>();
            mockLogAssert
                .Setup(m => m.IsTrue(It.IsAny<string>(), It.Is<bool>(p => !p)))
                .Throws(new Exception(NotEqualExceptionMessage));

            this.xmlTester = new XmlTester(mockLogAssert.Object);
        }

        #endregion

        #region Given

        [Given("two xml samples with the same namespace urls but different namespace prefixes")]
        public void GivenTwoXmlSamplesWithTheSameNamespaceUrlsButDifferentNamespacePrefixes()
        {
            this.xmlTester.ArrangeExpectedXml(@"<a xmlns=""http://wwww.com""><b>hi</b><c>c</c></a>");
            this.xmlTester.ArrangeActualXml(@"<ns0:a xmlns:ns0=""http://wwww.com""><ns0:c>c</ns0:c><ns0:b>hi</ns0:b></ns0:a>");
        }

        [Given("two xml samples with the same child nodes but in a different order")]
        public void GivenTwoXmlSamplesWithTheSameChildNodesButInADifferentOrder()
        {
            this.xmlTester.ArrangeExpectedXml("<a><b>hi</b><c>c</c></a>");
            this.xmlTester.ArrangeActualXml("<a><c>c</c><b>hi</b></a>");
        }

        [Given("two xml samples with the different elements")]
        public void GivenTwoXmlSamplesWithTheDifferentElements()
        {
            this.xmlTester.ArrangeExpectedXml("<a><b>hi</b></a>");
            this.xmlTester.ArrangeActualXml("<a><c>c</c></a>");
        }

        [Given("two xml samples with the same elements but a different value")]
        public void GivenTwoXmlSamplesWithTheSameElementsButADifferentValue()
        {
            this.xmlTester.ArrangeExpectedXml("<a><b>hi</b><c>c</c></a>");
            this.xmlTester.ArrangeActualXml("<a><b>oh</b><c>c</c></a>");
        }

        #endregion

        #region When

        [When("the two xml samples are compared")]
        public void WhenTheTwoXmlSamplesAreCompared()
        {
            Logger.Log("Comparing...");
            this.isEqual = xmlTester.IsEqual();

            if (!this.isEqual)
            {
                var report = string.Join("." + Environment.NewLine, xmlTester.Differences.Differences);
                Logger.Log("*** The differences are: " + Environment.NewLine + report);
            }
        }

        [When("an equality assertion is performed")]
        public void WhenAnEqualityAssertionIsPerformed()
        {
            this.ExpectedException.MessageShouldContainText = NotEqualExceptionMessage;
            Try(() => xmlTester.AssertActualXmlEqualsExpectedXml());
        }

        #endregion

        #region Then

        [Then("the two xml samples are treated as equal")]
        public void ThenTheTwoXmlSamplesAreTreatedAsEqual()
        {
            LogAssert.IsTrue("Xml samples are equal", this.isEqual);
        }

        [Then("the two xml samples are treated as different")]
        public void ThenTheTwoXmlSamplesAreTreatedAsDifferent()
        {
            LogAssert.IsTrue("Xml samples are different", !this.isEqual);
        }

        [Then("an assertion exception occurred")]
        public void ThenAnAssertionExceptionOccurred()
        {
            base.AssertExpectedException();
        }

        #endregion
    }
}