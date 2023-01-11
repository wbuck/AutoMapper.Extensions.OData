using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutoMapper.OData.Cosmos.Tests.Entities;

[JsonConverter(typeof(StringEnumConverter))]
public enum DcStatus
{
    Healthy,
    NotHealthy
}
