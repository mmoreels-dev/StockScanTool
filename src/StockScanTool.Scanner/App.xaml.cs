using StockScanTool.Scanner.Views;

namespace StockScanTool.Scanner;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new LoginPage());
    }
}
