using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnoMech.Scenarios.Fru;

// A multiplayer peer only accepts engine ids its own build declares under a field named for the
// id's kind (see SimAssets). The FRU constants are flat and named for the mechanic instead, so
// they are sorted into kinds here, by their naming convention and id range.
internal static class FruNetAssets
{
    public static readonly ulong[] FruActionIds = Numbers((_, v) => v is >= 39000 and <= 41000);
    public static readonly ulong[] FruBNpcBaseIds = Numbers((_, v) => v is 9020 or (>= 17800 and <= 17900));
    public static readonly ulong[] FruModelCharaIds = Numbers((name, _) => name.EndsWith("Model", StringComparison.Ordinal));
    public static readonly ulong[] FruTetherIds = Numbers((name, _) => name.EndsWith("Tether", StringComparison.Ordinal));
    public static readonly ulong[] FruTimelineIds = Numbers((name, _) =>
        name.EndsWith("Timeline", StringComparison.Ordinal) || name.EndsWith("Loop", StringComparison.Ordinal));
    public static readonly ulong[] FruEObjIds = Numbers((_, v) => v is >= 0x1E0000 and <= 0x1FFFFF);
    public static readonly ulong[] FruVoiceIds = Numbers((name, _) => name.EndsWith("Voice", StringComparison.Ordinal));
    public static readonly string[] FruVfxPaths = Fields()
        .Select(f => f.Value as string)
        .Where(s => s != null && s.EndsWith(".avfx", StringComparison.OrdinalIgnoreCase))
        .Select(s => s!)
        .Distinct()
        .ToArray();

    private static ulong[] Numbers(Func<string, ulong, bool> belongs)
        => Fields().SelectMany(f => Values(f.Value).Where(v => belongs(f.Name, v))).Distinct().ToArray();

    private static IEnumerable<ulong> Values(object? value) => value switch
    {
        byte or ushort or uint or int or long when Convert.ToInt64(value) >= 0 => [(ulong)Convert.ToInt64(value)],
        uint[] items => items.Select(i => (ulong)i),
        ushort[] items => items.Select(i => (ulong)i),
        _ => [],
    };

    private static IEnumerable<(string Name, object? Value)> Fields()
    {
        foreach (var type in typeof(FruNetAssets).Assembly.GetTypes())
        {
            if (type == typeof(FruNetAssets) || type.Namespace?.StartsWith("AnoMech.Scenarios.Fru", StringComparison.Ordinal) != true) continue;
            if (!type.FullName!.Contains("Constants", StringComparison.Ordinal)) continue;
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                object? value;
                try { value = field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null); }
                catch (Exception) { continue; }
                yield return (field.Name, value);
            }
        }
    }
}
