using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace EdgePilot.UI;

public sealed class SettingsWindow : Window
{
    public SettingsWindow(NotchPreferences current, Action<NotchPreferences> apply, string? warning = null, string? storagePath = null)
    {
        Title = "EdgePilot · Impostazioni";
        Width = 420;
        Height = 460;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var edge = new ComboBox
        {
            ItemsSource = new[] { "Destra", "Sinistra", "Alto", "Basso" },
            SelectedIndex = (int)current.Edge,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var mode = new ComboBox
        {
            ItemsSource = new[] { "Al passaggio del mouse", "Sempre aperto", "Nascosto" },
            SelectedIndex = (int)current.Mode,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var message = new TextBlock
        {
            Text = warning ?? "Premi Applica per salvare le modifiche.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        var applyButton = new Button { Content = "Applica", HorizontalAlignment = HorizontalAlignment.Right };
        applyButton.Click += (_, _) =>
        {
            var value = new NotchPreferences((EdgeSide)edge.SelectedIndex, (NotchDisplayMode)mode.SelectedIndex);
            try
            {
                PreferenceStore.Save(storagePath ?? PreferenceStore.DefaultPath, value);
                apply(value);
                message.Text = value.Mode == NotchDisplayMode.Hidden
                    ? "Pannello nascosto. Scegli un’altra modalità per mostrarlo. Chiudendo le impostazioni esci da EdgePilot; al prossimo avvio tornerai qui."
                    : "Impostazioni salvate. Fai clic destro sul pannello per riaprirle.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                message.Text = "Impossibile salvare le impostazioni. Le preferenze attive non sono cambiate. Verifica i permessi di scrittura e lo spazio disponibile, poi riprova.";
            }
        };
        Content = new ScrollViewer { Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Personalizza EdgePilot", FontSize = 22 },
                new TextBlock { Text = "Bordo dello schermo" }, edge,
                new TextBlock { Text = "Visualizzazione" }, mode,
                message, applyButton
            }
        } };
    }
}
