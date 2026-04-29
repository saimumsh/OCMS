using OptimumCoaching.core;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class RolePermissionHelper
    {
        public IList<RolePermissionGroup> Items { get; private set; } = new List<RolePermissionGroup>();

        public RolePermissionHelper()
        {
            LoadItems();
        }

        private void LoadItems()
        {
            var groupId = 0;
            var childId = 100;

            foreach (var nested in typeof(Permissions)
                         .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
                         .OrderBy(t => t.Name))
            {
                var group = new RolePermissionGroup
                {
                    Id = groupId++,
                    Title = SplitCamelCase(nested.Name),
                    IsSelected = false,
                    Children = new List<RolePermissionItem>()
                };

                foreach (var field in nested
                             .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                             .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string)))
                {
                    var name = (string?)field.GetRawConstantValue();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    group.Children.Add(new RolePermissionItem
                    {
                        Id = childId++,
                        Title = SplitCamelCase(field.Name),
                        ParentId = group.Id,
                        ParentName = group.Title,
                        Name = name,
                        IsSelected = false
                    });
                }

                if (group.Children.Any())
                    Items.Add(group);
            }
        }

        public void LoadRolePermissions(IEnumerable<string>? permissions)
        {
            var set = new HashSet<string>(permissions ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var group in Items)
            {
                var allChecked = group.Children.Any();
                foreach (var child in group.Children)
                {
                    child.IsSelected = set.Contains(child.Name);
                    if (!child.IsSelected) allChecked = false;
                }
                group.IsSelected = allChecked;
            }
        }

        public void Reset()
        {
            foreach (var group in Items)
            {
                group.IsSelected = false;
                foreach (var child in group.Children) child.IsSelected = false;
            }
        }

        public IList<string> CollectSelected()
        {
            var result = new List<string>();
            foreach (var group in Items)
                foreach (var child in group.Children)
                    if (child.IsSelected) result.Add(child.Name);
            return result;
        }

        private static string SplitCamelCase(string input) =>
            string.IsNullOrEmpty(input)
                ? input
                : Regex.Replace(input, "([A-Z])", " $1").Trim();
    }

    public class RolePermissionGroup
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public IList<RolePermissionItem> Children { get; set; } = new List<RolePermissionItem>();
    }

    public class RolePermissionItem
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
