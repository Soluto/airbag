using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Airbag.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestEase;

namespace Airbag.OpenPolicyAgent
{
    public class OpenPolicyAgentInput
    {
        [JsonProperty("path")]
        public string[] Path { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonConverter(typeof(KeyValuePairConverter))]
        [JsonProperty("query")]
        public IEnumerable<KeyValuePair<string, string[]>> Query { get; set; }

        [JsonConverter(typeof(KeyValuePairConverter))]
        [JsonProperty("claims")]
        public IEnumerable<KeyValuePair<string,string[]>> Claims { get; set; }
    }

    public class OpenPolicyAgentQueryRequest
    {
        [JsonProperty("input")]
        public OpenPolicyAgentInput Input { get; set; }
    }

    public class OpenPolicyAgentQueryResponse
    {
        [JsonProperty("result")]
        public bool? Result { get; set; }

        [JsonProperty("decision_id")]
        public string DecisionId { get; set; }
    }

    public interface IOpenPolicyAgent
    {
        [Post("v1/data/{query}")]
        Task<OpenPolicyAgentQueryResponse> Query([Path(UrlEncode = false)] string query, [Body]OpenPolicyAgentQueryRequest request);
    }
}