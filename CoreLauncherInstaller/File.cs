using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreLauncherInstaller
{
    internal class File
    {
        [JsonProperty("version")]
        public double Version { get; set; }
        [JsonProperty("url")]
        public string URL { get; set; }
    }
}
