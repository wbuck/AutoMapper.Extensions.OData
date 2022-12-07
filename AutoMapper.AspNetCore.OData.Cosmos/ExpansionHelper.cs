#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;


namespace AutoMapper.AspNet.OData;

internal static class ExpansionHelper
{
    public static List<List<ODataExpansionOptions>> GetExpansions<TModel>(this ODataQueryOptions<TModel> options)
    {
        var complexTypeNames = options.Context
            .GetEdmSchemaElementOfType<IEdmComplexType>()
            .Select(e => e.FullTypeName())
            .ToArray();
       
        var test = options.GetSelectedItems().GetExpansions<TModel>(complexTypeNames);

        return test;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions<TModel>(
        this IEnumerable<SelectItem> selectedItems, IReadOnlyList<string> complexTypeNames)
    {
        Type parentType = typeof(TModel);
        List<List<ODataExpansionOptions>>? rootIncludes = null;

        // If there are no selects or only selects for expanded entities,
        // we need to expand the complex types on the root entity.
        if (!selectedItems.Any() || !selectedItems.Any(s => s is PathSelectItem))
        {
            rootIncludes = new();
            rootIncludes.ExpandComplex(null, parentType, complexTypeNames);
        }

        var expansions = selectedItems.GetExpansions(parentType, complexTypeNames);

        if (rootIncludes is not null)
            expansions.InsertRange(0, rootIncludes);

        return expansions;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions(
        this IEnumerable<SelectItem> selectedItems, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
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

                includes.Add(new()
                {
                    MemberType = memberType,
                    ParentType = currentParentType,
                    MemberName = pathSegment.Identifier,
                    FilterOptions = GetFilter()!,
                    QueryOptions = GetQuery()!,
                    Selects = GetSelects()
                });

                if (pathSegment is not NavigationPropertySegment || 
                    (pathSegment is NavigationPropertySegment && !includes.Last().Selects.Any()))
                {
                    expansions.ExpandComplex(includes, elementType, complexTypeNames);
                }

                currentParentType = elementType;


                FilterOptions? GetFilter()
                {
                    if (TryGetNavigationSelectItem(out var item) 
                        && memberType.IsList() && item.FilterOption is not null)
                    {
                        return new FilterOptions(item.FilterOption);
                    }
                    return null;
                }

                QueryOptions? GetQuery()
                {
                    if (TryGetNavigationSelectItem(out var item)
                        && (item.OrderByOption is not null || item.SkipOption.HasValue || item.TopOption.HasValue))
                    {
                        return new QueryOptions(item.OrderByOption!, (int?)item.SkipOption, (int?)item.TopOption);
                    }
                    return null;
                }

                List<string> GetSelects()
                {
                    if (TryGetNavigationSelectItem(out var item))
                        return item.SelectAndExpand.GetSelects();

                    return new();
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


    /// <summary>
    /// Finds all of the complex types in the object hierarchy and builds paths from the base object 
    /// (the object whose declaring type is the same as 'parentType') to the complex type. 
    /// These paths are then used to include those complex objects in the SQL query.
    /// </summary>
    /// 
    /// <remarks>   
    /// Given the classes below with the 'parentType' set to typeof(Entity)
    /// the following include paths will be returned:
    /// 
    /// [0]: FirstComplex/SecondComplex/ThirdComplex
    /// [1]: FirstComplex/FourthComplex    
    /// 
    /// The first segment of both paths (FirstComplex) is the "base object"
    /// whose declaring type is equal to typeof(Entity).
    /// </remarks>
    /// 
    /// <example>        
    /// <![CDATA[
    /// class FourthComplex {}
    /// 
    /// class ThirdComplex {}
    /// 
    /// class SecondComplex
    /// {
    ///     public ThirdComplex ThirdComplex {get; set;} 
    /// }
    /// 
    /// class FirstComplex
    /// {
    ///     public SecondComplex SecondComplex {get; set;}
    ///     public FourthComplex FourthComplex {get; set;}
    /// }
    /// 
    /// class Entity 
    /// {
    ///     public Guid Id {get; set;}
    ///     public FirstComplex FirstComplex {get; set;}
    /// }
    /// ]]>
    /// </example>
    private static void ExpandComplex(
        this List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions>? currentExpansions, Type parentType, IEnumerable<string> complexTypeNames)
    {
        currentExpansions = currentExpansions is null ? new() : new(currentExpansions);
        ExpandComplexInternal(expansions, currentExpansions, parentType, complexTypeNames, 0);

        static void ExpandComplexInternal(List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions> currentExpansions,
            Type parentType, IEnumerable<string> complexTypeNames, int depth)
        {
            var members = parentType.GetComplexMembers(complexTypeNames);            

            for (int i = 0; i < members.Count; ++i)
            {
                var member = members[i];
                var memberType = member.GetMemberType().GetCurrentType();

                var nextExpansions = AddExpansion
                (
                    member, memberType, i == 0 ? currentExpansions : currentExpansions.ResetToMatchingParentType(member)
                );

                ExpandComplexInternal(expansions, nextExpansions, memberType, complexTypeNames, depth + 1);

                if (!nextExpansions.Equals(currentExpansions) || depth == 0)
                    expansions.Add(nextExpansions);
            }            
        }        

        static List<ODataExpansionOptions> AddExpansion(MemberInfo member, Type memberType, List<ODataExpansionOptions> expansions)
        {
            expansions.Add(new()
            {
                MemberName = member.Name,
                MemberType = memberType,
                ParentType = member.DeclaringType,
                Selects = new()
            });
            return expansions;
        }
    }

    private static List<List<ODataExpansionOptions>> GetNestedExpansions(this ExpandedNavigationSelectItem node, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        if (node is null)
            return new();

        return node.SelectAndExpand.SelectedItems
            .GetExpansions(parentType, complexTypeNames);
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

    private static List<ODataExpansionOptions> ResetToMatchingParentType(this List<ODataExpansionOptions> currentExpansions, MemberInfo member) =>
        currentExpansions.FindIndex(e => e.MemberType.GetCurrentType() == member.DeclaringType) switch
        {
            var index when index >= 0 => new(currentExpansions.Take(index + 1)),
            _ => new()
        };

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
