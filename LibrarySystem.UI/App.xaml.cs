using LibrarySystem.UI.Helpers;

namespace LibrarySystem.UI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        ThemeHelper.ApplySavedTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
