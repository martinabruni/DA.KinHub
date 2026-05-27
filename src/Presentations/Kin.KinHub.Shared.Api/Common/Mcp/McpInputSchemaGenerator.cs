using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

internal static class McpInputSchemaGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly NullabilityInfoContext NullabilityInfoContext = new();

    public static JsonElement Generate(Type type)
    {
        var schema = BuildSchema(type, new HashSet<Type>());
        return JsonSerializer.SerializeToElement(schema, SerializerOptions);
    }

    private static object BuildSchema(Type type, HashSet<Type> stack)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType == typeof(string))
            return new Dictionary<string, object?> { ["type"] = "string" };
        if (actualType == typeof(Guid))
            return new Dictionary<string, object?> { ["type"] = "string", ["format"] = "uuid" };
        if (actualType == typeof(bool))
            return new Dictionary<string, object?> { ["type"] = "boolean" };
        if (actualType == typeof(TimeSpan))
            return new Dictionary<string, object?> { ["type"] = "string", ["format"] = "duration" };
        if (actualType == typeof(byte)
            || actualType == typeof(short)
            || actualType == typeof(int)
            || actualType == typeof(long))
            return new Dictionary<string, object?> { ["type"] = "integer" };
        if (actualType == typeof(float)
            || actualType == typeof(double)
            || actualType == typeof(decimal))
            return new Dictionary<string, object?> { ["type"] = "number" };
        if (actualType.IsEnum)
            return new Dictionary<string, object?> { ["type"] = "string", ["enum"] = Enum.GetNames(actualType) };

        if (TryGetEnumerableType(actualType, out var itemType))
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "array",
                ["items"] = BuildSchema(itemType!, stack),
            };
        }

        if (!stack.Add(actualType))
            return new Dictionary<string, object?> { ["type"] = "object" };

        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var required = new List<string>();

        foreach (var property in actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
                continue;

            properties[property.Name] = BuildSchema(property.PropertyType, stack);

            if (IsRequired(property))
            {
                required.Add(property.Name);
            }
        }

        stack.Remove(actualType);

        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
        };
    }

    private static bool IsRequired(PropertyInfo property)
    {
        if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null)
            return true;

        if (property.CustomAttributes.Any(attribute =>
            attribute.AttributeType == typeof(RequiredMemberAttribute)))
            return true;

        return NullabilityInfoContext.Create(property).WriteState is NullabilityState.NotNull;
    }

    private static bool TryGetEnumerableType(Type type, out Type? itemType)
    {
        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return true;
        }

        if (type == typeof(string))
        {
            itemType = null;
            return false;
        }

        itemType = type
            .GetInterfaces()
            .Append(type)
            .Where(static candidate => candidate.IsGenericType)
            .FirstOrDefault(static candidate => candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];

        return itemType is not null && typeof(IEnumerable).IsAssignableFrom(type);
    }
}
