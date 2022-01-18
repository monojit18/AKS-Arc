using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ValidateOCRArcApp.Models
{
    public class OCRInfoModel
    {

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("textAngle")]
        public double TextAngle { get; set; }        


    }

}
