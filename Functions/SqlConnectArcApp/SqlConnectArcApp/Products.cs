using System;
using Newtonsoft.Json;

namespace SqlConnectArcApp
{
    public class Products
    {

        [JsonProperty("id")]
        public string ProductID { get; set; }

        [JsonProperty("name")]
        public string ProductName { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("desc")]
        public string ProductDescription { get; set; }

        [JsonProperty("quantity")]
        public float Quantity { get; set; }

    }
}
