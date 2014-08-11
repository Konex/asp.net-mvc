Json.net for asp.net mvc and web api
===========
Mobile devices are getting exceedingly ubiquitous these days so quite often you find yourself building asp.net mvc apps that need to support different clients such as Android, ios, Windows phone, and web. By default, asp.net mvc uses JsonResult class in System.Web.Mvc while asp.net web api uses Json.net. So it would be good if we have something that is consistent across the board when we are returning json responses for all these clients whether there be mobile clients or web browsers. 

Obviously Json.net is much better compared to JsonResult in several aspects so we are going to use Json.net for both mvc and web api. 

Now, let's take a look at the **JsonNetResultExtension.cs**

	public static class JsonNetResultExtension
    {
        public static ActionResult JsonNet(this IController controller, ModelStateDictionary state)
        {
            var dct = state.Where(s => s.Value.Errors.Count > 0)
                .ToDictionary(s => s.Key, s => s.Value.Errors.Select(e => e.ErrorMessage).Join(","));
            return controller.JsonNet(false, "", dct);
        }

        public static ActionResult JsonNet(this IController controller, bool isSuccessful, string message = "", object data = null)
        {
            return new JsonNetResult() { Data = new JsonResultViewModel<object>(isSuccessful, message) { Data = data } };
        }
    }

The first extension method is to convert model state errors into json. Before we explain the code, let's take a look at how model state looks like.

![ModelState Overview](https://github.com/Konex/asp.net-mvc/blob/master/json%20response/images/modelStateOverview.PNG)

![ModelState Expended View](https://github.com/Konex/asp.net-mvc/blob/master/json%20response/images/modelStateExpendedView.PNG)

The "Keys" in ModelState are the names of property in your model. In the "Values" note, it contains various values for each property. As you can see there is an "Errors" attribute under "Values" which specifies if there are any errors corresponding to each model property. Our goal is to extract out those model properties that have errors and stitch the errors into a string split by a comma then this error string into a dictionary by the model property name.

	// Select only those model properties that have errors.
	state.Where(s => s.Value.Errors.Count > 0)
	
For each one of those model properties that have errors, we get the errors out as Enumerable<String> then convert it into a delimited string by comma.
	
	s => s.Value.Errors.Select(e => e.ErrorMessage).Join(",")
	
The extension method Join is specified as below and the whole reason to use this method is to be able to chain the operation together.	

	public static string Join(this IEnumerable<string> src, string separator = "")
	{
		if (src == null || !src.Any()) return string.Empty;
		
		return string.Join(separator, src);
	}

After we get the errors out for those model properties that giving out errors. We then use ToDictionary in Linq to turn it into a Dictionary<string, string> object. And then we pass that dictionary object to another controller extension method to finally convert the model state errors to json.

	public static ActionResult JsonNet(this IController controller, bool isSuccessful, string message = "", object data = null)
	{
		return new JsonNetResult() { Data = new JsonResultViewModel<object>(isSuccessful, message) { Data = data } };
	}

In above code we new a JsonResultViewModel which contains information such as whether the action is successful, message, and json payload which could be a json serialized data entity that you send back to the client or model state error dictionary.

**JsonResultViewModel** is the view model that contains our action result. The "data" property is where we store the json payload or model state errors.

	public class JsonResultViewModel<T>
    {
        public JsonResultViewModel()
        {
        }

        public JsonResultViewModel(bool isSuccessful, T data)
        {
            Data = data;
        }
        public JsonResultViewModel(bool isSuccessful, string msg = "")
        {
            Success = isSuccessful;
            Message = msg;
        }

        [JsonProperty("isSuccessful")]
        public bool IsSuccessful { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

In **JsonNetResult**, 

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
            if (Data != null) 
			{
				if (IsoDateTimeConverter != null && Formatting != null) 
                    response.Write(Data.ToJsonNet(SerializerSettings, Formatting, IsoDateTimeConverter));
                else 
                    response.Write(Data.ToJsonNet());
			}
        }
    }
	
**ToJsonNet** extension method. 
	
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
			// Instructs jsonnet whether to reference duplicate entity by id.
			PreserveReferencesHandling = PreserveReferencesHandling.None
		};
		
	    // TODO: set the date format in your culture. Here is set for Australia date.
		var timeConverter = new IsoDateTimeConverter {DateTimeFormat = "dd-MM-yyyy HH:mm:ss"};
		settings.Converters.Add(timeConverter);

		return = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
	}

To catch model state errors in your controller, simply do:

	public ActionResult YiniAction(yiniViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return this.JsonNet(ModelState);
		}
	}












