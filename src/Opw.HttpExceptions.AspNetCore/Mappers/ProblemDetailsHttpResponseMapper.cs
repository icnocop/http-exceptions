using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Net;

namespace Opw.HttpExceptions.AspNetCore.Mappers
{
    /// <summary>
    /// Default mapper for mapping HTTP response errors to ProblemDetails.
    /// </summary>
    public class ProblemDetailsHttpResponseMapper : IHttpResponseMapper
    {
        /// <summary>
        /// HttpExceptions options.
        /// </summary>
        protected IOptions<HttpExceptionsOptions> Options { get; }

        /// <summary>
        /// The status code to map for.
        /// </summary>
        public int Status { get; set; } = int.MinValue;

        /// <summary>
        /// Initializes the HttpResponseMapper.
        /// </summary>
        public ProblemDetailsHttpResponseMapper(IOptions<HttpExceptionsOptions> options)
        {
            Options = options;
        }

        /// <summary>
        /// Can the given status code be mapped for.
        /// </summary>
        /// <param name="status">The status code to map for.</param>
        public bool CanMap(int status)
        {
            return Status == int.MinValue || Status == status;
        }

        /// <summary>
        /// Maps the HTTP response error to a ProblemDetailsResult.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="actionResult">A representation of the HTTP response error as a ProblemDetailsResult.</param>
        public bool TryMap(HttpResponse response, out IStatusCodeActionResult actionResult)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));

            actionResult = default;

            if (!CanMap(response.StatusCode))
                return false;

            try
            {
                actionResult = Map(response);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates and returns a representation of the HTTP response error as a ProblemDetailsResult.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>A representation of the response error as a ProblemDetailsResult.</returns>
        public IStatusCodeActionResult Map(HttpResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));

            if (!CanMap(response.StatusCode))
                throw new ArgumentOutOfRangeException(nameof(response), response, $"HttpResponse status is not {Status}.");

            var problemDetails = new ProblemDetails
            {
                Status = MapStatus(response),
                Type = MapType(response),
                Title = MapTitle(response),
                Detail = MapDetail(response),
                Instance = MapInstance(response)
            };

            return new ProblemDetailsResult(problemDetails);
        }

        /// <summary>
        /// Map the ProblemDetails.Instance property using the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>Returns the request path, or null.</returns>
        protected virtual string MapInstance(HttpResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));

            if (Options.Value.HttpContextInstanceMapping != null)
            {
                string instance = Options.Value.HttpContextInstanceMapping(response.HttpContext);
                if (!string.IsNullOrEmpty(instance))
                    return instance;
            }

            if (response.HttpContext.Request?.Path.HasValue == true)
                return response.HttpContext.Request.Path;

            return null;
        }

        /// <summary>
        /// Map the ProblemDetails.Status property using the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>Returns the status of the response.</returns>
        protected virtual int MapStatus(HttpResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));

            if (Options.Value.HttpContextStatusMapping != null)
            {
                int? status = Options.Value.HttpContextStatusMapping(response.HttpContext);
                if (status.HasValue)
                    return status.Value;
            }

            return response.StatusCode;
        }

        /// <summary>
        /// Map the ProblemDetails.Title property using the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>Returns the HTTP status name or the status code.</returns>
        protected virtual string MapTitle(HttpResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));

            string status = null;

            if (Options.Value.HttpContextTitleMapping != null)
            {
                status = Options.Value.HttpContextTitleMapping(response.HttpContext);
                if (!string.IsNullOrEmpty(status))
                    return status;
            }

            status = response.StatusCode.ToString();
            try
            {
                status = ((HttpStatusCode)response.StatusCode).ToString();
            }
            catch { }

            return status;
        }

        /// <summary>
        /// Map the ProblemDetails.Detail property using the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>Returns the HTTP status name or the status code.</returns>
        protected virtual string MapDetail(HttpResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));

            if (Options.Value.HttpContextDetailMapping != null)
            {
                string detail = Options.Value.HttpContextDetailMapping(response.HttpContext);
                if (!string.IsNullOrEmpty(detail))
                    return detail;
            }

            return MapTitle(response);
        }

        /// <summary>
        /// Map the ProblemDetails.Type property using the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>Returns a status code information link (https://tools.ietf.org/html/rfc7231) or the URI with the HTTP status name ("error:[status:slug]").</returns>
        protected virtual string MapType(HttpResponse response)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));

            Uri uri = null;

            if (Options.Value.HttpContextTypeMapping != null)
                uri = Options.Value.HttpContextTypeMapping(response.HttpContext);

            if (uri == null && response.StatusCode.TryGetInformationLink(out var url))
            {
                try
                {
                    uri = new Uri(url);
                }
                catch { }
            }

            if (uri == null)
                uri = Options.Value.DefaultHelpLink;
            if (uri == null)
                uri = new Uri($"error:{MapTitle(response).ToSlug()}");
            return uri.ToString();
        }
    }
}