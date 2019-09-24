using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace TilosAzureMvc.Models {
    public class AcrCallback: TableEntity {
        [JsonProperty(PropertyName = "stream_id")]
        public string StreamId { get; set; }

        [JsonProperty(PropertyName = "stream_url")]
        public string StreamUrl { get; set; }

        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }

        public AcrCallback(string streamId, string ts ) {
            this.PartitionKey = streamId;
            this.RowKey = ts;
            this.StreamId = streamId;
        }
        public AcrCallback() { }

    }
}