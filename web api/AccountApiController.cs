using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using vDieu.Common;
using vDieu.Dto;
using vDieu.Service;
using vDieu.Web.Models.Account;
using vDieu.Web.Models.APIs;
using vDieu.Web.Models.APIs.Account;
using vDieu.Web.resource.Account;
using vDieu.Web.Utils;
using vDieu.Web.Utils.CustomMethods;

namespace vDieu.Web.Controllers.APIs
{
    public class AccountApiController : ApiController
    {
        private readonly IUserService _userService;
        private readonly IAccountService _accountService;
		
        public AccountApiController(IUserService us, IAccountService accountService)
        {
            _userService = us;
            _accountService = accountService;
        }
        
        [HttpGet]
        public HttpResponseMessage Login(string userName, string password)
        {
            var result = new HttpResult { IsSuccess = AccRes.Yes };
            if (_accountService.Authenticate(userName, password))
            {
                System.Web.Security.FormsAuthentication.SetAuthCookie(userName, false);
            }
            else
            {
                result.IsSuccess = AccRes.No;
                result.Message = AccRes.MsgLogin;
            }
            return Request.CreateResponse(HttpStatusCode.OK, result);
        }
		
        [HttpPost]
        public async Task<HttpResponseMessage> Register([FromUri]string userName)
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return CheckDataError(AccRes.MsgSubmitDataWayError);
            }
            try
            {
                if (String.IsNullOrEmpty(userName))
                    return CheckDataError(vConsts.MsgUserNameCannotBeNull);

                var result = new HttpResult { IsSuccess = vConsts.IsSuccessYes };

                if (this._userService.UserExists(userName))
                    return CheckDataError(AccRes.MsgUserExists);

                var perUserDto = new PerUserDto();

                var filePath = "~/App_Data/Uploads/" + userName + "/PersonalInfo";
                if (!System.IO.Directory.Exists(HttpContext.Current.Server.MapPath(filePath)))
                    System.IO.Directory.CreateDirectory(HttpContext.Current.Server.MapPath(filePath));

                var root = HttpContext.Current.Server.MapPath(filePath);
                //var provider = new CustomMultipartFormDataStreamProvider(root);
                var provider = new MultipartFormDataStreamProvider(root);

                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                var _userName = provider.FormData.GetValues("UserName");
                if (_userName != null)
                    perUserDto.UserName = MultipartFormHelper.RemoveNewline(_userName.First());
                else return CheckDataError(AccRes.MsgDataNoComplete);
                
                var password = provider.FormData.GetValues("Password");
                if (password != null)
                    perUserDto.Password = MultipartFormHelper.RemoveNewline(password.First());
                else return CheckDataError(AccRes.MsgDataNoComplete);
                
                var mobile = provider.FormData.GetValues("Mobile");
                if (mobile != null) perUserDto.Mobile = MultipartFormHelper.RemoveNewline(mobile.First());
                else return CheckDataError(AccRes.MsgDataNoComplete);
                
                var email = provider.FormData.GetValues("Email");
                if (email != null) perUserDto.Email = MultipartFormHelper.RemoveNewline(email.First());
                else return CheckDataError(AccRes.MsgDataNoComplete);
               
                var address = provider.FormData.GetValues("Address");
                if (address != null)
                    perUserDto.Address = MultipartFormHelper.RemoveNewline(address.First());
                else return CheckDataError(AccRes.MsgDataNoComplete);

                if (provider.FileData.Count != 2)
                {
                    result.IsSuccess = Consts.No;
                    result.Message = Consts.MsgPicError;
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                
                perUserDto = _userService.CreatePerUser(perUserDto);
                if (perUserDto.UserId == 0)
                {
                    result.IsSuccess = Consts.No;
                    result.Message = Consts.MsgRegisterFailed;
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                
                _userService.AddUsersToRoles(new[] { perUserDto.UserName }, new[] { Consts.UserRoleRegisteredUser });

                var userFiles = provider.FileData.Select(file => new UserFileDto
                {
                    UserFileTypeId = 1,
                    Name = file.Headers.ContentDisposition.Name.Replace("\"", string.Empty),
                    Ext = file.Headers.ContentDisposition.DispositionType,
                    Uri = file.LocalFileName,
                    UserId = perUserDto.UserId
                }).ToList();

                _userService.InsertUserFiles(userFiles);
				
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [HttpGet]
        public HttpResponseMessage DeepFetchUser(string userName)
        {
            try
            {
                var httpResponse = new HttpRespDeepFetchPerUser
                {
                    IsSuccess = Consts.No,
                    Message = Consts.MsgUserNotExist
                };

                if (this._userService.UserExists(userName))
                {
                    string incld = Consts.ModelName1 + "," +
                                   Consts.ModelName2 + "," +
                                   Consts.ModelName3;

                    var user = _userService.GetPerUser(userName, incld);
                    var userVm = AutoMapper.Mapper.Map<ApiUserVm>(user);

                    httpResponse = new HttpRespDeepFetchPerUser
                    {
                        IsSuccess = Consts.Yes,
                        ApiUserVm = userVm,
                    };
                }

                return Request.CreateResponse(HttpStatusCode.OK, httpResponse);
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetPerUserFiles(string userName)
        {
            try
            {
                var isSuccessField = new StringContent(Consts.No.ToString(CultureInfo.InvariantCulture));
                isSuccessField.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                isSuccessField.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "IsSuccess",
                };

                var msgField = new StringContent("No File Found.");
                msgField.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                msgField.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "Message",
                };

                var content = new MultipartFormDataContent();

                var files = _userService.GetUserFiles(userName);

                if (files != null && files.Any())
                {
                    isSuccessField = new StringContent(Consts.Yes.ToString(CultureInfo.InvariantCulture));
                    isSuccessField.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                    isSuccessField.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "IsSuccess",
                    };

                    msgField = new StringContent("");
                    msgField.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                    msgField.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "Message",
                    };

                    foreach (var file in files)
                    {
                        var fileContent = new StreamContent(new FileStream(file.Uri, FileMode.Open));
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                        {
                            Name = "file",
                            FileName = file.Name.ToString(),
                        };
                        content.Add(fileContent);
                    }
                }

                content.Add(isSuccessField);
                content.Add(msgField);

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = content;
                return response;
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [HttpGet]
        public HttpResponseMessage UpdatePassword(string userName, string oldPassword, string newPassword)
        {
            if (!_accountService.Authenticate(userName, oldPassword))
                return CheckDataError(Consts.MsgOldPasswordErroe);

            if (!_accountService.ResetPassword(userName, newPassword))
                return CheckDataError(Consts.MsgUpdateNewPssswordError);

            return Request.CreateResponse(HttpStatusCode.OK,
                new HttpResult { IsSuccess = Consts.Yes });
        }
       
        private HttpResponseMessage CheckDataError(string message)
        {
            return Request.CreateResponse(HttpStatusCode.OK,
                new HttpResult { IsSuccess = Consts.No, Message = message });
        }
    }
}
