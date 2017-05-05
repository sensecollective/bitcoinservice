using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DocumentDbRepositories.DocumentDb
{
    public class DocumentEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
