namespace GrantOS.Sentinel.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new MainPage()) { Title = "GrantOS Sentinel" };
}
