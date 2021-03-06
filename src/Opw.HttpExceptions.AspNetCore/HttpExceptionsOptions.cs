using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Opw.HttpExceptions.AspNetCore.Mappers;
using System;
using System.Collections.Generic;

namespace Opw.HttpExceptions.AspNetCore
{
    /// <summary>
    /// Represents the possible HttpExceptions options for services.
    /// </summary>
    public class HttpExceptionsOptions
    {
        /// <summary>
        /// Include exception details, default behavior is only to include exception details in a development environment.
        /// </summary>
        public Func<HttpContext, bool> IncludeExceptionDetails { get; set; }

        /// <summary>
        /// Include all public exception properties that are attributed with <see cref="ProblemDetailsAttribute"/> in exception details.
        /// Default <c>true</c>
        /// </summary>
        public bool IsProblemDetailsAttributeEnabled { get; set; } = true;

        /// <summary>
        /// Is the response an exception and should it be handled by the HttpExceptions middleware.
        /// </summary>
        public Func<HttpContext, bool> IsExceptionResponse { get; set; }

        /// <summary>
        /// Should an exception be logged by the HttpExceptions middleware or not, default behavior is to log all exceptions (all status codes).
        /// </summary>
        public Func<Exception, bool> ShouldLogException { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Type property using the exception.
        /// </summary>
        public Func<Exception, Uri> ExceptionTypeMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Type property using the HTTP context.
        /// </summary>
        public Func<HttpContext, Uri> HttpContextTypeMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Instance property using the exception.
        /// </summary>
        public Func<Exception, string> ExceptionInstanceMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Instance property using the HTTP context.
        /// </summary>
        public Func<HttpContext, string> HttpContextInstanceMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Title property using the exception.
        /// </summary>
        public Func<Exception, string> ExceptionTitleMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Title property using the HTTP context.
        /// </summary>
        public Func<HttpContext, string> HttpContextTitleMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Status property using the exception.
        /// </summary>
        public Func<Exception, int?> ExceptionStatusMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Status property using the HTTP context.
        /// </summary>
        public Func<HttpContext, int?> HttpContextStatusMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Detail property using the exception.
        /// </summary>
        public Func<Exception, string> ExceptionDetailMapping { get; set; }

        /// <summary>
        /// Inject a function to map the ProblemDetails.Detail property using the HTTP context.
        /// </summary>
        public Func<HttpContext, string> HttpContextDetailMapping { get; set; }

        /// <summary>
        /// A default help link that will be used to map the ProblemDetails.Type property, if the Exception.HelpLink
        /// or the HTTP status code information link are empty.
        /// </summary>
        public Uri DefaultHelpLink { get; set; }

        /// <summary>
        /// Register of the ExceptionMappers that will be used during mapping.
        /// </summary>
        public IDictionary<Type, ExceptionMapperDescriptor> ExceptionMapperDescriptors { get; set; } = new Dictionary<Type, ExceptionMapperDescriptor>();

        /// <summary>
        /// Gets or sets the ExceptionMapper collection that will be used during mapping.
        /// </summary>
        public ICollection<IExceptionMapper> ExceptionMappers { get; set; } = new List<IExceptionMapper>();

        /// <summary>
        /// Register of the HttpResponseMappers that will be used during mapping.
        /// HttpResponseMappers handle unauthorized and other non-exceptions responses.
        /// </summary>
        public IDictionary<int, HttpResponseMapperDescriptor> HttpResponseMapperDescriptors { get; set; } = new Dictionary<int, HttpResponseMapperDescriptor>();

        /// <summary>
        /// Gets or sets the HttpResponseMapper collection that will be used during mapping.
        /// HttpResponseMappers handle unauthorized and other non-exceptions responses.
        /// </summary>
        public ICollection<IHttpResponseMapper> HttpResponseMappers { get; set; } = new List<IHttpResponseMapper>();

        /// <summary>
        /// Initializes the HttpExceptionsOptions.
        /// </summary>
        public HttpExceptionsOptions() { }

        internal bool TryMap(Exception exception, HttpContext context, out IStatusCodeActionResult actionResult)
        {
            foreach (var mapper in ExceptionMappers)
            {
                if (mapper.TryMap(exception, context, out actionResult))
                    return true;
            }

            actionResult = default;
            return false;
        }

        internal bool TryMap(HttpResponse response, out IStatusCodeActionResult actionResult)
        {
            foreach (var mapper in HttpResponseMappers)
            {
                if (mapper.TryMap(response, out actionResult))
                    return true;
            }

            actionResult = default;
            return false;
        }
    }
}
