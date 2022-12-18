#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.RecordIO;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoMapper.AspNet.OData;

internal static partial class ExpansionHelper
{
    public static List<List<ODataExpansionOptions>> GetExpansions<TModel>(this ODataQueryOptions<TModel> options)
    {
        var complexTypeNames = options.Context
            .GetEdmSchemaElementOfType<IEdmComplexType>()
            .Select(e => e.Name)
            .ToArray();

        

        Type parentType = typeof(TModel);

        IEdmModel edmModel = options.Context.Model;

        var selectPaths = options.GetSelects<PathSelectItem>()
            .ToList().BuildSelectPaths(parentType, edmModel, new(), new());

        // TODO: - If there are no selects then create selects for each member of the root model.
        if (!selectPaths.Any())
        {
            selectPaths = parentType.GetLiteralAndComplexSelects(edmModel);
        }
      
        var expansionPaths = options.GetSelects<ExpandedNavigationSelectItem>()
            .ToList().BuildExpansionPaths(parentType, edmModel, new(), new());
        
        //selectPaths.AddRange(
        //    expansionPaths.Where(expansion => !expansion.Last().IsExpansionSegment));
        //
        //expansionPaths
        //    .RemoveAll(expansion => !expansion.Last().IsExpansionSegment);

        Debugger.Break();

        //var expansions = options
        //    .GetSelectedItems()
        //    .ParseSelects(parentType)
        //    .GetExpansions<TModel>(complexTypeNames);

        return null;
        //return options.GetSelectedItems().GetExpansions<TModel>(complexTypeNames);
    }

    private static List<List<ODataExpansionOptions>> GetExpansions<TModel>(
        this IDictionary<string, SelectItemPathSegments> selectPaths, IReadOnlyList<string> complexTypeNames)
    {
        Type parentType = typeof(TModel);

        var expansions = selectPaths.GetExpansions(parentType, complexTypeNames);

        return expansions;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions(
        this IDictionary<string, SelectItemPathSegments> selectPaths, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        return selectPaths.Aggregate(new List<List<ODataExpansionOptions>>(), (expansions, values) =>
        {
            var (_, pathSegments) = values;
            Type currentParentType = parentType;
            List<ODataExpansionOptions> includes = new();

            foreach (var pathSegment in pathSegments)
            {
                Type memberType = currentParentType.GetMemberInfo(pathSegment.Identifier).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                if (elementType.IsLiteralType())
                    continue;

                if (!pathSegment.IsNavigationPropertySegment ||
                    (pathSegment.IsNavigationPropertySegment && pathSegments.IsExpandedNavigationSelectItem))
                {
                    includes.Add(new()
                    {
                        MemberType = memberType,
                        ParentType = currentParentType,
                        MemberName = pathSegment.Identifier,
                        FilterOptions = pathSegment.Filter!,
                        QueryOptions = pathSegment.Query!,
                        Selects = pathSegment.Selects
                    });

                    if (!pathSegment.Selects.Any())
                    {
                        if (!pathSegment.IsNavigationPropertySegment || !pathSegments.HasExpansions())
                        {
                            var complexExpansions = elementType.ExpandComplexTypes(complexTypeNames, pathSegment.PropertiesToIgnore).Select
                            (
                                expansions =>
                                {
                                    expansions.InsertRange(0, includes);
                                    return expansions;
                                }
                            ).ToList();

                            if (complexExpansions.Any())
                                expansions.AddRange(complexExpansions);
                        }
                    }
                }
                currentParentType = elementType;
            }

            var navigationItems = pathSegments.GetNestedExpansions(currentParentType, complexTypeNames).Select
            (
                expansions =>
                {
                    if (includes.Any())
                        expansions.InsertRange(0, includes);
            
                    return expansions;
                }
            ).ToList();
            
            if (navigationItems.Any())
                expansions.AddRange(navigationItems);
            else if (includes.Any())
                expansions.Add(includes);

            return expansions;
        });
    }

    private static List<List<ODataExpansionOptions>> GetExpansions<TModel>(
        this IEnumerable<SelectItem> selectedItems, IReadOnlyList<string> complexTypeNames)
    {
        Type parentType = typeof(TModel);
        List<List<ODataExpansionOptions>>? rootIncludes = null;

        // If there are no selects or only selects for expanded entities,
        // we need to expand the complex types on the root entity.
        if (!selectedItems.Any() || !selectedItems.Any(s => s is PathSelectItem))        
            rootIncludes = parentType.ExpandComplexTypes(complexTypeNames, null);

        var pathSegments = selectedItems.ToExpansionSegments();
        var expansions = pathSegments.GetExpansions(parentType, complexTypeNames);

        if (rootIncludes is not null)
            expansions.InsertRange(0, rootIncludes);

        return expansions;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions(
        this IImmutableSet<SelectPathSegments> selectedItems, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        return selectedItems.Aggregate(new List<List<ODataExpansionOptions>>(), (expansions, pathSegments) =>
        {
            Type currentParentType = parentType;
            List<ODataExpansionOptions> includes = new();

            foreach (var pathSegment in pathSegments)
            {
                Type memberType = currentParentType.GetMemberInfo(pathSegment.Identifier).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                if (elementType.IsLiteralType())
                    continue;

                if (!pathSegment.IsNavigationPropertySegment() || 
                    (pathSegment.IsNavigationPropertySegment() && pathSegments.IsExpandedNavigationSelectItem()))
                {
                    includes.Add(new()
                    {
                        MemberType = memberType,
                        ParentType = currentParentType,
                        MemberName = pathSegment.Identifier,
                        FilterOptions = pathSegment.GetFilter(memberType)!,
                        QueryOptions = pathSegment.GetQuery()!,
                        Selects = pathSegment.GetSelects()
                    });

                    if (!pathSegment.HasSelects())
                    {
                        var complexExpansions = elementType.ExpandComplexTypes(complexTypeNames, null).Select
                        (
                            expansions =>
                            {
                                expansions.InsertRange(0, includes);
                                return expansions;
                            }
                        ).ToList();
                    
                        if (complexExpansions.Any())
                            expansions.AddRange(complexExpansions);
                    }                    
                }

                currentParentType = elementType;
            }

            var navigationItems = pathSegments.GetNestedExpansions(currentParentType, complexTypeNames).Select
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

    private static List<List<ODataExpansionOptions>> GetNestedExpansions(this SelectItemPathSegments pathSegments, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        if (pathSegments.SelectItem is ExpandedNavigationSelectItem navigationSelectItem)
        {
            return navigationSelectItem.SelectAndExpand.SelectedItems
                .ParseSelects(parentType)
                .GetExpansions(parentType, complexTypeNames);
        }
        return new();
    }

    private static List<List<ODataExpansionOptions>> GetNestedExpansions(in this SelectPathSegments pathSegments, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        var selects = pathSegments.GetNestedExpansionSegments();
        return selects.GetExpansions(parentType, complexTypeNames);
    }

    private record PS
    {
        public PS(ODataPathSegment segment, List<string>? selects, FilterOptions? filter, QueryOptions? query, List<string>? propertiesToIgnore = null)
        {
            Segment = segment;
            Selects = selects ?? new();
            Filter = filter;
            Query = query;
            PropertiesToIgnore = propertiesToIgnore ?? new();
        }

        public string Identifier => 
            Segment.Identifier;

        public ODataPathSegment Segment { get; }
         
        public bool IsNavigationPropertySegment =>
            Segment is NavigationPropertySegment;

        public List<string> Selects { get; }

        public FilterOptions? Filter { get; }

        public QueryOptions? Query { get; }

        public List<string> PropertiesToIgnore { get; }
    }

    private sealed class SelectItemPathSegments : List<PS>
    {        
        public SelectItemPathSegments(SelectItem selectItem) =>
            SelectItem = selectItem;

        public SelectItem SelectItem { get; }

        public bool IsExpandedNavigationSelectItem =>
            SelectItem is ExpandedNavigationSelectItem;

        public bool HasExpansions()
        {
            if (SelectItem is ExpandedNavigationSelectItem navigation)            
                return navigation.SelectAndExpand.SelectedItems.Any();

            return false;
        }
    }

#if true

    private static List<List<PathSegment>> BuildExpansionPaths(
        this IReadOnlyList<ExpandedNavigationSelectItem> selectItems,
        Type parentType,
        IEdmModel edmModel,
        List<List<PathSegment>> paths,
        List<PathSegment> currentExpansionPath,
        int depth = 0)
    {
        if (!selectItems.Any())
            return paths;

        List<PathSegment> newExpansionPath = depth switch
        {
            > 0 => new(currentExpansionPath),
            _ => currentExpansionPath
        };

        BuildPathSegments(selectItems.First(), newExpansionPath, depth);

        if (depth == 0 || !currentExpansionPath.Equals(newExpansionPath))
            paths.Add(newExpansionPath);

        foreach (var selectItem in selectItems.Skip(1))
        {
            paths.Add(BuildPathSegments(selectItem,
                newExpansionPath.Take(depth).ToList(), depth));
        }

        return paths;

        List<PathSegment> BuildPathSegments(ExpandedNavigationSelectItem pathSegments, List<PathSegment> path, int depth)
        {
            Type rootType = parentType;
            foreach (var pathSegment in pathSegments.PathToNavigationProperty)
            {
                Type memberType = rootType.GetMemberInfo(pathSegment.Identifier).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                path.Add(new
                (
                    true,
                    pathSegment.Identifier,
                    rootType,
                    memberType,
                    pathSegment.EdmType.TypeKind,
                    edmModel,
                    pathSegment.GetFilter(pathSegments),
                    pathSegment.GetQuery(pathSegments),
                    pathSegment.GetSelects(pathSegments, elementType, edmModel)
                ));

                rootType = elementType;
            }
            
            pathSegments.GetSelects<ExpandedNavigationSelectItem>()
                .ToList().BuildExpansionPaths(rootType, edmModel, paths, path, depth + 1);

            return path;
        }       
    }

    private static List<List<PathSegment>> GetLiteralAndComplexSelects(this Type parentType, IEdmModel edmModel) =>
        parentType.GetLiteralSelects(edmModel).Concat(edmModel.GetComplexTypeSelects(parentType)).ToList();

    private static List<List<PathSegment>>? GetSelects(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments, Type parentType, IEdmModel edmModel)
    {
        if (pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier))
        {
            return pathSegments.GetSelects<PathSelectItem>().ToList() switch
            {
                var selects when selects.Any() => selects.BuildSelectPaths(parentType, edmModel, new(), new()),
                _ => parentType.GetLiteralAndComplexSelects(edmModel)
            };
        }
        return null;
    }

    private static List<List<PathSegment>> BuildSelectPaths(
        this IReadOnlyList<PathSelectItem> selectedPaths,        
        Type parentType,
        IEdmModel edmModel,
        List<List<PathSegment>> paths,
        List<PathSegment> currentPath,
        int depth = 0,
        in int start = 0)
    {
        for (int i = 0; i < selectedPaths.Count; ++i)
        {
            List<PathSegment> segments = i == 0 ? currentPath : currentPath.Take(depth).ToList();
            segments = BuildPathSegments
            (
                selectedPaths[i], 
                segments, 
                depth, 
                start
            );

            if (depth == start || !segments.Equals(currentPath))
            {
                if (!segments.Last().IsComplex)
                    paths.Add(segments);
                else
                {
                    PathSegment pathSegment = segments.Last();
                    Type memberType = pathSegment.MemberType;

                    var literalMembers = memberType.GetLiteralTypeMembers();

                    var memberSelects = literalMembers
                        .Select(m => AddExpansion(m, m.GetMemberType().GetCurrentType(), EdmTypeKind.Primitive, new(segments)));

                    var complexPaths = edmModel.GetComplexTypeSelects(pathSegment.MemberType).Select
                    (
                        paths =>
                        {
                            paths.InsertRange(0, segments);
                            return paths;
                        }
                    );

                    paths.AddRange(memberSelects.Concat(complexPaths));

                    List<PathSegment> AddExpansion(MemberInfo member, Type memberType, EdmTypeKind edmTypeKind, List<PathSegment> pathSegments)
                    {
                        pathSegments.Add(new
                        (
                            false,
                            member.Name,
                            memberType.DeclaringType!,
                            memberType,
                            edmTypeKind,
                            edmModel
                        ));
                        return pathSegments;
                    }
                }
            }
        }

        return paths;

        List<PathSegment> BuildPathSegments(PathSelectItem pathSegments, List<PathSegment> path, int depth, int start)
        {
            Type rootType = parentType;
            foreach (var pathSegment in pathSegments.SelectedPath)
            {
                Type memberType = rootType.GetMemberInfo(pathSegment.Identifier).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                path.Add(new
                (
                    false,
                    pathSegment.Identifier,
                    rootType,
                    memberType,
                    pathSegment.EdmType.TypeKind,
                    edmModel,
                    pathSegment.GetFilter(pathSegments),
                    pathSegment.GetQuery(pathSegments)
                ));

                rootType = elementType;
            }            

            pathSegments.GetSelects<PathSelectItem>().ToList()
                .BuildSelectPaths(rootType, edmModel, paths, path, depth + 1, start);

            return path;
        }        
    }

    private static FilterOptions? GetFilter(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments)
    {
        if (pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier)
            && pathSegments.FilterOption is not null)
        {
            return new(pathSegments.FilterOption);
        }
        return null;
    }

    private static QueryOptions? GetQuery(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments)
    {
        if (!pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier))
            return null;

        if (pathSegments.OrderByOption is not null || pathSegments.SkipOption.HasValue || pathSegments.TopOption.HasValue)
            return new(pathSegments.OrderByOption!, (int?)pathSegments.SkipOption, (int?)pathSegments.TopOption);

        return null;
    }

    private static FilterOptions? GetFilter(this ODataPathSegment pathSegment, PathSelectItem pathSegments)
    {
        if (pathSegments.SelectedPath.Last().Identifier.Equals(pathSegment.Identifier)
            && pathSegments.FilterOption is not null)
        {
            return new(pathSegments.FilterOption);
        }

        return null;
    }

    private static QueryOptions? GetQuery(this ODataPathSegment pathSegment, PathSelectItem pathSegments)
    {
        if (!pathSegments.SelectedPath.Last().Identifier.Equals(pathSegment.Identifier))
            return null;

        if (pathSegments.OrderByOption is not null || pathSegments.SkipOption.HasValue || pathSegments.TopOption.HasValue)
            return new(pathSegments.OrderByOption!, (int?)pathSegments.SkipOption, (int?)pathSegments.TopOption);

        return null;
    }


#endif

#if false
    private static List<List<ODataPathSegment>> BuildExpansionPaths(
        this IReadOnlyList<ExpandedNavigationSelectItem> selectItems, 
        List<List<ODataPathSegment>> paths, 
        List<ODataPathSegment> currentPath, 
        int depth = 0)
    {
        if (!selectItems.Any())
            return paths;

        BuildPaths(selectItems.FirstOrDefault(), currentPath, depth);
        
        if (depth == 0)
            paths.Add(currentPath);

        foreach (var selectItem in selectItems.Skip(1))
        {
            paths.Add(BuildPaths(selectItem, 
                currentPath.Take(depth).ToList(), depth));
        }

        List<ODataPathSegment> BuildPaths(ExpandedNavigationSelectItem item, List<ODataPathSegment> path, int depth)
        {
            path.AddRange(item.PathToNavigationProperty);

            if (item.SelectAndExpand?.SelectedItems.Any() == true)
            {
                int count = item.PathToNavigationProperty.Count;
                item.GetSelects<PathSelectItem>()
                    .ToList().BuildSelectPaths(paths, new(path), count, count);
            }

            item.GetSelects<ExpandedNavigationSelectItem>()
                .ToList().BuildExpansionPaths(paths, path, depth + 1);

            return path;
        }

        return paths;
    }

    private static List<List<ODataPathSegment>> BuildSelectPaths(
        this IReadOnlyList<PathSelectItem> selectItems, 
        List<List<ODataPathSegment>> paths, 
        List<ODataPathSegment> currentPath, 
        int depth = 0, 
        in int start = 0)
    {
        if (!selectItems.Any())
            return paths;

        BuildPaths(selectItems.First(), currentPath, depth, start);

        if (depth == start)
            paths.Add(currentPath);

        foreach (var selectItem in selectItems.Skip(1))
        {
            paths.Add(BuildPaths(selectItem, currentPath.Take(depth).ToList(), depth, start));
        }

        List<ODataPathSegment> BuildPaths(PathSelectItem item, List<ODataPathSegment> path, int depth, int start)
        {
            path.AddRange(item.SelectedPath);
            item.GetSelects<PathSelectItem>().ToList().BuildSelectPaths(paths, path, depth + 1, start);
            return path;
        }

        return paths;
    }

    private static IEnumerable<ODataPathSegment> ToPathSegments(this IEnumerable<ODataPathSegment> paths, Type parentType)
    {
        foreach (var path in paths)
        {
            Debugger.Break();
        }
        return paths;
    }

#endif

                private static IDictionary<string, SelectItemPathSegments> ParseSelects(this IEnumerable<SelectItem> selectItems, Type rootType)
    {
        const char separator = '/';

        ConcurrentDictionary<string, SelectItemPathSegments> selectPaths = new();
        
        foreach (var pathSegments in selectItems)
        {
            Type parentType = rootType;
            StringBuilder pathBuilder = new();
            SelectItemPathSegments segments = new(pathSegments);

            foreach (var pathSegment in pathSegments.GetPathSegments())
            {
                Type memberType = parentType.GetMemberInfo(pathSegment.Identifier).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                if (elementType.IsLiteralType())
                {
                    if (segments.Any())
                        segments.Last().Selects.Add(pathSegment.Identifier);
            
                    continue;
                }

                PS? previousSegment = segments.LastOrDefault();
                if (previousSegment is not null)
                    previousSegment.PropertiesToIgnore.Add(pathSegment.Identifier);

                pathBuilder.AppendFormat("{0}{1}", separator, pathSegment.Identifier);
                segments.Add(new(pathSegment, GetSelects(), GetFilter(memberType), GetQuery()));
                                
                parentType = elementType;

                List<string>? GetSelects()
                {
                    if (TryGetExpandedNavigationSelectItem(out var navigation))
                    {
                        return navigation.SelectAndExpand.SelectedItems
                             .OfType<PathSelectItem>()
                             .Select(p => p.SelectedPath.First().Identifier)                             
                             .ToList();
                    }
                    return null;
                }

                FilterOptions? GetFilter(Type memberType)
                {
                    if (TryGetExpandedNavigationSelectItem(out var navigation)
                        && memberType.IsList() && navigation.FilterOption is not null)
                    {
                        return new FilterOptions(navigation.FilterOption);
                    }
                    return null;
                }

                QueryOptions? GetQuery()
                {
                    if (TryGetExpandedNavigationSelectItem(out var navigation)
                        && (navigation.OrderByOption is not null || navigation.SkipOption.HasValue || navigation.TopOption.HasValue))
                    {
                        return new QueryOptions(navigation.OrderByOption!, (int?)navigation.SkipOption, (int?)navigation.TopOption);
                    }
                    return null;
                }

                bool TryGetExpandedNavigationSelectItem([MaybeNullWhen(false)] out ExpandedNavigationSelectItem item)
                {
                    if (pathSegment is NavigationPropertySegment
                        && pathSegments is ExpandedNavigationSelectItem navigation)
                    {
                        item = navigation;
                        return true;
                    }
                    item = null;
                    return false;
                }
            }

            if (segments.Any())
            {
                selectPaths.AddOrUpdate(pathBuilder.ToString(), segments, (_, previous) =>
                {
                    var lastSegment = segments.Last();

                    if (lastSegment.Selects.Any())
                        previous.Last().Selects.AddRange(lastSegment.Selects);

                    return previous;
                });
            }                        
        }
        return selectPaths;
    }

    private static IEnumerable<ODataPathSegment> GetPathSegments(this SelectItem selectItem) =>
        selectItem switch
        {
            PathSelectItem item => item.SelectedPath,
            ExpandedNavigationSelectItem item => item.PathToNavigationProperty,
            _ => throw new NotSupportedException()
        };

    private static IImmutableSet<SelectPathSegments> ToExpansionSegments(this IEnumerable<SelectItem> selectItems)
    {       
        var selectPathGroups = selectItems
            .OfType<PathSelectItem>()
            .Select(s => (Path: s, Segments: new SelectPathSegments(s)))
            .GroupBy(s => s.Segments.RawPath);

        var selectPaths = new List<SelectPathSegments>();

        foreach (var group in selectPathGroups)
        {
            if (group.Count() == 1)
            {
                selectPaths.Add(group.First().Segments);
                continue;
            }

            var selectItem = group.First().Path;

            var pathSegments = group.First().Segments.ToList();
            pathSegments[^1] = new
            (
                selectItem, 
                pathSegments.Last().PathSegment, 
                GetSelects()
            );

            selectPaths.Add(new SelectPathSegments(selectItem, pathSegments));

            List<string> GetSelects() => group
                .SelectMany(s => s.Segments.AsEnumerable())
                .SelectMany(s => s.GetSelects())
                .ToList();
        }

        var expansions = selectItems
            .OfType<ExpandedNavigationSelectItem>()
            .Select(s => new SelectPathSegments(s));

        selectPaths.AddRange
        (
            selectItems.OfType<ExpandedNavigationSelectItem>()
                .Select(s => new SelectPathSegments(s))
        );

        return selectPaths.ToImmutableHashSet();
    }

    private static bool HasSelects<TPathType>(this ExpandedNavigationSelectItem item) where TPathType : SelectItem =>
        item.SelectAndExpand?.SelectedItems.OfType<TPathType>().Any() == true;

    private static IEnumerable<TPathType> GetSelects<TPathType>(this ExpandedNavigationSelectItem item) where TPathType : SelectItem =>
        item.SelectAndExpand?.SelectedItems.OfType<TPathType>() ?? Enumerable.Empty<TPathType>();

    private static IEnumerable<TPathType> GetSelects<TPathType>(this PathSelectItem item) where TPathType : SelectItem =>
        item.SelectAndExpand?.SelectedItems.OfType<TPathType>() ?? Enumerable.Empty<TPathType>();

    private static IEnumerable<TPathType> GetSelects<TPathType>(this ODataQueryOptions options) where TPathType : SelectItem =>
        options.SelectExpand?.SelectExpandClause?.SelectedItems.OfType<TPathType>() ?? Enumerable.Empty<TPathType>();


    //private static IEnumerable<SelectItem> GetSelectedItems<TModel>(this ODataQueryOptions<TModel> options)
    //{
    //    var selects = options.SelectExpand?.SelectExpandClause?.SelectedItems ?? Enumerable.Empty<SelectItem>();
    //    return selects.OrderByDescending(s => s is ExpandedNavigationSelectItem);
    //}
}
