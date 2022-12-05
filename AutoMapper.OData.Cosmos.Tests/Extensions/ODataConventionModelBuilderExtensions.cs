using Microsoft.OData.ModelBuilder;

namespace AutoMapper.OData.Cosmos.Tests.Extensions;

internal static class ODataConventionModelBuilderExtensions
{
    public static ODataConventionModelBuilder EntitySet<TModel>(
        this ODataConventionModelBuilder builder,
        string name,
        Action<EntityTypeConfiguration<TModel>> configure) where TModel : class
    {
        configure(builder.EntitySet<TModel>(name).EntityType);
        return builder;
    }
}
