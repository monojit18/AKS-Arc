using System;
using Newtonsoft.Json;

namespace SqlConnectArcApp
{
    public class Products
    {

        [JsonProperty("id")]
        public int ProductID { get; set; }

        [JsonProperty("name")]
        public string ProductName { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("desc")]
        public string ProductDescription { get; set; }

    }
}
