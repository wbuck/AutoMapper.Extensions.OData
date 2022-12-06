#nullable enable

using LogicBuilder.Expressions.Utils;
using LogicBuilder.Expressions.Utils.Expansions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;


namespace AutoMapper.AspNet.OData;

internal static class ExpansionHelper
{
    public static List<List<ODataExpansionOptions>> GetExpansions<TModel>(this ODataQueryOptions<TModel> options)
    {
        var complexTypeNames = options.Context
            .GetEdmSchemaElementOfType<IEdmComplexType>()
            .Select(e => e.FullTypeName())
            .ToArray();

        var parentType = typeof(TModel);

        var test = options.GetSelectedItems().GetExpansions(parentType, complexTypeNames);

        return test;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions(this IEnumerable<SelectItem> selectedItems, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        if (!selectedItems.Any())
        {
            // TODO - If there are no selects we still need to expand all of the complex types.
            return new();
        }

        return selectedItems.Aggregate(new List<List<ODataExpansionOptions>>(), (expansions, next) =>
        {
            Type currentParentType = parentType;
            List<ODataExpansionOptions> includes = new();

            foreach (var pathSegment in next.GetPathSegments())
            {
                Type memberType = currentParentType.GetMemberInfo(pathSegment.Identifier).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                if (elementType.IsLiteralType())                
                    continue;

                var selectedMembers = GetSelects();

                includes.Add(new()
                {
                    MemberType = memberType,
                    ParentType = currentParentType,
                    MemberName = pathSegment.Identifier,
                    FilterOptions = GetFilter(),
                    QueryOptions = GetQuery(),
                    Selects = selectedMembers
                });

                foreach (var member in elementType.GetComplexMembers(complexTypeNames, selectedMembers))                
                    includes.ExpandComplexTypeHierarchy(member, complexTypeNames);                

                currentParentType = elementType;


                FilterOptions? GetFilter()
                {
                    if (!HasFilter())
                        return null;

                    var item = (ExpandedNavigationSelectItem)next;
                    return new FilterOptions(item.FilterOption);
                }

                QueryOptions? GetQuery()
                {
                    if (!HasQuery())
                        return null;

                    var item = (ExpandedNavigationSelectItem)next;
                    return new QueryOptions(item.OrderByOption, (int?)item.SkipOption, (int?)item.TopOption);
                }

                List<string> GetSelects()
                {
                    if (TryGetNavigationSelectItem(out var item))
                        return item.SelectAndExpand.GetSelects();

                    return new();
                }

                bool HasFilter()
                {
                    if (TryGetNavigationSelectItem(out var item))                    
                        return memberType.IsList() && item.FilterOption is not null;
                                          
                    return false;
                }

                bool HasQuery()
                {                    
                    if (TryGetNavigationSelectItem(out var item))
                    {
                        return memberType.IsList() && 
                            (item.OrderByOption is not null || item.SkipOption.HasValue || item.TopOption.HasValue);
                    }
                    return false;
                }

                bool TryGetNavigationSelectItem([MaybeNullWhen(false)] out ExpandedNavigationSelectItem item)
                {
                    if (pathSegment is NavigationPropertySegment)
                    {
                        item = (ExpandedNavigationSelectItem)next;
                        return true;
                    }
                    item = null;
                    return false;
                }
            }

            var navigationItem = next as ExpandedNavigationSelectItem;
            var navigationItems = navigationItem?.GetNestedExpansions(currentParentType, complexTypeNames).Select
            (
                expansions =>
                {
                    if (includes.Any())
                        expansions.InsertRange(0, includes);
                
                    return expansions;
                }
            ).ToList();

            if (navigationItems is not null && navigationItems.Any())
                expansions.AddRange(navigationItems);
            else if (includes.Any())
                expansions.Add(includes);

            return expansions;            
        });
    }

    private static void ExpandComplexTypeHierarchy(this List<ODataExpansionOptions> expansions, MemberInfo member, IEnumerable<string> complexTypeNames)
    {
        var actualType = member.GetMemberType().GetCurrentType();

        expansions.Add(new ODataExpansionOptions
        {
            MemberName = member.Name,
            MemberType = actualType,
            ParentType = member.DeclaringType,
            Selects = new()
        });

        foreach (var nextMember in actualType.GetComplexMembers(complexTypeNames))
            expansions.ExpandComplexTypeHierarchy(nextMember, complexTypeNames);
    }

    private static List<List<ODataExpansionOptions>> GetNestedExpansions(this ExpandedNavigationSelectItem node, Type type, IReadOnlyList<string> complexTypeNames)
    {
        if (node is null)
            return new();

        return node.SelectAndExpand.SelectedItems.GetExpansions(type, complexTypeNames);
    }


    private static List<string> GetSelects(this SelectExpandClause clause)
    {
        if (clause == null)
            return new List<string>();

        return clause.SelectedItems
            .OfType<PathSelectItem>()
            .Select(item => item.SelectedPath.FirstSegment.Identifier)//Only first segment is necessary because of the new syntax $expand=Builder($expand=City) vs $expand=Builder/City
            .ToList();
    }

    private static IEnumerable<ODataPathSegment> GetPathSegments(this SelectItem selectItem) => 
        selectItem switch
        {
            PathSelectItem item => item.SelectedPath,
            ExpandedNavigationSelectItem item => item.PathToNavigationProperty,
            _ => throw new NotImplementedException()
        };

    private static IEnumerable<SelectItem> GetSelectedItems<TModel>(this ODataQueryOptions<TModel> options) =>
        options.SelectExpand?.SelectExpandClause?.SelectedItems ?? Enumerable.Empty<SelectItem>();
}
