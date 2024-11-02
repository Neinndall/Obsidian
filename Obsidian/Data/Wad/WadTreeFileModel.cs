using LeagueToolkit.Core.Wad;
using System.Diagnostics;

namespace Obsidian.Data.Wad;

[DebuggerDisplay("{Name}")]
public sealed class WadTreeFileModel : WadTreeItemModel {
    public WadChunk Chunk { get; }
    public WadFile Wad { get; }

    public WadTreeFileModel(IWadTreeParent parent, string name, WadFile wad, WadChunk chunk)
        : base(parent, name) {
        // Verifica que el nombre no sea nulo o vacío
        if (string.IsNullOrEmpty(name)) {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        // Aquí podrías verificar si el chunk es válido mediante una propiedad personalizada
        if (!IsValidChunk(chunk)) {
            throw new ArgumentException("Chunk is invalid.", nameof(chunk));
        }

        if (wad == null) {
            throw new ArgumentNullException(nameof(wad));
        }

        this.Chunk = chunk;
        this.Wad = wad;
    }

    // Método para verificar la validez del WadChunk
    private bool IsValidChunk(WadChunk chunk) {
        // Implementa la lógica para determinar si el chunk es válido
        // Por ejemplo, verifica si tiene propiedades en un estado que consideres válido
        return true; // Cambia esto según tu lógica
    }
}
