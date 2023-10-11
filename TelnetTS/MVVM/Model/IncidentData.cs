using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TelnetTS.MVVM.Model
{
    public class IncidentData
    {
        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("dataStart")]
        public DateTime DataStart { get; set; } = DateTime.Now;

        [JsonProperty("dataEnd")]
        public DateTime? DataEnd { get; set; }

        [JsonProperty("zues")]
        public string Zues { get; set; } = "-";

        [JsonProperty("rues")]
        public string Rues { get; set; } = "-";

        [JsonProperty("uslugi_all")]
        public string UslugiAll { get; set; } = "-";

        [JsonProperty("IP")]
        public string IP { get; set; } = "-";

        [JsonProperty("address")]
        public string Address { get; set; } = "-";

        [JsonProperty("problem")]
        public string Problem { get; set; } = "-";

        public List<IncidentData> UpdateAsync()
        {
            var httpClient = new HttpClient();
            var res = httpClient.GetStringAsync("http://192.168.114.142:3000/api/get_incidents?isGou=false&fix=true&mob=false&type=open&zues=%D0%92%D1%81%D0%B5+%D0%97%D0%A3%D0%AD%D0%A1&rues=%D0%92%D1%81%D0%B5+%D0%A0%D0%A3%D0%AD%D0%A1").Result;
            var json = JObject.Parse(res)["incidents"].ToString();
            var incidents = JObject.Parse(json);
            var data = JArray.Parse(incidents["data"].ToString());

            return data.ToObject<List<IncidentData>>().OrderBy(x => x.ID).ToList();
        }
    }
}
