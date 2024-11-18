using LeagueToolkit.Core.Wad;
using LeagueToolkit.Hashing;
using System.Text.RegularExpressions;

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
    private static readonly Dictionary<IWadTreePathable, string> pathCache = new();
    private static readonly Dictionary<IWadTreePathable, int> depthCache = new();

    public static string GetPath(this IWadTreePathable pathable) {
        // Retrieve from cache or compute the path
        if (pathCache.TryGetValue(pathable, out var cachedPath))
            return cachedPath;

        string path = pathable.Parent switch {
            null => pathable.Name,
            WadTreeModel => pathable.Name,
            _ => Path.Combine(pathable.Parent.Path, pathable.Name)
        };

        // Cache the computed path for future use
        pathCache[pathable] = path;
        return path;
    }

    public static int GetDepth(this IWadTreePathable pathable) {
        // Retrieve from cache or compute the depth
        if (depthCache.TryGetValue(pathable, out var cachedDepth))
            return cachedDepth;

        int depth = pathable.Parent switch {
            null => 0,
            WadTreeModel => 0,
            _ => pathable.Parent.Depth + 1
        };

        // Cache the computed depth for future use
        depthCache[pathable] = depth;
        return depth;
    }

    public static bool IsEqualTo(this IWadTreePathable pathable, IWadTreePathable other) =>
        pathable.Name == other.Name && pathable.Path == other.Path;

    public static IEnumerable<IWadTreePathable> GetSiblings(this IWadTreePathable pathable) {
        if (pathable.Parent == null) yield break;

        // Iterate through siblings without converting the list to a different type
        foreach (var sibling in pathable.Parent.Items) {
            // Only return if it's not the same object
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
            .Where(child => child is WadTreeItemModel item && item.Type == type);
    }
}
