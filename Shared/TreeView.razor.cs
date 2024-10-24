using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obsidian.Shared;

public partial class TreeView<TItem> 
{
    private string _style =>
        new StyleBuilder()
            .AddStyle("height", this.Height, !string.IsNullOrEmpty(this.Height))
            .AddStyle("max-height", this.MaxHeight, !string.IsNullOrEmpty(this.MaxHeight))
            .AddStyle("overflow-y", "scroll")
            .AddStyle(this.Style)
            .Build();

    /// <summary>
    /// Colección de ítems planos que se mostrarán en el árbol.
    /// </summary>
    [Parameter]
    public ICollection<TItem> ItemsFlat { get; set; } = new List<TItem>();

    /// <summary>
    /// Plantilla de renderizado para cada ítem del árbol.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem> ItemTemplate { get; set; }

    /// <summary>
    /// Tamaño de cada ítem en el árbol.
    /// </summary>
    [Parameter]
    public float ItemSize { get; set; } = 50f;

    /// <summary>
    /// Número de ítems visibles alrededor del área de vista.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 5;

    /// <summary>
    /// Altura del árbol.
    /// </summary>
    [Parameter]
    public string Height { get; set; }

    /// <summary>
    /// Altura máxima del árbol.
    /// </summary>
    [Parameter]
    public string MaxHeight { get; set; }

    /// <summary>
    /// Estilos personalizados para el árbol.
    /// </summary>
    [Parameter]
    public string Style { get; set; }

    /// <summary>
    /// Evento llamado cuando se cambia el ítem seleccionado.
    /// </summary>
    [Parameter]
    public EventCallback<TItem> OnSelectedItemChanged { get; set; }

    private async Task OnItemClick(MouseEventArgs e, TItem item) 
    {
        try
        {
            await this.OnSelectedItemChanged.InvokeAsync(item);
        }
        catch (Exception ex)
        {
            // Manejar el error aquí si es necesario
            Console.WriteLine($"Error al manejar el click del ítem: {ex.Message}");
        }
    }
}
