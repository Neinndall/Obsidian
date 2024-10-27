namespace Obsidian.Data.Wad;

public interface IWadTreePathable {
    IWadTreeParent Parent { get; }
    int Depth { get; }

    string Name { get; }
    string Path { get; }
    ulong NameHash { get; }
    ulong PathHash { get; }

    public bool IsWadArchive { get; }
}

public static class IWadTreePathableExtensions {
    public static string GetPath(this IWadTreePathable pathable) =>
        pathable.Parent switch {
            null => pathable.Name,
            WadTreeModel => pathable.Name,
            _ => string.Join('/', pathable.Parent.Path, pathable.Name)
        };

    public static int GetDepth(this IWadTreePathable pathable) =>
        pathable.Parent switch {
            null => 0,
            WadTreeModel => 0,
            _ => pathable.Parent.Depth + 1,
        };

    // Método para comparar nodos
    public static bool IsEqualTo(this IWadTreePathable pathable, IWadTreePathable other) =>
        pathable.Name == other.Name && pathable.Path == other.Path;

    // Método para obtener hermanos
    public static IEnumerable<IWadTreePathable> GetSiblings(this IWadTreePathable pathable) {
        if (pathable.Parent == null) yield break;

        foreach (var sibling in pathable.Parent.Items.OfType<WadTreeItemModel>()) {
            // Comparamos las instancias usando Equals para una comparación de valores
            if (!ReferenceEquals(sibling, pathable)) yield return sibling;
        }
    }

    // Método para obtener hijos
    public static IEnumerable<IWadTreePathable> GetChildren(this IWadTreePathable pathable) {
        if (pathable is WadTreeItemModel folder && folder.Type == WadTreeItemType.Directory) {
            foreach (var child in folder.Items) {
                yield return child;
            }
        }
    }

    // Método para filtrar por tipo
    public static IEnumerable<IWadTreePathable> FilterByType(this IWadTreePathable pathable, WadTreeItemType type) {
        return pathable.GetChildren()
            .OfType<WadTreeItemModel>() // Asegúrate de que solo estás trabajando con WadTreeItemModel
            .Where(child => child.Type == type);
    }
}
