using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace EdgePilot.UI;

public sealed class SettingsWindow : Window
{
    public SettingsWindow(NotchPreferences current, Action<NotchPreferences> apply, string? warning = null, string? storagePath = null)
    {
        Title = "EdgePilot · Settings";
        Width = 420;
        Height = 460;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var edge = new ComboBox
        {
            ItemsSource = new[] { "Right", "Left", "Top", "Bottom" },
            SelectedIndex = (int)current.Edge,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var mode = new ComboBox
        {
            ItemsSource = new[] { "Show on hover", "Always open", "Hidden" },
            SelectedIndex = (int)current.Mode,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var message = new TextBlock
        {
            Text = warning ?? "Changes are saved when you press Apply.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        var applyButton = new Button { Content = "Apply", HorizontalAlignment = HorizontalAlignment.Right };
        applyButton.Click += (_, _) =>
        {
            var value = new NotchPreferences((EdgeSide)edge.SelectedIndex, (NotchDisplayMode)mode.SelectedIndex);
            try
            {
                PreferenceStore.Save(storagePath ?? PreferenceStore.DefaultPath, value);
                apply(value);
                message.Text = value.Mode == NotchDisplayMode.Hidden
                    ? "Notch hidden. Choose another mode to show it. Closing Settings now exits EdgePilot; reopening EdgePilot returns here."
                    : "Saved. Right-click the notch to return to Settings.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                message.Text = "Could not save settings. Your current settings are unchanged. " + ex.Message;
            }
        };
        Content = new ScrollViewer { Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Make EdgePilot yours", FontSize = 22 },
                new TextBlock { Text = "Screen edge" }, edge,
                new TextBlock { Text = "Display" }, mode,
                message, applyButton
            }
        } };
    }
}
