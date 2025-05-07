using UnityEngine;

namespace Presentation.Interfaces
{
    public interface IPageManager
    {
        void InitializePages();
        void RegisterPage(PageType pageType, GameObject page);
        void NavigateTo(PageType pageType);
        PageType GetCurrentPage();
    }

    public enum PageType
    {
        Login,
        AvatarSystem
    }
}
