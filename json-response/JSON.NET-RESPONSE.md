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

![ModelState Overview](https://github.com/Konex/asp.net-mvc/blob/master/json-response/images/modelStateOverview.PNG)

![ModelState Expended View](https://github.com/Konex/asp.net-mvc/blob/master/json-response/images/modelStateExpendedView.PNG)

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

        public JsonResultViewModel(T data)
        {
            Data = data;
        }
        public JsonResultViewModel(bool isSuccessful, string msg = "")
        {
            IsSuccessful = isSuccessful;
            Message = msg;
        }

        [JsonProperty("isSuccessful")]
        public bool IsSuccessful { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

In **JsonNetResult**, it is standard procedure straight from Json.net [documentation](http://james.newtonking.com/archive/2008/10/16/asp-net-mvc-and-json-net).

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
	
The **ToJsonNet** extension method. 
	
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

To catch model state errors or return a json action result using the default jsonNet settings in your controller. 

There are a few things we need to take note here. First, we use a service instance rather than directly interacting with DbContext for a number of reasons such as clearer separation of concerns, EF being non-thread-safe means you better do your all EF stuff in one project thus to be able to apply to a more complex scenario where you deploy different projects(dlls) to different servers. Second, "_yiniService" is constructor-DIed into our controller. Third, we convert view model into Data Transfer Object by using Automapper's dynamic mapping capability. Then convert DTO into our domain object to be persisted by EF. Any one of these three aspects deserves its own discussion and we are not going to do that in this article.      

	public ActionResult YiniAction(yiniViewModel viewModel)
	{
		if (!ModelState.IsValid) return this.JsonNet(ModelState);
		
		if (viewModel.Id == 0) _yiniService.Add(viewModel.DynamicMap<YiniDto>());
		else _yiniService.Update(viewModel.DynamicMap<YiniDto>())
		
		return this.JsonNet(true);
	}

Now, let's move on to Web API. In the **ApiJsonNetResultExtensions**.

	public static class ApiJsonNetResultExtensions
    {
        public static JsonResultViewModel<T> JsonNet<T>(this ApiController controller,
                bool isSuccessful, string message = "", T data = default(T))
        {
            return new JsonResultViewModel<T>(true) { Message = message, Data = data };
        }

        public static JsonResultViewModel<object> JsonNet(this ApiController controller,
              bool isSuccessful, string message = "")
        {
            return controller.JsonNet<Object>(isSuccessful, message, null);
        }
    }













