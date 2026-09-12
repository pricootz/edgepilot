// Display matching is adapted from Edge-Drop's display resolver:
// https://github.com/Deepender25/Edge-Drop/blob/15ad660bdf643624355c31383c7f2a768c820ea8/electron/main/geometry.ts
// Copyright the Edge-Drop contributors, licensed under Apache-2.0.
// This C#/Avalonia port was modified for persisted monitor choices, four-edge placement,
// cross-platform handles, localized labels, and reconnect-safe fallback behavior.

using System.Globalization;
using Avalonia;
using Avalonia.Platform;
using EdgePilot.Core;

namespace EdgePilot.UI;

public sealed record DisplayRect(int X, int Y, int Width, int Height)
{
    public bool IsValid => Width > 0 && Height > 0;

    public static DisplayRect FromPixelRect(PixelRect value) =>
        new(value.X, value.Y, value.Width, value.Height);
}

public sealed record DisplayTarget(string? SessionId, string? Name, DisplayRect WorkingArea, double Scaling)
{
    // Prevents one of two identical monitors from being mistaken for the other after unplugging.
    public bool NameWasUnique { get; init; }
}

public sealed record DisplaySnapshot(
    string? SessionId,
    string? Name,
    DisplayRect Bounds,
    DisplayRect WorkingArea,
    double Scaling,
    bool IsPrimary)
{
    public bool IsValid => Bounds.IsValid && WorkingArea.IsValid && double.IsFinite(Scaling) && Scaling > 0;

    public static DisplaySnapshot FromScreen(Screen screen)
    {
        var handle = screen.TryGetPlatformHandle();
        var sessionId = handle is null || handle.Handle == IntPtr.Zero
            ? null
            : $"{handle.HandleDescriptor}:{handle.Handle.ToInt64():X}";

        return new DisplaySnapshot(
            sessionId,
            Normalize(screen.DisplayName),
            DisplayRect.FromPixelRect(screen.Bounds),
            DisplayRect.FromPixelRect(screen.WorkingArea),
            screen.Scaling,
            screen.IsPrimary);
    }

    internal static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum DisplayMatchKind
{
    AutomaticPrimary,
    SessionId,
    Name,
    Geometry,
    FallbackPrimary
}

public sealed record DisplayResolution(int Index, DisplaySnapshot Display, DisplayMatchKind MatchKind);

public static class DisplayTargetResolver
{
    public const int GeometryTolerance = 8;
    private const double ScalingTolerance = 0.01;

    public static DisplayTarget Capture(DisplaySnapshot display, IReadOnlyList<DisplaySnapshot>? topology = null)
    {
        topology ??= [display];
        var name = DisplaySnapshot.Normalize(display.Name);
        var nameWasUnique = name is not null && topology.Count(candidate =>
            string.Equals(DisplaySnapshot.Normalize(candidate.Name), name, StringComparison.OrdinalIgnoreCase)) == 1;
        return new DisplayTarget(display.SessionId, name, display.WorkingArea, display.Scaling)
        {
            NameWasUnique = nameWasUnique
        };
    }

    public static DisplayResolution? Resolve(IReadOnlyList<DisplaySnapshot> displays, DisplayTarget? target)
    {
        var valid = displays
            .Select((display, index) => (Display: display, Index: index))
            .Where(entry => entry.Display.IsValid)
            .ToArray();
        if (valid.Length == 0) return null;

        var primary = valid.FirstOrDefault(entry => entry.Display.IsPrimary);
        if (primary == default) primary = valid[0];

        if (target is null)
            return Result(primary, DisplayMatchKind.AutomaticPrimary);

        var sessionId = DisplaySnapshot.Normalize(target.SessionId);
        if (sessionId is not null)
        {
            var exact = valid.FirstOrDefault(entry =>
                string.Equals(entry.Display.SessionId, sessionId, StringComparison.Ordinal));
            if (exact != default) return Result(exact, DisplayMatchKind.SessionId);
        }

        var name = DisplaySnapshot.Normalize(target.Name);
        if (name is not null)
        {
            var named = valid.Where(entry =>
                    string.Equals(entry.Display.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (target.NameWasUnique && named.Length == 1)
                return Result(named[0], DisplayMatchKind.Name);

            var namedGeometry = BestGeometryMatch(named, target);
            if (namedGeometry is not null) return Result(namedGeometry.Value, DisplayMatchKind.Geometry);
        }

        var geometry = BestGeometryMatch(valid, target);
        return geometry is not null
            ? Result(geometry.Value, DisplayMatchKind.Geometry)
            : Result(primary, DisplayMatchKind.FallbackPrimary);
    }

    private static (DisplaySnapshot Display, int Index)? BestGeometryMatch(
        IReadOnlyList<(DisplaySnapshot Display, int Index)> displays,
        DisplayTarget target)
    {
        var matches = displays
            .Where(entry => Near(entry.Display.WorkingArea.X, target.WorkingArea.X)
                            && Near(entry.Display.WorkingArea.Y, target.WorkingArea.Y)
                            && Near(entry.Display.WorkingArea.Width, target.WorkingArea.Width)
                            && Near(entry.Display.WorkingArea.Height, target.WorkingArea.Height))
            .OrderBy(entry => Math.Abs(entry.Display.Scaling - target.Scaling) <= ScalingTolerance ? 0 : 1)
            .ThenBy(entry => GeometryDistance(entry.Display.WorkingArea, target.WorkingArea))
            .ThenBy(entry => entry.Index)
            .ToArray();

        if (matches.Length == 0) return null;
        if (matches.Length > 1)
        {
            var scaleMatches = matches.Where(entry =>
                Math.Abs(entry.Display.Scaling - target.Scaling) <= ScalingTolerance).ToArray();
            if (scaleMatches.Length == 1) return scaleMatches[0];
        }

        return matches[0];
    }

    private static bool Near(int current, int saved) => Math.Abs(current - saved) <= GeometryTolerance;

    private static long GeometryDistance(DisplayRect current, DisplayRect saved) =>
        Math.Abs((long)current.X - saved.X) +
        Math.Abs((long)current.Y - saved.Y) +
        Math.Abs((long)current.Width - saved.Width) +
        Math.Abs((long)current.Height - saved.Height);

    private static DisplayResolution Result((DisplaySnapshot Display, int Index) entry, DisplayMatchKind kind) =>
        new(entry.Index, entry.Display, kind);
}

internal sealed record DisplayChoice(DisplayTarget? Target, string Caption, bool IsAvailable)
{
    public override string ToString() => Caption;
}

internal sealed record DisplayOptions(IReadOnlyList<DisplayChoice> Choices, DisplayChoice Selected);

internal static class DisplaySelection
{
    public static DisplayOptions Build(IReadOnlyList<DisplaySnapshot> displays, DisplayTarget? selected)
    {
        var valid = displays
            .Where(display => display.IsValid)
            .OrderByDescending(display => display.IsPrimary)
            .ThenBy(display => display.Bounds.X)
            .ThenBy(display => display.Bounds.Y)
            .ToArray();

        var automatic = new DisplayChoice(null, Localization.T("display.automatic"), true);
        var choices = new List<DisplayChoice> { automatic };
        var captions = valid.Select(Caption).ToArray();
        var captionCounts = captions.GroupBy(caption => caption, StringComparer.CurrentCultureIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.CurrentCultureIgnoreCase);
        var captionOrdinals = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        for (var index = 0; index < valid.Length; index++)
        {
            var caption = captions[index];
            if (captionCounts[caption] > 1)
            {
                captionOrdinals.TryGetValue(caption, out var ordinal);
                captionOrdinals[caption] = ++ordinal;
                caption = Localization.T("display.numberedCaption", caption, ordinal);
            }

            choices.Add(new DisplayChoice(
                DisplayTargetResolver.Capture(valid[index], valid), caption, true));
        }

        if (selected is null) return new DisplayOptions(choices, automatic);

        var resolution = DisplayTargetResolver.Resolve(valid, selected);
        if (resolution is not null && resolution.MatchKind != DisplayMatchKind.FallbackPrimary)
            return new DisplayOptions(choices, choices[resolution.Index + 1]);

        var unavailable = new DisplayChoice(selected, UnavailableCaption(selected), false);
        choices.Insert(1, unavailable);
        return new DisplayOptions(choices, unavailable);
    }

    private static string Caption(DisplaySnapshot display)
    {
        var name = display.Name ?? Localization.T("display.unnamed");
        var primary = display.IsPrimary ? Localization.T("display.primarySuffix") : "";
        return Localization.T(
            "display.caption",
            name,
            display.Bounds.Width,
            display.Bounds.Height,
            display.Scaling.ToString("0.##", CultureInfo.CurrentCulture),
            primary);
    }

    private static string UnavailableCaption(DisplayTarget selected)
    {
        var name = DisplaySnapshot.Normalize(selected.Name) ??
                   $"{selected.WorkingArea.Width} × {selected.WorkingArea.Height}";
        return Localization.T("display.unavailable", name);
    }
}
