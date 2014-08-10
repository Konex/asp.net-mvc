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















