using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vDieu.Web.Models;

namespace vDieu.Web
{
    public static class JsonResultExtension
    {

        public static ActionResult JsonResult(this IController controller, ModelStateDictionary state)
        {
            var dct = state.Where(s => s.Value.Errors.Count > 0)
                .ToDictionary(s => s.Key, s => s.Value.Errors.Select(p => p.ErrorMessage).Join(","));
            return controller.JsonResult(false, "", dct);
        }

        public static ActionResult JsonResult(this IController controller, bool success, string message = "", object data = null)
        {
            return new JsonActionResult() { Data = new JsonResultModel<object>(success, message) { Data = data } };
        }
    }
}