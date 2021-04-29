using System;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Resurfaceio
{
    class HttpMessage
    {
        public static void send(
            HttpLogger logger,
            HttpRequest request,
            HttpResponse response,
            string response_body = null,
            string request_body = null,
            long now = 0,
            long interval = 0)
        {

            if (!logger.Enabled) return;

            // copy details from request & response
            List<string[]> message = HttpMessage.build(request, response, response_body, request_body);


            // TODO copy data from session if configured
            // if (logger.rules.copy_session_field.Count != 0) {
                // TODO check if System.Web.HttpContext can be used instead
                // Since it has a HttpContext.Session property
                // Microsoft.AspNetCore.Http.HttpContext is currenty in use, and it does not
            // }

            // add timing details
            if (now == 0) now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            message.Add(new string[]{"now", now.ToString()});
            if (interval != 0) message.Add(new string[]{"interval", interval.ToString()});

            logger.SubmitIfPassing(message);
        }

        public static List<string[]> build(
            HttpRequest request,
            HttpResponse response,
            string response_body,
            string request_body)
        {
            var message = new List<string[]>();
            string method = request.Method;
            if (method != null) message.Add(new string[]{"request_method", method});
            string formatted_url = request.GetEncodedUrl();
            if (formatted_url != null) message.Add(new string[]{"request_url", formatted_url});
            message.Add(new string[]{"response_code", response.StatusCode.ToString()});
            appendRequestHeaders(message, request);
            appendRequestParams(message, request);
            appendResponseHeaders(message, response);
            if (request_body != null && !request_body.Equals("")) message.Add(new string[]{"request_body", request_body});
            if (response_body != null && !response_body.Equals("")) message.Add(new string[]{"response_body", response_body});
            return message;
        }
        private static void appendRequestHeaders(List<string[]> message, HttpRequest request)
        {
            foreach (var headerKey in request.Headers.Keys)
            {
                string name = "request_header:" + headerKey;
                string[] headers = request.Headers.GetCommaSeparatedValues(headerKey);
                foreach (var header in headers)
                {
                    message.Add(new string[] {name, header});
                }
            }
        }
        // TODO check if System.Web.HttpContext can be used instead
        // Since its request.Params includes query + form + cookies + server
        // Microsoft.AspNetCore.Http.HttpContext is currenty in use, and it ony has request.Query
        private static void appendRequestParams(List<string[]> message, HttpRequest request)
        {
            foreach (var paramKey in request.Query.Keys)
            {
                string name = "request_param:" + paramKey;
                foreach (var param in request.Query[paramKey])
                {
                    if(param is not null) message.Add(new string[] {name, param});
                }
            }
        }
        private static void appendResponseHeaders(List<string[]> message, HttpResponse response)
        {
            foreach (var headerKey in response.Headers.Keys)
            {
                string name = "response_header:" + headerKey;
                string[] headers = response.Headers.GetCommaSeparatedValues(headerKey);
                foreach (var header in headers)
                {
                    message.Add(new string[] {name, header});
                }
            }
        }
    }
}