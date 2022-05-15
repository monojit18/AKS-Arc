using System;
using Newtonsoft.Json;

namespace SqlConnectArcApp
{
    public class Returns
    {

        [JsonProperty("id")]
        public string ProductID { get; set; }

        [JsonProperty("name")]
        public string ProductName { get; set; } 

        [JsonProperty("quantity")]
        public float Quantity { get; set; }      

        [JsonProperty("reason")]
        public string Reason { get; set; }

    }
}
