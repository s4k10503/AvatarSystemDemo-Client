using System;
using System.IO;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Repositories;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnitTests.Infrastructure
{
    public class JsonAvatarParameterRepositoryTests
    {
        private JsonAvatarParameterRepository _repository;
        private string _testFilePath;

        [SetUp]
        public void Setup()
        {
            // 一時ディレクトリ内に一意なテストファイルパスを作成することを推奨
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_avatar_{Guid.NewGuid()}.json");
            // リポジトリにテスト用ファイルパスを注入して初期化
            _repository = new JsonAvatarParameterRepository(_testFilePath);

            // テスト前に既存のファイルを確実に削除
            DeleteTestFile();
        }

        [TearDown]
        public void TearDown()
        {
            // テスト後にファイルを確実に削除
            DeleteTestFile();
        }

        private void DeleteTestFile()
        {
            try
            {
                if (File.Exists(_testFilePath))
                {
                    File.Delete(_testFilePath);
                }
                // ディレクトリが作成されている場合はそれも削除検討 (ただし他のテストと共有している場合は注意)
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"Could not delete test file {_testFilePath}: {ex.Message}");
            }
        }

        [UnityTest]
        public IEnumerator 保存して読み込むと_正しく動作すること() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var settings = new AvatarCustomizationSettings
            {
                Height = 1.8f,
                BodyWidth = 1.2f,
                ShoulderWidth = 1.5f,
                HeadSize = 1.1f,
                id = "test-avatar-001" // id も設定
            };

            // Act
            await _repository.SaveAsync(settings, CancellationToken.None);
            var loadedSettings = await _repository.LoadAsync(CancellationToken.None);

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null.");
            // float の比較には許容誤差を指定する
            Assert.That(loadedSettings.Height, Is.EqualTo(settings.Height).Within(0.0001f), $"Height mismatch. Expected: {settings.Height}, Actual: {loadedSettings.Height}");
            Assert.That(loadedSettings.BodyWidth, Is.EqualTo(settings.BodyWidth).Within(0.0001f), $"BodyWidth mismatch. Expected: {settings.BodyWidth}, Actual: {loadedSettings.BodyWidth}");
            Assert.That(loadedSettings.ShoulderWidth, Is.EqualTo(settings.ShoulderWidth).Within(0.0001f), $"ShoulderWidth mismatch. Expected: {settings.ShoulderWidth}, Actual: {loadedSettings.ShoulderWidth}");
            Assert.That(loadedSettings.HeadSize, Is.EqualTo(settings.HeadSize).Within(0.0001f), $"HeadSize mismatch. Expected: {settings.HeadSize}, Actual: {loadedSettings.HeadSize}");
            Assert.That(loadedSettings.id, Is.EqualTo(settings.id), $"id mismatch. Expected: {settings.id}, Actual: {loadedSettings.id}");
        });

        [UnityTest]
        public IEnumerator ファイルが存在しない場合に読み込むと_デフォルト値が返されること() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - ファイルが存在しないことを確認 (Setupで削除される)
            // Ensure file doesn't exist from previous runs if Setup failed somehow
            DeleteTestFile();

            // Act
            var loadedSettings = await _repository.LoadAsync(CancellationToken.None);

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null even if file doesn't exist.");
            var defaultSettings = new AvatarCustomizationSettings(); // 比較用のデフォルトインスタンス
            Assert.That(loadedSettings.Height, Is.EqualTo(defaultSettings.Height).Within(0.0001f), "Height should be default.");
            Assert.That(loadedSettings.BodyWidth, Is.EqualTo(defaultSettings.BodyWidth).Within(0.0001f), "BodyWidth should be default.");
            Assert.That(loadedSettings.ShoulderWidth, Is.EqualTo(defaultSettings.ShoulderWidth).Within(0.0001f), "ShoulderWidth should be default.");
            Assert.That(loadedSettings.HeadSize, Is.EqualTo(defaultSettings.HeadSize).Within(0.0001f), "HeadSize should be default.");
            Assert.That(loadedSettings.id, Is.EqualTo(defaultSettings.id), "id should be default (null)."); // id もデフォルト値と比較
        });
    }
}
