using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace vDieu.Web.Models
{
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
}