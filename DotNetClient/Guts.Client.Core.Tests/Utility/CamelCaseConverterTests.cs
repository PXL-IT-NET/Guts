using System;
using System.Collections.Generic;
using System.Text;
using Guts.Client.Core.Utility;
using NUnit.Framework;

namespace Guts.Client.Core.Tests.Utility
{
    [TestFixture]
    internal class CamelCaseConverterTests
    {
        [TestCase("ThisIsATest", "This is a test")]
        [TestCase("SomeMethod_SomeCondition_ShouldResultInSomething",
            "Some method - Some condition - Should result in something")]
        [TestCase("SomeMethod-SomeCondition-ShouldResultInSomething",
            "Some method - Some condition - Should result in something")]
        [TestCase("_01_ThisIsATest", "01 - This is a test")]
        public void ToNormalSentence_ShouldConvertCamelCaseStringToNormalSentence(string camelCaseString,
            string expectedSentence)
        {
            //Arrange
            var converter = new CamelCaseConverter();

            //Act
            var actualSentence = converter.ToNormalSentence(camelCaseString);

            //Assert
            Assert.That(actualSentence, Is.EqualTo(expectedSentence));
        }   
    }
}
