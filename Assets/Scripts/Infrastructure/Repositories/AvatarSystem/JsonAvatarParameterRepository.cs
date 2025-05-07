using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

using Domain.Interfaces;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// JSONファイルを使用したアバターパラメータの保存と読み込みを行うリポジトリ実装
    /// </summary>
    public sealed class JsonAvatarParameterRepository : IAvatarParameterRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerSettings _jsonSettings;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="filePath">JSONファイルの保存パス（nullの場合はデフォルトパスを使用）</param>
        public JsonAvatarParameterRepository(string filePath = null)
        {
            _filePath = filePath ?? Path.Combine(Application.persistentDataPath, "avatar_customization.json");
            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            EnsureDirectoryExists();
        }

        /// <summary>
        /// アバターカスタマイズ設定を非同期で読み込む
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>読み込まれたアバターカスタマイズ設定</returns>
        public async UniTask<AvatarCustomizationSettings> LoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new AvatarCustomizationSettings();
                }

                string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                return DeserializeSettings(json);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"設定の読み込みに失敗しました: {_filePath}", ex);
            }
        }

        /// <summary>
        /// アバターカスタマイズ設定を非同期で保存する
        /// </summary>
        /// <param name="settings">保存するアバターカスタマイズ設定</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        public async UniTask SaveAsync(AvatarCustomizationSettings settings, CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            try
            {
                string json = JsonConvert.SerializeObject(settings, _jsonSettings);
                await File.WriteAllTextAsync(_filePath, json, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"設定の保存に失敗しました: {_filePath}", ex);
            }
        }

        #region プライベートメソッド

        /// <summary>
        /// 保存ディレクトリが存在することを確認し、必要に応じて作成する
        /// </summary>
        private void EnsureDirectoryExists()
        {
            string directory = Path.GetDirectoryName(_filePath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"ディレクトリの作成に失敗しました: {directory}", ex);
            }
        }

        /// <summary>
        /// JSON文字列からAvatarCustomizationSettingsオブジェクトへデシリアライズする
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>デシリアライズされたオブジェクト、またはJSON文字列が無効な場合は新しいインスタンス</returns>
        private AvatarCustomizationSettings DeserializeSettings(string json)
        {
            try
            {
                var settings = JsonConvert.DeserializeObject<AvatarCustomizationSettings>(json, _jsonSettings);
                return settings ?? new AvatarCustomizationSettings();
            }
            catch (JsonException ex)
            {
                throw new FormatException("無効なJSON形式です", ex);
            }
        }

        #endregion
    }
}
