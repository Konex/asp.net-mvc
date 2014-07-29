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
			    // Json.NET will ignore objects in reference loops and not serialize them. 
				// The first time an object is encountered it will be serialized as usual 
				// but if the object is encountered as a child object of itself the serializer 
				// will skip serializing it.
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				// maximum depth allowed when reading JSON.
                MaxDepth = 1,
                PreserveReferencesHandling = PreserveReferencesHandling.None
            };
			
			// TODO: set the date format in your culture. Here is set for Australia date.
            var timeConverter = new IsoDateTimeConverter {DateTimeFormat = "dd-MM-yyyy HH:mm:ss"};
            settings.Converters.Add(timeConverter);

            return = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
        }
		
		 public static string ToJsonNet(this object obj, JsonSerializerSettings settings, 
            Formatting formatting, IsoDateTimeConverter dateTimeConverter)
        {
            settings.Converters.Add(dateTimeConverter);
            return JsonConvert.SerializeObject(obj, formatting, settings);
        }
    }
}