using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace AppStream.SmartParser;

internal interface IJsonSchemaGenerator
{
    BinaryData? GenerateSchema<TType>();
}

internal class JsonSchemaGenerator(IMemoryCache memoryCache) : IJsonSchemaGenerator
{
    private readonly IMemoryCache _memoryCache = memoryCache;

    public BinaryData? GenerateSchema<TType>()
    {
        var key = typeof(TType).GetHashCode();
        return this._memoryCache.GetOrCreate(key, (cacheEntry) =>
        {
            var generator = new JSchemaGenerator
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                SchemaIdGenerationHandling = SchemaIdGenerationHandling.TypeName,
                DefaultRequired = Required.Always
            };
            var schema = generator.Generate(typeof(TType));
            schema.AllowAdditionalProperties = false;
            var schemaJson = Encoding.UTF8.GetBytes(schema.ToString());
            return BinaryData.FromBytes(schemaJson);
        });
    }
}
