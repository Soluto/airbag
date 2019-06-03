using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Airbag.Utils
{
    public class KeyValuePairConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IEnumerable<KeyValuePair<string, string[]>>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var pairs = value as IEnumerable<KeyValuePair<string, string[]>>;

            if (pairs == null)
            {
                throw new InvalidOperationException("type not supported");
            }

            var jObject = new JObject(pairs.Select(pair => new JProperty(pair.Key, pair.Value)));

            serializer.Serialize(writer, jObject);
        }
    }
}
