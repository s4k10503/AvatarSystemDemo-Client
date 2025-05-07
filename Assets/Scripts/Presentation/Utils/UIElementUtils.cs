using UnityEngine;
using UnityEngine.UIElements;
using System.Runtime.CompilerServices;

namespace Presentation.Utils
{
    /// <summary>
    /// UI Toolkit の VisualElement に関するユーティリティメソッドを提供します。
    /// </summary>
    public static class UIElementUtils
    {
        /// <summary>
        /// UI要素を検索し、見つからない場合はエラーログを出力する。
        /// </summary>
        /// <typeparam name="T">検索するVisualElementの型</typeparam>
        /// <param name="root">検索対象の親要素</param>
        /// <param name="elementName">検索する要素の名前 (classNameがnullの場合に使用)</param>
        /// <param name="className">検索する要素のクラス名 (これが指定された場合、elementNameは無視される)</param>
        /// <param name="context">ログ出力に使用するコンテキストオブジェクト</param>
        /// <returns>見つかった要素、またはnull</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T QueryAndCheck<T>(VisualElement root, string elementName = null, string className = null, Object context = null) where T : VisualElement
        {
            if (root == null)
            {
                // コンテキストがあれば、それを使用してログ出力
                string contextName = context != null ? $" in {context.GetType().Name}" : "";
                string targetName = !string.IsNullOrEmpty(className) ? $"class '{className}'" : $"name '{elementName}'";
                Debug.LogError($"[UIElementUtils] Querying {targetName} failed because the parent element is null{contextName}.");
                return null;
            }

            T element = default;
            string queryDescription = "";

            if (!string.IsNullOrEmpty(className))
            {
                element = root.Q<T>(className: className);
                queryDescription = $"class '{className}'";
            }
            else if (!string.IsNullOrEmpty(elementName))
            {
                element = root.Q<T>(name: elementName);
                queryDescription = $"name '{elementName}'";
            }
            else
            {
                Debug.LogError($"[UIElementUtils] QueryAndCheck requires either an elementName or a className to be specified.", context);
                return null;
            }

            if (element == null)
            {
                string parentPath = GetElementPath(root);
                string contextInfo = context != null ? $" (Context: {context.GetType().Name})" : "";
                // コンテキストがあれば、それを使用してログ出力し、GameObjectをハイライトできるようにする
                Debug.LogError($"[UIElementUtils] Element with {queryDescription} ({typeof(T).Name}) が見つかりません。親: '{parentPath}'{contextInfo}", context);
            }
            return element;
        }

        /// <summary>
        /// デバッグ用にVisualElementのパスを取得する（簡易版）。
        /// </summary>
        /// <param name="element">パスを取得する要素</param>
        /// <returns>要素のパス文字列</returns>
        public static string GetElementPath(VisualElement element)
        {
            if (element == null) return "null";
            string path = element.name;
            var parent = element.parent;
            while (parent != null && !string.IsNullOrEmpty(parent.name))
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            return path ?? "UnnamedRoot"; // ルート要素が名前を持たない場合
        }
    }
}
