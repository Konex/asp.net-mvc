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
            return new JsonNetResult() { Data = new JsonResultModel<object>(isSuccessful, message) { Data = data } };
        }
    }

The first extension method is to convert model state errors into json. Before we dive into the code, let's take a look at how model state looks like.

![ModelState Overview](https://github.com/Konex/asp.net-mvc/json%20response/images/modelStateOverview.png)

![ModelState Expended View](https://github.com/Konex/asp.net-mvc/json%20response/images/modelStateExpendedView.png)











