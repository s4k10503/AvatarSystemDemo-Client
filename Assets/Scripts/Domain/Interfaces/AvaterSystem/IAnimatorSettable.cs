namespace Domain.Interfaces
{
    /// <summary>
    /// Animator を設定できるインターフェース
    /// </summary>
    public interface IAnimatorSettable
    {
        /// <summary>
        /// Animator を設定する
        /// </summary>
        void SetAnimator(object animatorInstance);
    }
}
