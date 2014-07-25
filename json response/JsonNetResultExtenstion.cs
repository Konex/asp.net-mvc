using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vDieu.Web.Models;

namespace vDieu.Web
{
    public static class JsonNetResultExtenstion
    {

        public static ActionResult JsonNet(this IController controller, ModelStateDictionary state)
        {
            var dct = state.Where(o => o.Value.Errors.Count > 0)
                .ToDictionary(o => o.Key, o => o.Value.Errors.Select(p => p.ErrorMessage).Join(","));
            return controller.JsonNet(false, "", dct);
        }

        public static ActionResult JsonNet(this IController controller, bool success, string message = "", object data = null)
        {
            return new JsonNetResult() { Data = new JsonResultModel<object>(success, message) { Data = data } };
        }
    }
}