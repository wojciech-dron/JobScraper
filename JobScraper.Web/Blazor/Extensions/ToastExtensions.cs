using BlazorBootstrap;

namespace JobScraper.Web.Blazor.Extensions;

public static class ToastExtensions
{
    public static void PushMessage(this List<ToastMessage> messages,
        string message,
        ToastType toastType = ToastType.Success,
        bool autoHide = true
    ) =>
        messages.Insert(0,
            new ToastMessage
            {
                Type = toastType,
                Message = message,
                AutoHide = autoHide,
            });
}
