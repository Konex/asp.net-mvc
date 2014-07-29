using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace vDieu.Web 
{
    public class JsonNetResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public object Data { get; set; }

		public IsoDateTimeConverter IsoDateTimeConverter { get; set;}
        public JsonSerializerSettings SerializerSettings { get; set; }
        public Formatting Formatting { get; set; }

        public JsonNetResult()
        {
            SerializerSettings = new JsonSerializerSettings();
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var response = context.HttpContext.Response;

            response.ContentType = !string.IsNullOrEmpty(ContentType)
              ? ContentType
              : "application/json";

            response.CacheControl = "no-cache";

            if (ContentEncoding != null) response.ContentEncoding = ContentEncoding;

			// Here we call the extension method Object.ToJsonNet().
            if (Data != null) response.Write(Data.ToJsonNet());
        }
    }
}