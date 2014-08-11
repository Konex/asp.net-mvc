using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using vDieu.Web.Models;

namespace vDieu.Web
{
    public static class ApiJsonNetResultExtensions
    {
        public static JsonResultModel<T> JsonNet<T>(this ApiController controller,
                bool isSuccessful, string message = "", T data = default(T))
        {
            var result = new JsonResultModel<T>(true) { Message = message, Data = data };
            return result;
        }

        public static JsonResultModel<object> JsonNet(this ApiController controller,
              bool isSuccessful, string message = "")
        {
            return controller.JsonNet<Object>(isSuccessful, message, null);
        }
    }
}