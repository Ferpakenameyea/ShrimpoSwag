using System;
using System.Collections.Generic;

namespace ShrimpoSwag;
internal static class HttpInvocationMethods
{
    public const string Ok = "Ok";
    public const string BadRequest = "BadRequest";
    public const string NotFound = "NotFound";
    public const string Created = "Created";
    public const string NoContent = "NoContent";
    public const string Conflict = "Conflict";

    private static readonly Dictionary<string, int> _httpMethodStatusCodes = new()
    {
        { Ok, 200 },
        { BadRequest, 400 },
        { NotFound, 404 },
        { Created, 201 },
        { NoContent, 204 },
        { Conflict, 409 }
    };

    public static bool IsHttpResponseMethod(string methodName)
    {
        return _httpMethodStatusCodes.ContainsKey(methodName);
    }

    public static int GetStatusCode(string methodName)
    {
        if (!_httpMethodStatusCodes.TryGetValue(methodName, out var statusCode))
        {
            throw new ArgumentException($"{methodName} is not a http response call");
        }

        return statusCode;
    }
}
