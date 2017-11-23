// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;

namespace UnityEditor.RestService
{
    [UnityEngine.Scripting.RequiredByNativeCode]
    internal abstract class Handler
    {
        // The following methods are invoked from native code.
        protected abstract void InvokeGet(Request request, string payload, Response writeResponse);
        protected abstract void InvokePost(Request request, string payload, Response writeResponse);
        protected abstract void InvokeDelete(Request request, string payload, Response writeResponse);
    }

    internal abstract class JSONHandler : Handler
    {
        protected override void InvokeGet(Request request, string payload, Response writeResponse)
        {
            CallSafely(request, payload, writeResponse, HandleGet);
        }

        protected override void InvokePost(Request request, string payload, Response writeResponse)
        {
            CallSafely(request, payload, writeResponse, HandlePost);
        }

        protected override void InvokeDelete(Request request, string payload, Response writeResponse)
        {
            CallSafely(request, payload, writeResponse, HandleDelete);
        }

        private static void CallSafely(Request request, string payload, Response writeResponse, Func<Request, JSONValue, JSONValue> method)
        {
            try
            {
                JSONValue json = null;

                if (payload.Trim().Length == 0)
                    json = new JSONValue();
                else
                {
                    try
                    {
                        json = new JSONParser(request.Payload).Parse();
                    }
                    catch (JSONParseException)
                    {
                        ThrowInvalidJSONException();
                    }
                }

                writeResponse.SimpleResponse(HttpStatusCode.Ok, "application/json", method(request, json).ToString());
            }
            catch (JSONTypeException)
            {
                ThrowInvalidJSONException();
            }
            catch (KeyNotFoundException)
            {
                RespondWithException(writeResponse, new RestRequestException { HttpStatusCode = HttpStatusCode.BadRequest });
            }
            catch (RestRequestException rre)
            {
                RespondWithException(writeResponse, rre);
            }
            catch (Exception e)
            {
                RespondWithException(writeResponse, new RestRequestException {HttpStatusCode = HttpStatusCode.InternalServerError, RestErrorString = "InternalServerError", RestErrorDescription = "Caught exception while fulfilling request: " + e});
            }
        }

        private static void ThrowInvalidJSONException()
        {
            throw new RestRequestException {HttpStatusCode = HttpStatusCode.BadRequest, RestErrorString = "Invalid JSON"};
        }

        private static void RespondWithException(Response writeResponse, RestRequestException rre)
        {
            var body = new StringBuilder("{");
            if (rre.RestErrorString != null)
                body.AppendFormat("\"error\":\"{0}\",", rre.RestErrorString);
            if (rre.RestErrorDescription != null)
                body.AppendFormat("\"errordescription\":\"{0}\"", rre.RestErrorDescription);
            body.Append("}");
            writeResponse.SimpleResponse(rre.HttpStatusCode, "application/json", body.ToString());
        }

        virtual protected JSONValue HandleGet(Request request, JSONValue payload)
        {
            throw new RestRequestException {HttpStatusCode = HttpStatusCode.MethodNotAllowed, RestErrorString = "MethodNotAllowed", RestErrorDescription = "This endpoint does not support the GET verb."};
        }

        virtual protected JSONValue HandlePost(Request request, JSONValue payload)
        {
            throw new RestRequestException { HttpStatusCode = HttpStatusCode.MethodNotAllowed, RestErrorString = "MethodNotAllowed", RestErrorDescription = "This endpoint does not support the POST verb."};
        }

        virtual protected JSONValue HandleDelete(Request request, JSONValue payload)
        {
            throw new RestRequestException { HttpStatusCode = HttpStatusCode.MethodNotAllowed, RestErrorString = "MethodNotAllowed", RestErrorDescription = "This endpoint does not support the DELETE verb."};
        }

        protected static JSONValue ToJSON(IEnumerable<string> strings)
        {
            return new JSONValue(strings.Select(s => new JSONValue(s)).ToList());
        }
    }
}
