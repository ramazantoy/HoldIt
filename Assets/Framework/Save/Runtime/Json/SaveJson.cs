using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace AF.Save
{
    /// <summary>
    /// The save system's own JSON settings. The global <c>JsonConvert.DefaultSettings</c> is never set: it would change every
    /// other Newtonsoft user in the process, Unity's tools included.
    /// </summary>
    public static class SaveJson
    {
        // Used on the main thread only (capture and restore). Parsing and writing documents do not need a serializer.
        private static readonly JsonSerializer _serializer = JsonSerializer.Create(CreateSettings());

        public static JsonSerializerSettings CreateSettings() => new JsonSerializerSettings
        {
            // No type names in the data: renaming a class must not break saves, and $type is an injection risk.
            TypeNameHandling = TypeNameHandling.None,
            // Default Auto reuses collections created by field initializers, so a list initialized with two items would load
            // those two plus the saved ones.
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            // Strings that look like dates stay strings; Utc keeps DateTime values culture- and time-zone-free.
            DateParseHandling = DateParseHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            FloatParseHandling = FloatParseHandling.Double,
            Culture = CultureInfo.InvariantCulture,
            // A field removed from a data class is ignored on load; migrations clean such data up when needed.
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            Converters =
            {
                new StringEnumConverter(),
                new Vector2Converter(),
                new Vector3Converter(),
                new Vector2IntConverter(),
                new Vector3IntConverter(),
                new QuaternionConverter(),
                new ColorConverter(),
            },
        };

        public static JToken ToToken(object data) => JToken.FromObject(data, _serializer);

        public static object FromToken(JToken token, Type type) => token.ToObject(type, _serializer);
    }
}
