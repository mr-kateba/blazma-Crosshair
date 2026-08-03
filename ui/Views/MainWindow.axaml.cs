using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Blazma.ViewModels;

namespace Blazma.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // The window draws its own chrome, so the title bar has to move the window
        // and the buttons have to do what the system ones normally would.
        var titleBar = this.FindControl<Grid>("TitleBar");
        if (titleBar is not null)
            titleBar.PointerPressed += OnTitleBarPressed;

        var minimise = this.FindControl<Button>("MinimiseButton");
        if (minimise is not null)
            minimise.Click += (_, _) => WindowState = WindowState.Minimized;

        var close = this.FindControl<Button>("CloseButton");
        if (close is not null)
            close.Click += (_, _) => Close();

        Loaded += OnLoaded;

        // Tunnelled so this runs before the control under the pointer reacts, which
        // means the first user edit is already being persisted when it lands.
        AddHandler(PointerPressedEvent, OnFirstInput, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnFirstInput, RoutingStrategies.Tunnel);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var scroll = this.FindControl<ScrollViewer>("SettingsScroll");

        // Background priority runs after layout. Scrolling first realises the
        // controls at the top of the list, then the second pass undoes whatever
        // they wrote back on their way in.
        Dispatcher.UIThread.Post(() =>
        {
            scroll?.ScrollToHome();
            Dispatcher.UIThread.Post(
                () => (DataContext as MainViewModel)?.RestoreFromFile(),
                DispatcherPriority.Background);
        }, DispatcherPriority.Background);
    }

    private void OnFirstInput(object? sender, RoutedEventArgs e)
    {
        RemoveHandler(PointerPressedEvent, OnFirstInput);
        RemoveHandler(KeyDownEvent, OnFirstInput);
        (DataContext as MainViewModel)?.GoLive();
    }

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}
