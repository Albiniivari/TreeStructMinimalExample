
using Test2TreeStruct.Shared.TreeSelector.TreeStructure;

namespace Test2TreeStruct.Shared.TreeSelector.NewTreeSelector
{
    // Modellklass för trädelementet
    public class TreeSelectItem : TreeItemBase
    {
        public TreeSelectItem(string id, string name, string? parentId, List<string>? childrenIds, bool? isExpanded = false) : base(id, name, parentId, childrenIds, isExpanded)
        {
        }
        // Om man skickar in IDs som är ints görs om till string.
        // Skönt att ha möjligheten att ha id som en sträng, och skadar inte ifall man vill använda ints.
        public TreeSelectItem(int id, string name, int? parentId, List<int>? childrenIds, bool? isExpanded = false) : base(id, name, parentId, childrenIds, isExpanded)
        {

        }


        //// Om man vill skicka in ett träd av items. T.ex att varje parent innehåller referens till children.
        public TreeSelectItem(int id, string name, int? parentId, List<TreeSelectItem>? children, bool? isExpanded = false)
        {
            Id = id.ToString();
            Name = name;
            ParentId = parentId == null ? null : parentId?.ToString();
            Children = children ?? new();
            IsExpanded = isExpanded ?? false;
        }
        public TreeSelectItem(string id, string name, string? parentId, List<TreeSelectItem>? children, bool? isExpanded = false)
        {
            Id = id.ToString();
            Name = name;
            ParentId = parentId == null ? null : parentId?.ToString();
            Children = children ?? new();
            IsExpanded = isExpanded ?? false;
        }
        public TreeSelectItem()
        {

        }

        public List<TreeSelectItem> Children { get; set; } = new();

        internal bool IsSelected { get; set; }
        internal bool HasSelectedChild { get; set; }
        internal int SelectedDescendantCount { get; set; }
    }
}
