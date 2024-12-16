using System.Text;

namespace AppStream.SmartParser;

internal interface IJsonSchemaGenerator
{
    BinaryData? GenerateSchema<TType>();
}

internal class JsonSchemaGenerator : IJsonSchemaGenerator
{
    public BinaryData? GenerateSchema<TType>()
    {
        var schema = NJsonSchema.JsonSchema.FromType<TType>();
        var schemaData = schema.ToJson();
        var schemaJson = Encoding.UTF8.GetBytes(schemaData);
        return BinaryData.FromBytes(schemaJson);
    }
}
