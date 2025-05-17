using System;
using NUnit.Framework;
using UnityEngine;
using NSubstitute;

using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class AvatarCustomizationServiceTests
    {
        private AvatarCustomizationService _avatarCustomizationService;
        private Animator _mockAnimator;
        private IAvatarParameterRepository _mockRepository;
        private Transform _testTransform;
        private GameObject _testGameObject;

        [SetUp]
        public void Setup()
        {
            _mockAnimator = Substitute.For<Animator>();
            _mockRepository = Substitute.For<IAvatarParameterRepository>();

            // Create a real GameObject and Transform for testing
            _testGameObject = new GameObject("TestAvatarRoot");
            _testTransform = _testGameObject.transform;

            _avatarCustomizationService = new AvatarCustomizationService(_mockRepository);
            _avatarCustomizationService.SetAnimator(_mockAnimator);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject); // Use DestroyImmediate in EditMode
            }
        }

        [Test]
        public void ApplyBodyScale_有効なスケールで全スケール適用()
        {
            // Arrange
            var scale = new BodyScale(
                height: 1.5f,
                shoulderWidth: 1.3f,
                bodyWidth: 1.2f,
                headSize: 1.1f
            );
            _mockAnimator.GetBoneTransform(Arg.Any<HumanBodyBones>()).Returns(_testTransform);

            // Act
            _avatarCustomizationService.ApplyBodyScale(scale);

            // Assert
            var currentScale = _avatarCustomizationService.GetCurrentBodyScale();
            Assert.That(currentScale.Height, Is.EqualTo(scale.Height));
            Assert.That(currentScale.BodyWidth, Is.EqualTo(scale.BodyWidth));
            Assert.That(currentScale.ShoulderWidth, Is.EqualTo(scale.ShoulderWidth));
            Assert.That(currentScale.HeadSize, Is.EqualTo(scale.HeadSize));
        }

        [Test]
        public void ApplyHeight_有効な身長でボーンスケール更新()
        {
            // Arrange
            const float height = 1.5f;
            _mockAnimator.GetBoneTransform(Arg.Any<HumanBodyBones>()).Returns(_testTransform);

            // Act
            _avatarCustomizationService.ApplyHeight(height);

            // Assert
            Assert.That(_testTransform.localScale.y, Is.EqualTo(height));
            Assert.That(_testTransform.localScale.x, Is.EqualTo(height));
        }

        [Test]
        public void ApplyBodyWidth_有効な体幅でボーンスケール更新()
        {
            // Arrange
            const float width = 1.2f;
            _mockAnimator.GetBoneTransform(Arg.Any<HumanBodyBones>()).Returns(_testTransform);

            // Act
            _avatarCustomizationService.ApplyBodyWidth(width);

            // Assert
            Assert.That(_testTransform.localScale.z, Is.EqualTo(width));
        }

        [Test]
        public void ApplyShoulderWidth_有効な肩幅で肩ボーン更新()
        {
            // Arrange
            const float width = 1.3f;
            _mockAnimator.GetBoneTransform(Arg.Any<HumanBodyBones>()).Returns(_testTransform);

            // Act
            _avatarCustomizationService.ApplyShoulderWidth(width);

            // Assert
            Assert.That(_testTransform.localScale.z, Is.EqualTo(width));
        }

        [Test]
        public void ResetBodyScale_デフォルト値にリセット()
        {
            // Arrange
            var nonDefaultScale = new BodyScale(
                height: 1.5f,
                shoulderWidth: 1.3f,
                bodyWidth: 1.2f,
                headSize: 1.1f
            );
            _avatarCustomizationService.ApplyBodyScale(nonDefaultScale);
            _mockAnimator.GetBoneTransform(Arg.Any<HumanBodyBones>()).Returns(_testTransform);

            // Act
            _avatarCustomizationService.ResetBodyScale();

            // Assert
            var currentScale = _avatarCustomizationService.GetCurrentBodyScale();
            Assert.That(currentScale.Height, Is.EqualTo(1.0f));
            Assert.That(currentScale.BodyWidth, Is.EqualTo(1.0f));
            Assert.That(currentScale.ShoulderWidth, Is.EqualTo(1.0f));
            Assert.That(currentScale.HeadSize, Is.EqualTo(1.0f));
        }

        [Test]
        public void GetCurrentBodyScale_現在のスケールを返す()
        {
            // Arrange
            var expectedScale = new BodyScale(
                height: 1.5f,
                shoulderWidth: 1.3f,
                bodyWidth: 1.2f,
                headSize: 1.1f
            );
            _avatarCustomizationService.ApplyBodyScale(expectedScale);
            _mockAnimator.GetBoneTransform(Arg.Any<HumanBodyBones>()).Returns(_testTransform);

            // Act
            var currentScale = _avatarCustomizationService.GetCurrentBodyScale();

            // Assert
            Assert.That(currentScale.Height, Is.EqualTo(expectedScale.Height));
            Assert.That(currentScale.BodyWidth, Is.EqualTo(expectedScale.BodyWidth));
            Assert.That(currentScale.ShoulderWidth, Is.EqualTo(expectedScale.ShoulderWidth));
            Assert.That(currentScale.HeadSize, Is.EqualTo(expectedScale.HeadSize));
        }
    }
}
