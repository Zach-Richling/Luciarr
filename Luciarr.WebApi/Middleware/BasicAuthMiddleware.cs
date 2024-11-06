using Microsoft.Extensions.Options;
using Luciarr.WebApi.Models;
using System.Net.Http.Headers;
using System.Text;

namespace Luciarr.WebApi.Middleware
{
    public class BasicAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public BasicAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IOptionsSnapshot<AppSettings> config)
        {
            try
            {
                var appSettings = config.Value;
                var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers.Authorization.ToString());

                if (authHeader?.Parameter == null)
                {
                    return;
                }

                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter)).Split(':', 2);

                var username = credentials[0];
                var password = credentials[1];

                if (username == appSettings.AuthUsername && password == appSettings.AuthPassword)
                {
                    context.Items["User"] = username;
                }
            }
            catch { }
            finally { await _next(context); }
        }
    }
}
