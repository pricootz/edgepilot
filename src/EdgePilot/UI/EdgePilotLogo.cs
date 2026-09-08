using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;

namespace EdgePilot.UI;

/// <summary>
/// Renders the canonical EdgePilot SVG directly as Avalonia vector geometry.
/// This keeps the in-app logo tied to Assets/edgepilot.svg without adding a
/// second raster source or an SVG rendering dependency.
/// </summary>
public sealed class EdgePilotLogo : Viewbox
{
    public EdgePilotLogo()
    {
        Stretch = Stretch.Uniform;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Child = BuildLogo();
    }

    private static Control BuildLogo()
    {
        var canvas = new Canvas
        {
            Width = 1095,
            Height = 1095
        };

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "edgepilot.svg");
            var document = XDocument.Load(path);
            var svg = document.Root ?? throw new InvalidDataException("SVG root missing.");

            var viewBox = ((string?)svg.Attribute("viewBox"))?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (viewBox is { Length: 4 } &&
                double.TryParse(viewBox[2], System.Globalization.CultureInfo.InvariantCulture, out var width) &&
                double.TryParse(viewBox[3], System.Globalization.CultureInfo.InvariantCulture, out var height))
            {
                canvas.Width = width;
                canvas.Height = height;
            }

            foreach (var node in svg.Elements().Where(x => x.Name.LocalName == "path"))
            {
                var data = (string?)node.Attribute("d");
                var fill = (string?)node.Attribute("fill");
                if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(fill)) continue;

                canvas.Children.Add(new Avalonia.Controls.Shapes.Path
                {
                    Data = Geometry.Parse(data),
                    Fill = new SolidColorBrush(Color.Parse(fill)),
                    Stretch = Stretch.None
                });
            }

            if (canvas.Children.Count > 0) return canvas;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException or InvalidDataException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
        }

        return new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new Avalonia.CornerRadius(16),
            Background = new SolidColorBrush(Color.Parse("#252527")),
            Child = new TextBlock
            {
                Text = "E",
                FontSize = 30,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#FF8A3D")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }
}
