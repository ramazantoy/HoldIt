using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AF.Save
{
    // Newtonsoft serializes Unity structs through their properties: Vector3.normalized is another Vector3, so the default
    // serializer fails with a self-referencing loop. These write only the components.

    internal sealed class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer) =>
            UnityJson.Write(writer, ("x", value.x), ("y", value.y));

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            UnityJson.Read(reader, out var o) ? new Vector2(o.Value<float>("x"), o.Value<float>("y")) : default;
    }

    internal sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer) =>
            UnityJson.Write(writer, ("x", value.x), ("y", value.y), ("z", value.z));

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            UnityJson.Read(reader, out var o) ? new Vector3(o.Value<float>("x"), o.Value<float>("y"), o.Value<float>("z")) : default;
    }

    internal sealed class Vector2IntConverter : JsonConverter<Vector2Int>
    {
        public override void WriteJson(JsonWriter writer, Vector2Int value, JsonSerializer serializer) =>
            UnityJson.Write(writer, ("x", value.x), ("y", value.y));

        public override Vector2Int ReadJson(JsonReader reader, Type objectType, Vector2Int existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            UnityJson.Read(reader, out var o) ? new Vector2Int(o.Value<int>("x"), o.Value<int>("y")) : default;
    }

    internal sealed class Vector3IntConverter : JsonConverter<Vector3Int>
    {
        public override void WriteJson(JsonWriter writer, Vector3Int value, JsonSerializer serializer) =>
            UnityJson.Write(writer, ("x", value.x), ("y", value.y), ("z", value.z));

        public override Vector3Int ReadJson(JsonReader reader, Type objectType, Vector3Int existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            UnityJson.Read(reader, out var o) ? new Vector3Int(o.Value<int>("x"), o.Value<int>("y"), o.Value<int>("z")) : default;
    }

    internal sealed class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer) =>
            UnityJson.Write(writer, ("x", value.x), ("y", value.y), ("z", value.z), ("w", value.w));

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            UnityJson.Read(reader, out var o)
                ? new Quaternion(o.Value<float>("x"), o.Value<float>("y"), o.Value<float>("z"), o.Value<float>("w"))
                : Quaternion.identity;
    }

    internal sealed class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer) =>
            UnityJson.Write(writer, ("r", value.r), ("g", value.g), ("b", value.b), ("a", value.a));

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            UnityJson.Read(reader, out var o) ? new Color(o.Value<float>("r"), o.Value<float>("g"), o.Value<float>("b"), o.Value<float>("a")) : default;
    }

    internal static class UnityJson
    {
        public static void Write(JsonWriter writer, params (string name, float value)[] components)
        {
            writer.WriteStartObject();
            foreach (var (name, value) in components)
            {
                writer.WritePropertyName(name);
                writer.WriteValue(value);
            }
            writer.WriteEndObject();
        }

        public static void Write(JsonWriter writer, params (string name, int value)[] components)
        {
            writer.WriteStartObject();
            foreach (var (name, value) in components)
            {
                writer.WritePropertyName(name);
                writer.WriteValue(value);
            }
            writer.WriteEndObject();
        }

        /// <summary>False for a JSON null; the caller returns the type's default.</summary>
        public static bool Read(JsonReader reader, out JObject value)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                value = null;
                return false;
            }
            value = JObject.Load(reader);
            return true;
        }
    }

    /// <summary>The Unity types the save serializer converts. Validation reports other UnityEngine structs in save data.</summary>
    internal static class UnityConverters
    {
        public static readonly Type[] SupportedTypes =
        {
            typeof(Vector2), typeof(Vector3), typeof(Vector2Int), typeof(Vector3Int), typeof(Quaternion), typeof(Color),
        };
    }
}
