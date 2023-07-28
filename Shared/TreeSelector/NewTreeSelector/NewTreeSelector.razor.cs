using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Linq;

namespace Test2TreeStruct.Shared.TreeSelector.NewTreeSelector
{
    public partial class NewTreeSelector
    {
        [Parameter, EditorRequired]
        public List<TreeSelectItem> Items { get; set; }

        /// <summary>
        /// Lista av de valda itemsen
        /// </summary>
        [Parameter]
        public List<TreeSelectItem> SelectedItems { get; set; } = new();

        /// <summary>
        /// Eventcallback som skickar tillbaka den uppdaterade listan av selected items.
        /// </summary>
        [Parameter]
        public EventCallback<List<TreeSelectItem>> SelectedItemsChanged { get; set; }

        /// <summary>
        /// Eventcallback som skickar tillbaka itemet som lagts till/tagits bort.
        /// Kan användas om du själv vill hantera vad som ska hända när man klickar på ett item.
        /// </summary>
        [Parameter]
        public EventCallback<TreeSelectItem> OnAddedOrRemovedItem { get; set; } = new();

        [Parameter]
        public bool AllowMultiSelect { get; set; }

        /// <summary>
        /// Bestämmer om man kan fällta ut ett item endast genom att klicka på ikonen (true)
        /// Eller om man även kan expandera genom att klicka på raden (false)
        ///
        /// OBS! Om denna är false så kan man inte välja ett item som är en parent.
        /// </summary>
        [Parameter]
        public bool ExpandOnIconOnly { get; set; }

        /// <summary>
        /// Om du klickar på en parent-nod när denna är false, så sätts alla leaf-nodes under den till selected.
        /// Om du klickar på en parent-nod när denna är true, så sätts denna och alla dess children till selected.
        /// </summary>
        [Parameter]
        public bool AllowSelectParents { get; set; }

        /// <summary>
        /// Om du vill söka efter ett namn i trädet, skriv in namnet som ska sökas efter här.
        /// </summary>
        [Parameter]
        public string? SearchQuery { get; set; }

        private HashSet<TreeSelectItem> _selectedItems = new HashSet<TreeSelectItem>();
        private Dictionary<string, TreeSelectItem> _itemsDict = new Dictionary<string, TreeSelectItem>();
        private List<TreeSelectItem> _cachedItems = default!;
        private List<TreeSelectItem> _cachedSelectedItems = default!;
        private List<TreeSelectItem> _flattenedItems = default!;
        private List<TreeSelectItem> _filteredFlattenedItems = default!;
        private string? _cachedSearchQuery;
        protected override void OnInitialized()
        {
            _cachedItems = new();
            _cachedSelectedItems = new();
            _flattenedItems = new();
        }

        protected override void OnParametersSet()
        {
            // Om Item:sen man skickat in har ändrats så vi sätta om en del saker
            // OBS! Om items:en man skickar in inte är platta (rekursiva items med referenser till sina children)
            // så kan det hända att Selector inte hanterar uppdateringar korrekt.
            if (Items != null && !_cachedItems.SequenceEqual(Items))
            {
                UpdateItems();
                UpdateSelectedItems();
            }
            // Om man har skickat in en annan lista av SelectedItems så måste vi också ställa om
            else if (SelectedItems != null && !_cachedSelectedItems.SequenceEqual(SelectedItems))
            {
                UpdateSelectedItems();
            }

            if (string.IsNullOrEmpty(SearchQuery))
            {
                _cachedSearchQuery = SearchQuery;
                _filteredFlattenedItems = new(_flattenedItems);
            }
            else if (SearchQuery != _cachedSearchQuery)
            {
                _filteredFlattenedItems = new(_flattenedItems);
                // Check if the item matches the search query or if any child matches the search query
                SearchForItem(SearchQuery);
                _cachedSearchQuery = SearchQuery;
            }
        }

        private void SearchForItem(string searchQuery)
        {
            _filteredFlattenedItems.Clear();


            foreach (var item in _flattenedItems.Where(x => x.ChildrenIds.Count() > 0 || x.ParentId == null))
            {
                bool itemMatchesSearch = item.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                bool childMatchesSearch = item.ChildrenIds.Any(childId => _itemsDict[childId].Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
                if (itemMatchesSearch || childMatchesSearch)
                {
                    AddItemAndChildrenToFilteredItems(item);
                }
            }
        }

        private void AddItemAndChildrenToFilteredItems(TreeSelectItem item)
        {
            if (!_filteredFlattenedItems.Contains(item))
            {
                _filteredFlattenedItems.Add(item);
            }

            foreach (var childId in item.ChildrenIds)
            {
                var childItem = _itemsDict[childId];
                AddItemAndChildrenToFilteredItems(childItem);
            }
        }

        private void UpdateItems()
        {
            _cachedItems = new List<TreeSelectItem>(Items);

            // Om man skickat in en ItemLista som är rekursiv, t.ex att children innehåller referenser till children.
            if (Items.Count > 0 && Items.Any(x => x.Children.Count > 0))
            {
                _flattenedItems = FlattenTree(Items);
            }
            else
            {
                _flattenedItems = new(Items);
            }
            _itemsDict = _flattenedItems.ToDictionary(x => x.Id, x => x);
        }

        // Om man skickat in Lista av rekursiva items (items innehåller referenser till sina children)
        // så måste vi först platta ut dem innan vi skickar in till TreeStructure
        private List<TreeSelectItem> FlattenTree(List<TreeSelectItem> items)
        {
            var flattenedItems = new List<TreeSelectItem>();
            foreach (var item in items)
            {
                flattenedItems.Add(item);
                if (item.Children.Count > 0)
                {
                    item.ChildrenIds.Clear();
                    item.ChildrenIds.AddRange(item.Children.Select(x => x.Id));
                    flattenedItems.AddRange(FlattenTree(item.Children));
                }
            }
            return flattenedItems;
        }

        private void UpdateSelectedItems()
        {
            DeselectAllItems();
            _selectedItems = new HashSet<TreeSelectItem>(SelectedItems);
            _cachedSelectedItems = new List<TreeSelectItem>(SelectedItems);

            foreach (var selectedItem in SelectedItems)
            {
                if (_itemsDict.TryGetValue(selectedItem.Id, out var foundItem))
                {
                    foundItem.IsSelected = true;
                    SetParentSelectionStatus(foundItem, true);
                }
            }
        }

        private async Task HandleItemSelectionChanged(TreeSelectItem item)
        {
            // Om man klickar på en parent, och man kan ej select:a parents, så deselectar vi alla dess children.
            if (item.IsSelected || item.HasSelectedChild && !AllowSelectParents)
            {
                DeselectItem(item);
            }
            else
            {
                SelectItem(item);
            }

            _cachedSelectedItems = new(_selectedItems);
            await SelectedItemsChanged.InvokeAsync(_selectedItems.ToList());
            await OnAddedOrRemovedItem.InvokeAsync(item);
        }


        // Metod som markerar ett element och dess föräldrar som valda
        private void SelectItem(TreeSelectItem item)
        {
            if (!_selectedItems.Contains(item))
            {
                // Om multi-select ej tillåtet, ta bort alla selected items.
                if (!AllowMultiSelect)
                {
                    DeselectAllItems();
                }

                // Om det tillåts att selecta parents, och denna nod är en parent, select:a denna nod.
                // Eller om det inte är en parent, så selecta noden direkt.
                if (AllowSelectParents || item.ChildrenIds == null || item.ChildrenIds.Count == 0)
                {
                    item.IsSelected = true;
                    _selectedItems.Add(item);
                    SetParentSelectionStatus(item, true);
                }
                // Om man tillåter multiselect, selecta alla leaf-nodes under.
                // (Endast leaf-nodes blir selectade i detta fall. Även om AllowSelectParents = true)
                else if (item.ChildrenIds != null && AllowMultiSelect)
                {
                    SelectLeafNodes(item);
                }
            }
        }

        // Metod som avmarkerar ett element och dess underordnade element
        private void DeselectItem(TreeSelectItem item)
        {
            if (item.IsSelected)
            {
                item.IsSelected = false;
                _selectedItems.Remove(item);
                SetParentSelectionStatus(item, false);
            }
            else if (item.ChildrenIds != null && item.ChildrenIds.Count > 0)
            {
                DeselectLeafNodes(item);
            }
        }

        // Metod som sätter HasSelectedChild åt föräldrarrna för ett Item
        private void SetParentSelectionStatus(TreeSelectItem item, bool selected)
        {
            var parentId = item.ParentId;
            while (parentId != null)
            {
                // Hämta parent från dictionary
                if (_itemsDict.TryGetValue(parentId, out var parent))
                {
                    // Öka/minska mängden selected children
                    if (selected)
                    {
                        parent.SelectedDescendantCount++;
                    }
                    else
                    {
                        parent.SelectedDescendantCount--;
                    }

                    parent.HasSelectedChild = parent.SelectedDescendantCount > 0;
                    parentId = parent.ParentId;
                }
                else
                {
                    break;
                }
            }
        }

        private void DeselectAllItems()
        {
            foreach (var selectedItem in _selectedItems)
            {
                _itemsDict.TryGetValue(selectedItem.Id, out var foundItem);
                if (foundItem != null)
                {
                    foundItem.IsSelected = false;
                    SetParentSelectionStatus(foundItem, false);

                }
            }

            _selectedItems.Clear();
        }

        private void SelectLeafNodes(TreeSelectItem item)
        {
            var leafNodes = GetLeafNodes(item);
            foreach (var leafNode in leafNodes)
                SelectItem(leafNode);
        }

        private void DeselectLeafNodes(TreeSelectItem item)
        {
            var leafNodes = GetLeafNodes(item);
            foreach (var leafNode in leafNodes)
                DeselectItem(leafNode);
        }

        private List<TreeSelectItem> GetLeafNodes(TreeSelectItem item)
        {
            var leafNodes = new List<TreeSelectItem>();
            if (item.ChildrenIds != null)
            {
                foreach (var childId in item.ChildrenIds)
                {
                    var child = _itemsDict[childId];
                    if (child.ChildrenIds == null || child.ChildrenIds.Count == 0)
                        leafNodes.Add(child);
                    else
                        leafNodes.AddRange(GetLeafNodes(child));
                }
            }

            return leafNodes;
        }
    }
}