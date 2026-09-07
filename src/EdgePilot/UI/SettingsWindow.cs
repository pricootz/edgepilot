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
        Height = 650;
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
        var intervals = new[] { 500, 1000, 2000, 5000 };
        var refresh = new ComboBox
        {
            ItemsSource = new[] { "Ogni 0,5 secondi", "Ogni secondo", "Ogni 2 secondi", "Ogni 5 secondi" },
            SelectedIndex = Array.IndexOf(intervals, current.RefreshIntervalMs),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var sensitivity = new ComboBox
        {
            ItemsSource = new[] { "Precisa", "Normale", "Ampia" },
            SelectedIndex = (int)current.Sensitivity,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var metricOptions = new[] { VisibleMetrics.Cpu, VisibleMetrics.Memory, VisibleMetrics.Disk, VisibleMetrics.Network };
        var metricNames = new[] { "CPU", "Memoria", "Disco", "Rete" };
        var metrics = metricOptions.Select((flag, index) => new CheckBox
        {
            Content = metricNames[index], IsChecked = current.Metrics.HasFlag(flag)
        }).ToArray();
        var metricPanel = new WrapPanel();
        foreach (var metric in metrics)
        {
            metric.Margin = new Thickness(0, 0, 16, 0);
            metricPanel.Children.Add(metric);
        }
        var message = new TextBlock
        {
            Text = warning ?? "Premi Applica per salvare le modifiche.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        var applyButton = new Button { Content = "Applica", HorizontalAlignment = HorizontalAlignment.Right };
        applyButton.Click += (_, _) =>
        {
            VisibleMetrics selected = 0;
            for (var i = 0; i < metrics.Length; i++)
                if (metrics[i].IsChecked == true) selected |= metricOptions[i];
            if (selected == 0)
            {
                message.Text = "Seleziona almeno una metrica da mostrare.";
                return;
            }
            var value = new NotchPreferences((EdgeSide)edge.SelectedIndex, (NotchDisplayMode)mode.SelectedIndex)
            {
                RefreshIntervalMs = intervals[refresh.SelectedIndex],
                Sensitivity = (HoverSensitivity)sensitivity.SelectedIndex,
                Metrics = selected
            };
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
                new TextBlock { Text = "Metriche visibili" }, metricPanel,
                new TextBlock { Text = "Aggiornamento dei dati" }, refresh,
                new TextBlock { Text = "Sensibilità di apertura" }, sensitivity,
                new TextBlock
                {
                    Text = "Ampia: il pannello si apre anche passando vicino alla linguetta. Precisa: occorre avvicinarsi di più al bordo.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                message, applyButton
            }
        } };
    }
}
