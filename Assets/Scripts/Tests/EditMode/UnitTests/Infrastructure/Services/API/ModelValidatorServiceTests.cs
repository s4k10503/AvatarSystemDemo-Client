using System;
using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class ModelValidatorServiceTests
    {
        private ModelValidatorService _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new ModelValidatorService();
        }

        private class TestModel
        {
            [System.ComponentModel.DataAnnotations.Range(0, 100)]
            public float Height { get; set; }

            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; }
        }

        [Test]
        public void 有効なモデルの場合_例外が発生しないこと()
        {
            // Arrange
            var model = new TestModel { Height = 25.5f, Name = "Test" };

            // Act & Assert
            Assert.DoesNotThrow(() => _validator.Validate(model));
        }

        [Test]
        public void 無効な高さの場合_バリデーション例外が発生すること()
        {
            // Arrange
            var model = new TestModel { Height = 101f, Name = "Test" };

            // Act & Assert
            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(model));
            StringAssert.Contains("Height", exception.Message);
        }

        [Test]
        public void 必須の名前が欠落している場合_バリデーション例外が発生すること()
        {
            // Arrange
            var model = new TestModel { Height = 25.5f, Name = null };

            // Act & Assert
            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(model));
            StringAssert.Contains("Name", exception.Message);
        }

        [Test]
        public void モデルがNullの場合_ArgumentNullExceptionが発生すること()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.Validate(null));
        }
    }
}
