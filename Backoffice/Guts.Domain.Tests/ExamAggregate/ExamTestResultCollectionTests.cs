using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    [TestFixture]
    public class ExamTestResultCollectionTests : DomainTestBase
    {
        [Test]
        public void AddedExamPartResultCollection_ShouldBeRetrievableByExamPartId()
        {
            //Arrange
            var collection =new ExamTestResultCollection();
            int examPartId = Random.NextPositive();
            var examPartTestResultCollectionMock = new Mock<IExamPartTestResultCollection>();

            //Act
            collection.AddExamPartResults(examPartId, examPartTestResultCollectionMock.Object);
            var retrievedExamPartTestResultCollection = collection.GetExamPartResults(examPartId);

            //Assert
            Assert.That(retrievedExamPartTestResultCollection, Is.SameAs(examPartTestResultCollectionMock.Object));
        }
    }
}