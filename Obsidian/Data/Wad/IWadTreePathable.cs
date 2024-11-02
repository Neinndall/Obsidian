using System.Collections.Generic;

namespace Obsidian.Data.Wad;

public interface IWadTreePathable {
    IWadTreeParent Parent { get; }
    int Depth { get; }
    string Name { get; }
    string Path { get; }
    ulong NameHash { get; }
    ulong PathHash { get; }
    bool IsWadArchive { get; }
}

public static class IWadTreePathableExtensions {
    public static string GetPath(this IWadTreePathable pathable) =>
        pathable.Parent switch {
            null => pathable.Name,
            WadTreeModel => pathable.Name,
            _ => Path.Combine(pathable.Parent.Path, pathable.Name) // Mejor uso de Path.Combine
        };

    public static int GetDepth(this IWadTreePathable pathable) =>
        pathable.Parent switch {
            null => 0,
            WadTreeModel => 0,
            _ => pathable.Parent.Depth + 1,
        };

    public static bool IsEqualTo(this IWadTreePathable pathable, IWadTreePathable other) =>
        pathable.Name == other.Name && pathable.Path == other.Path;

    public static IEnumerable<IWadTreePathable> GetSiblings(this IWadTreePathable pathable) {
        if (pathable.Parent == null) yield break;

        foreach (var sibling in pathable.Parent.Items.OfType<WadTreeItemModel>()) {
            if (!ReferenceEquals(sibling, pathable)) yield return sibling;
        }
    }

    public static IEnumerable<IWadTreePathable> GetChildren(this IWadTreePathable pathable) {
        if (pathable is WadTreeItemModel folder && folder.Type == WadTreeItemType.Directory) {
            foreach (var child in folder.Items) {
                yield return child;
            }
        }
    }

    public static IEnumerable<IWadTreePathable> FilterByType(this IWadTreePathable pathable, WadTreeItemType type) {
        return pathable.GetChildren()
            .OfType<WadTreeItemModel>()
            .Where(child => child.Type == type);
    }
}
