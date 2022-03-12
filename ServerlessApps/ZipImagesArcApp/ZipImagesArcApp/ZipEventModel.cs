using System;
using Newtonsoft.Json;

namespace ZipImagesArcApp
{
    public class ZipEventModel
    {

        [JsonProperty("specversion")]
        public string SpecVersion { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("dataSchema")]
        public string DataSchema { get; set; }

        [JsonProperty("data")]
        public ZipModel ZipModel { get; set; }

    }

    public class ZipModel
    {

        [JsonProperty("zip")]
        public string Zip { get; set; }

    }
}
