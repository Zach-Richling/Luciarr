using System.Text.Json;

namespace Luciarr.WebApi.Clients
{
    public abstract class ClientBase
    {
        protected static readonly JsonSerializerOptions JsonSettings = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        protected string QueryString(IEnumerable<KeyValuePair<string, object>> queryParams)
        {
            return "?" + string.Join("&", queryParams.Select(x => $"{x.Key}={x.Value}"));
        }

        protected string SanitizeUri(string uri)
        {
            if (uri == null) 
                return "";

            return uri.EndsWith('/') ? uri : uri + "/";
        }
    }
}
