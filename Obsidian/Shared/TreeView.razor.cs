using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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

    [Parameter]
    public ICollection<TItem> ItemsFlat { get; set; } = new List<TItem>();

    [Parameter]
    public RenderFragment<TItem> ItemTemplate { get; set; }

    [Parameter]
    public float ItemSize { get; set; } = 50f;

    [Parameter]
    public int OverscanCount { get; set; } = 5;

    [Parameter]
    public string Height { get; set; }

    [Parameter]
    public string MaxHeight { get; set; }

    [Parameter]
    public string Style { get; set; }

    [Parameter]
    public EventCallback<HashSet<TItem>> OnSelectedItemsChanged { get; set; }

    private string _searchTerm = string.Empty;
    private HashSet<TItem> _selectedItems = new HashSet<TItem>();

    private IEnumerable<TItem> FilteredItems => 
        ItemsFlat.Where(item => 
            item.ToString().Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));

    private async Task OnItemClick(MouseEventArgs e, TItem item) 
    {
        if (_selectedItems.Contains(item))
        {
            _selectedItems.Remove(item);
        }
        else
        {
            _selectedItems.Add(item);
        }

        await OnSelectedItemsChanged.InvokeAsync(_selectedItems);
    }

    private void UpdateSearchTerm(string searchTerm)
    {
        _searchTerm = searchTerm;
        // Actualización del árbol si es necesario.
    }
}
