using MudBlazor;

namespace StockScanTool.Shared.Themes;

public static class StockScanToolTheme
{
    public static MudTheme Default => new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#FF6D00",
            Secondary = "#1A1A2E",
            Surface = "#FFFFFF",
            Background = "#F5F5F5",
            Success = "#2E7D32",
            Error = "#D32F2F",
            Warning = "#F9A825",
            TextPrimary = "#212121",
            TextSecondary = "#757575",
            DrawerBackground = "#1A1A2E",
            DrawerText = "#BDBDBD",
            DrawerIcon = "#BDBDBD",
            AppbarBackground = "#1A1A2E",
            AppbarText = "#FFFFFF",
        },
    };

    public static MudTheme Minimal => new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#FF6D00",
            Secondary = "#1A1A2E",
            Surface = "#FFFFFF",
            Background = "#1A1A2E",
            Success = "#2E7D32",
            Error = "#D32F2F",
            TextPrimary = "#212121",
            TextSecondary = "#757575",
        },
    };
}
