using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SurveyMonkey.Helpers;

namespace SurveyMonkey;

internal class TolerantJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        var type = GetUnderlyingType(objectType);
        return type.IsEnum || type.IsClass;
    }

    public override bool CanWrite => false;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var type = GetUnderlyingType(objectType);
        if (type.IsEnum)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var enumText = PropertyCasingHelper.SnakeToCamel(reader.Value.ToString());
                var names = Enum.GetNames(type);
                var match = names.FirstOrDefault(n => string.Equals(n, enumText, StringComparison.InvariantCultureIgnoreCase));
                if (match != null)
                {
                    return Enum.Parse(type, match);
                }
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                var enumVal = Convert.ToInt32(reader.Value);
                var values = (int[])Enum.GetValues(type);
                if (values.Contains(enumVal))
                {
                    return Enum.ToObject(type, enumVal);
                }
            }
            WarnOfMissingDeserializationOpportunity(reader.Value.ToString(), type.FullName);
            return null;
        }

        var instance = objectType.GetConstructor(Type.EmptyTypes).Invoke(null);
        var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        var jsonProperties = JObject.Load(reader).Properties();
        foreach (var jsonProperty in jsonProperties)
        {
            var name = PropertyCasingHelper.SnakeToCamel(jsonProperty.Name);
            //Find a property that either matches the (case-converted) name and which isn't ignored, or which matches a JsonProperty manually describing a name.
            var property = properties.FirstOrDefault(pi =>
                    (
                        string.Equals(pi.Name, name, StringComparison.OrdinalIgnoreCase)
                        && !Attribute.IsDefined(pi, typeof(JsonIgnoreAttribute))
                    )
                    ||
                        string.Equals(
                            ((JsonPropertyAttribute)pi.GetCustomAttribute(typeof(JsonPropertyAttribute)))?.PropertyName,
                            jsonProperty.Name,
                            StringComparison.OrdinalIgnoreCase)
                );

            if (property != null)
            {
                if (
                    jsonProperty.Value.Type != JTokenType.Null
                    && !IsUnparseableNumeric(property, jsonProperty))
                {
                    if (property.PropertyType == typeof(DateTime?))
                    {
                        //Want DateTimes to always be treated as UTC
                        var rawDate = (DateTime)jsonProperty.Value.ToObject(typeof(DateTime), serializer);
                        var convertedDate = new DateTime();
                        switch (rawDate.Kind)
                        {
                            case DateTimeKind.Local:
                                convertedDate = rawDate.ToUniversalTime();
                                break;
                            case DateTimeKind.Unspecified:
                                convertedDate = DateTime.SpecifyKind(rawDate, DateTimeKind.Utc);
                                break;
                            case DateTimeKind.Utc:
                                convertedDate = rawDate;
                                break;
                        }
                        property.SetValue(instance, convertedDate);
                    }
                    else
                    {
                        if (property.Name == "Choices" && jsonProperty.Value.Type != JTokenType.Array)
                        {
                            var legacyProperty = properties.FirstOrDefault(pi => string.Equals("LegacyChoices", pi.Name, StringComparison.OrdinalIgnoreCase));
                            legacyProperty.SetValue(instance, jsonProperty.Value.ToObject(legacyProperty.PropertyType, serializer));
                        }
                        else
                        {
                            property.SetValue(instance, jsonProperty.Value.ToObject(property.PropertyType, serializer));
                        }
                    }
                }
            }
            else
            {
                CheckPropsForMissingDeserializationOpportunity(type, properties, jsonProperty, name);
            }
        }
        return instance;
    }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private bool IsNullableType(Type t)
    {
        return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    private Type GetUnderlyingType(Type type)
    {
        return IsNullableType(type) ? Nullable.GetUnderlyingType(type) : type;
    }

    private bool IsUnparseableNumeric(PropertyInfo classProperty, JProperty jsonProperty)
    {
        //API very occasionally supplies strings for numeric values
        return (classProperty.PropertyType == typeof(int?) || classProperty.PropertyType == typeof(long?)) && !long.TryParse(jsonProperty.Value.ToString(), out _);
    }

    [Conditional("DEBUG")]
    private void CheckPropsForMissingDeserializationOpportunity(Type type, PropertyInfo[] properties, JProperty jsonProperty, string name)
    {
        var haveMatchingPropertyToIgnore = properties.Any(pi =>
            string.Equals(pi.Name, name, StringComparison.OrdinalIgnoreCase)
            && Attribute.IsDefined(pi, typeof(JsonIgnoreAttribute)));
        if (!haveMatchingPropertyToIgnore)
        {
            WarnOfMissingDeserializationOpportunity(jsonProperty.Name, type.Name);
        }
    }

    [Conditional("DEBUG")]
    private void WarnOfMissingDeserializationOpportunity(string propertyName, string type)
    {
        if(GetType() == typeof(TolerantJsonConverter))
        {
            if (!Debugger.IsAttached)
            {
                throw new ArgumentException($"Json property {propertyName} doesn't exist on object {type}");
            }
            //else
            //{
            //    Debug.Fail($"Json property {propertyName} doesn't exist on object {type}");
            //}
        }
    }
}