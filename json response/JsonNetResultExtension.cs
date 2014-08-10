using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vDieu.Web.Models;

namespace vDieu.Web
{
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
}
