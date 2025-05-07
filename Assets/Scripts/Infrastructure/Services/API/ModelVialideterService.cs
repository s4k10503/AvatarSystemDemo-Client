using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public sealed class ModelValidatorService : IModelValidatorService
    {
        /// <summary>
        /// 指定されたモデルのバリデーションを行い、失敗した場合は ValidationException を発生させます。
        /// </summary>
        /// <param name="model">検証対象のモデル</param>
        /// <exception cref="ValidationException">検証に失敗した場合に送出される例外。エラーメッセージが含まれます。</exception>
        /// <exception cref="ArgumentNullException">モデルが null の場合に送出される例外</exception>
        public void Validate(object model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "モデルはnullにできません。");
            }

            // 検証のコンテキストを作成
            var validationContext = new ValidationContext(model, serviceProvider: null, items: null);
            // 検証結果を格納するリスト
            var validationResults = new List<ValidationResult>();

            // Validator.TryValidateObject を使用してオブジェクト全体を検証
            // validateAllProperties: true を指定すると、すべてのプロパティが検証される
            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

            // 検証に失敗した場合
            if (!isValid)
            {
                // 検証結果からエラーメッセージを抽出し、連結する
                // 必要に応じて MemberNames も含めることができる:
                // var errorMessages = validationResults.Select(vr => $"{string.Join(", ", vr.MemberNames)}: {vr.ErrorMessage}");
                var errorMessages = validationResults.Select(vr => vr.ErrorMessage);

                // 連結したエラーメッセージを持つ ValidationException をスロー
                throw new ValidationException(string.Join(", ", errorMessages));
            }
            // 検証が成功した場合は何もせず終了
        }
    }
}
