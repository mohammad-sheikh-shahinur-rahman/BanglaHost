using System.Threading.Tasks;

namespace BanglaHost.App.Services;

/// <summary>Native folder picker for an unpackaged WinUI app (needs the window handle).</summary>
public static class Picker
{
    public static async Task<string?> FolderAsync()
    {
        if (BanglaHost.App.App.Window is null) return null;
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(BanglaHost.App.App.Window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    public static async Task<string?> FileSaveZipAsync(string suggestedName)
    {
        if (BanglaHost.App.App.Window is null) return null;
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("ZIP Archive", new[] { ".zip" });
        picker.SuggestedFileName = suggestedName;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(BanglaHost.App.App.Window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    public static async Task<string?> FileOpenZipAsync()
    {
        if (BanglaHost.App.App.Window is null) return null;
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".zip");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(BanglaHost.App.App.Window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
