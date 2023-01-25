#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal static class SelectPathsBuilder
{
    public static List<List<PathSegment>> GetSelects<TModel>(this ODataQueryOptions<TModel> options)
    {
        Type parentType = typeof(TModel);
        IEdmModel edmModel = options.Context.Model;

        var selects = options.GetSelectPaths<PathSelectItem>();

        if (!selects.Any())
        {
            // If there are no selects or only selects for expanded entities,
            // we need to expand the complex types on the root entity.
            return parentType.GetValueAndComplexMemberSelects(edmModel);
        }        

        return selects.ToList().BuildSelectPaths(parentType, edmModel, new(), new());
    }

    public static List<List<PathSegment>> GetExpansions<TModel>(this ODataQueryOptions<TModel> options)
    {
        Type parentType = typeof(TModel);
        IEdmModel edmModel = options.Context.Model;
      
        return options.GetSelectPaths<ExpandedNavigationSelectItem>()
            .ToList().BuildExpansionPaths(parentType, edmModel, new(), new());
    }

    private static List<List<PathSegment>> BuildExpansionPaths(
        this IReadOnlyList<ExpandedNavigationSelectItem> selectedPaths,
        Type parentType,
        IEdmModel edmModel,
        List<List<PathSegment>> paths,
        List<PathSegment> currentPath,
        in int depth = 0)
    {
        if (!selectedPaths.Any())
            return paths;

        List<PathSegment> segments = depth == 0 
            ? currentPath
            : currentPath.ToNewList();

        BuildPathSegments(selectedPaths[0], segments, depth);

        if (depth == 0 || !currentPath.Equals(segments))
            paths.Add(segments);

        foreach (var selectItem in selectedPaths.Skip(1))        
            paths.Add(BuildPathSegments(selectItem, currentPath.ToNewList(), depth));        

        return paths;

        List<PathSegment> BuildPathSegments(ExpandedNavigationSelectItem pathSegments, List<PathSegment> path, in int depth)
        {
            Type rootType = parentType;
            foreach (var pathSegment in pathSegments.PathToNavigationProperty)
            {
                MemberInfo member = rootType.GetMemberInfo(pathSegment.Identifier);
                Type memberType = member.GetMemberType();
                Type elementType = memberType.GetCurrentType();

                path.Add(new
                (                    
                    member,
                    rootType,
                    memberType,
                    pathSegment.EdmType.TypeKind,
                    pathSegment.GetFilter(pathSegments),
                    pathSegment.GetQuery(pathSegments),
                    pathSegment.GetSelects(pathSegments, elementType, edmModel)
                ));

                rootType = elementType;
            }

            pathSegments.GetSelectPaths<ExpandedNavigationSelectItem>()
                .ToList().BuildExpansionPaths(rootType, edmModel, paths, path, depth + 1);

            return path;
        }       
    }

    private static List<List<PathSegment>> BuildSelectPaths(
        this IReadOnlyList<PathSelectItem> selectedPaths,        
        Type parentType,
        IEdmModel edmModel,
        List<List<PathSegment>> paths,
        List<PathSegment> currentPath,
        in int depth = 0)
    {
        for (int i = 0; i < selectedPaths.Count; ++i)
        {
            List<PathSegment> segments = i == 0 
                ? currentPath 
                : currentPath.Take(depth).ToNewList();

            segments = BuildPathSegments(selectedPaths[i], segments, depth);

            if (depth == 0 || !segments.Equals(currentPath))
            {
                PathSegment lastSegment = segments.Last();

                if (!lastSegment.IsComplex || lastSegment.FilterOptions is not null || lastSegment.QueryOptions is not null)
                    paths.Add(segments);

                if (lastSegment.IsComplex)
                    paths.AddLiteralAndComplexSelects(segments, edmModel);
            }
        }

        return paths;

        List<PathSegment> BuildPathSegments(PathSelectItem pathSegments, List<PathSegment> path, in int depth)
        {
            Type rootType = parentType;
            foreach (var pathSegment in pathSegments.SelectedPath)
            {
                MemberInfo member = rootType.GetMemberInfo(pathSegment.Identifier);
                Type memberType = member.GetMemberType();
                Type elementType = memberType.GetCurrentType();

                path.Add(new
                (
                    member,
                    rootType,
                    memberType,
                    pathSegment.EdmType.AsElementType().TypeKind,
                    pathSegment.GetFilter(pathSegments),
                    pathSegment.GetQuery(pathSegments)
                ));

                rootType = elementType;
            }            

            pathSegments.GetSelectPaths<PathSelectItem>().ToList()
                .BuildSelectPaths(rootType, edmModel, paths, path, depth + 1);

            return path;
        }
    }

    private static List<List<PathSegment>> AddLiteralAndComplexSelects(this List<List<PathSegment>> paths, List<PathSegment> pathSegments, IEdmModel edmModel)
    {
        PathSegment pathSegment = pathSegments.Last();
        Type memberType = pathSegment.ElementType;

        var memberSelects = memberType.GetValueOrListOfValueTypeMembers()
            .Select(m => AddPathSegment(m, EdmTypeKind.Primitive, pathSegments.ToNewList()));

        var complexPaths = edmModel.GetComplexTypeSelects(memberType).Select
        (
            paths =>
            {
                paths.InsertRange(0, pathSegments.ToNewList());
                return paths;
            }
        );

        paths.AddRange(memberSelects.Concat(complexPaths));
        return paths;

        static List<PathSegment> AddPathSegment(MemberInfo member, EdmTypeKind edmTypeKind, List<PathSegment> pathSegments)
        {
            pathSegments.Add(new
            (
                member,
                member.DeclaringType!,
                member.GetMemberType().GetCurrentType(),
                edmTypeKind
            ));
            return pathSegments;
        }
    }     

    private static QueryOptions? GetQuery(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments)
    {
        if (!pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier))
            return null;

        return GetQueryOptions
        (
            pathSegments.OrderByOption,
            pathSegments.SkipOption,
            pathSegments.TopOption
        );
    }

    private static QueryOptions? GetQuery(this ODataPathSegment pathSegment, PathSelectItem pathSegments)
    {
        if (!pathSegments.SelectedPath.Last().Identifier.Equals(pathSegment.Identifier))
            return null;

        return GetQueryOptions
        (
            pathSegments.OrderByOption, 
            pathSegments.SkipOption, 
            pathSegments.TopOption
        );
    }

    private static QueryOptions? GetQueryOptions(OrderByClause? clause, long? skip, long? top) 
        => clause is not null || skip is not null || top is not null 
        ? new(clause!, (int?)skip, (int?)top)
        : null;


    private static FilterOptions? GetFilter(this ODataPathSegment pathSegment, SelectItem pathSegments)         
    {
        var (identifer, filterOption) = pathSegments switch
        {
            ExpandedNavigationSelectItem item => GetNavigationFilter(item),
            PathSelectItem item => GetPathSelectFilter(item),
            _ => throw new NotSupportedException($"Unknown {nameof(SelectItem)} type.")
        };

        return identifer.Equals(pathSegment.Identifier) && filterOption is not null
            ? new(filterOption)
            : null;

        static (string, FilterClause?) GetNavigationFilter(ExpandedNavigationSelectItem item) =>
            (item.PathToNavigationProperty.Last().Identifier, item.FilterOption);

        static (string, FilterClause?) GetPathSelectFilter(PathSelectItem item) =>
            (item.SelectedPath.Last().Identifier, item.FilterOption);
    }     

    private static List<List<PathSegment>>? GetSelects(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments, Type parentType, IEdmModel edmModel)
    {
        if (pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier))
        {
            return pathSegments.GetSelectPaths<PathSelectItem>().ToList() switch
            {
                var selects when selects.Any() => selects.BuildSelectPaths(parentType, edmModel, new(), new()),
                _ => parentType.GetValueAndComplexMemberSelects(edmModel)
            };
        }
        return null;
    }

    private static IEnumerable<T> GetSelectPaths<T>(this SelectItem selectItem) where T: SelectItem
    {
        return selectItem switch
        {
            ExpandedNavigationSelectItem item => GetItems<T>(item.SelectAndExpand),
            PathSelectItem item => GetItems<T>(item.SelectAndExpand),
            _ => throw new NotSupportedException($"Unknown {nameof(SelectItem)} type.")
        };        
    }
    private static IEnumerable<T> GetSelectPaths<T>(this ODataQueryOptions options) where T : SelectItem =>
        GetItems<T>(options.SelectExpand?.SelectExpandClause);

    private static IEnumerable<T> GetItems<T>(SelectExpandClause? clause) =>
            clause?.SelectedItems.OfType<T>() ?? Enumerable.Empty<T>();

    private static List<PathSegment> ToNewList(this IEnumerable<PathSegment> pathSegments) =>
        new(pathSegments.Select
        (
            p => new PathSegment
            (
                p.Member,
                p.ParentType,
                p.MemberType,
                p.EdmTypeKind
            ))
        );
}
