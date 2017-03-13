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
    abstract internal class Handler
    {
        //invoked from native code.
        void InvokeGet(Request request, string payload, Response writeResponse)
        {
            CallSafely(request, payload, writeResponse, HandleGet);
        }

        //invoked from native code.
        void InvokePost(Request request, string payload, Response writeResponse)
        {
            CallSafely(request, payload, writeResponse, HandlePost);
        }

        //invoked from native code.
        void InvokeDelete(Request request, string payload, Response writeResponse)
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

                writeResponse.SimpleResponse(HttpStatusCode.Ok, method(request, json).ToString());
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
            writeResponse.SimpleResponse(rre.HttpStatusCode, body.ToString());
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
