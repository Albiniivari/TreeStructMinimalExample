namespace Test2TreeStruct.Shared.TreeSelector.TreeStructure
{
    public abstract class TreeItemBase
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? ParentId { get; set; }
        public bool IsExpanded { get; set; }
        public int Level { get; set; }

        public List<string> ChildrenIds { get; set; }

        public TreeItemBase()
        {
            ChildrenIds = new List<string>();
        }
        public TreeItemBase(string id, string name, string? parentId, List<string>? childrenIds, bool? isExpanded = false)
        {
            Id = id;
            Name = name;
            ParentId = parentId;
            IsExpanded = isExpanded ?? false;
            ChildrenIds = childrenIds ?? new List<string>();
        }
        // Om man skickar in IDs som är ints görs om till string.
        // Skönt att ha möjligheten att ha id som en sträng, och skadar inte ifall man vill använda ints.
        public TreeItemBase(int id, string name, int? parentId, List<int>? childrenIds, bool? isExpanded = false)
        {
            Id = id.ToString();
            Name = name;
            ParentId = parentId == null ? null : parentId.ToString();
            IsExpanded = isExpanded ?? false;
            ChildrenIds = childrenIds?.Select(x => x.ToString()).ToList() ?? new List<string>();
        }

        public override bool Equals(object? obj)
        {
            if (obj is not TreeItemBase otherItem)
                return false;
            if (Id != otherItem.Id || Name != otherItem.Name || IsExpanded != otherItem.IsExpanded)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}
