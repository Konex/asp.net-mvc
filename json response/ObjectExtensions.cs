using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace vDieu
{
    public static class ObjectExtensions
    {
        public static string ToJsonNet(this object obj)
        {
            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = 1,
                PreserveReferencesHandling = PreserveReferencesHandling.None
            };
            var timeConverter = new IsoDateTimeConverter {DateTimeFormat = "yyyy-MM-dd HH:mm:ss"};
            settings.Converters.Add(timeConverter);

            var data = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);

            return data;
        }
    }
}