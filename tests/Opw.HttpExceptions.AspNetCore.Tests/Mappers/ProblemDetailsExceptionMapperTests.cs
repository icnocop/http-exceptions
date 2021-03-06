using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Opw.HttpExceptions.AspNetCore.Tests.TestData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Xunit;

namespace Opw.HttpExceptions.AspNetCore.Mappers
{
    public class ProblemDetailsExceptionMapperTests
    {
        private readonly ExposeProtectedProblemDetailsExceptionMapper _mapper;

        public ProblemDetailsExceptionMapperTests()
        {
            var optionsMock = TestHelper.CreateHttpExceptionsOptionsMock(false, new Uri("http://www.example.com/help-page"));
            _mapper = new ExposeProtectedProblemDetailsExceptionMapper(optionsMock.Object);
        }

        [Fact]
        public void Map_Should_ReturnProblemDetails()
        {
            var actionResult = _mapper.Map(new ApplicationException(), new DefaultHttpContext());

            actionResult.Should().BeOfType<ProblemDetailsResult>();
            var problemDetailsResult = (ProblemDetailsResult)actionResult;

            problemDetailsResult.Value.ShouldNotBeNull(HttpStatusCode.InternalServerError);
            problemDetailsResult.Value.Instance.Should().BeNull();

            var result = problemDetailsResult.Value.TryGetExceptionDetails(out var exception);

            result.Should().BeFalse();
            exception.Should().BeNull();
        }

        [Fact]
        public void Map_Should_ReturnThrowArgumentOutOfRangeException_ForInvalidExceptionType()
        {
            var mapper = new ProblemDetailsExceptionMapper<ApplicationException>(TestHelper.CreateHttpExceptionsOptionsMock(false).Object);

            Action action = () => mapper.Map(new NotSupportedException(), new DefaultHttpContext());

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Map_Should_ReturnProblemDetails_WithHelpLink()
        {
            var helpLink = "https://docs.microsoft.com/en-us/dotnet/api/system.exception.helplink?view=netcore-2.2";
            var actionResult = _mapper.Map(new ApplicationException { HelpLink = helpLink }, new DefaultHttpContext());

            actionResult.Should().BeOfType<ProblemDetailsResult>();
            var problemDetailsResult = (ProblemDetailsResult)actionResult;

            problemDetailsResult.Value.ShouldNotBeNull(HttpStatusCode.InternalServerError);
            problemDetailsResult.Value.Instance.Should().Be(helpLink);

            var result = problemDetailsResult.Value.TryGetExceptionDetails(out var exception);

            result.Should().BeFalse();
            exception.Should().BeNull();
        }

        [Fact]
        public void Map_Should_ReturnProblemDetails_WithExceptionDetails()
        {
            var mapper = TestHelper.CreateProblemDetailsExceptionMapper<Exception>(true);
            var actionResult = mapper.Map(new ApplicationException(), new DefaultHttpContext());

            actionResult.Should().BeOfType<ProblemDetailsResult>();
            var problemDetailsResult = (ProblemDetailsResult)actionResult;

            problemDetailsResult.Value.ShouldNotBeNull(HttpStatusCode.InternalServerError);
            problemDetailsResult.Value.Instance.Should().BeNull();

            var result = problemDetailsResult.Value.TryGetExceptionDetails(out var exception);

            result.Should().BeTrue();
            exception.Should().NotBeNull();
        }

        [Fact]
        public void Map_Should_ReturnProblemDetails_WithValidationErrors_ForValidationErrorException()
        {
            var exception = new ValidationErrorException<string>("param", new[] { "error1", "error1" });

            var mapper = TestHelper.CreateProblemDetailsExceptionMapper<Exception>(true);
            var actionResult = mapper.Map(exception, new DefaultHttpContext());

            actionResult.Should().BeOfType<ProblemDetailsResult>();
            var problemDetailsResult = (ProblemDetailsResult)actionResult;

            problemDetailsResult.Value.ShouldNotBeNull(HttpStatusCode.BadRequest);
            problemDetailsResult.Value.Instance.Should().Be(ResponseStatusCodeLink.BadRequest);

            var result = problemDetailsResult.Value.TryGetErrors(out var errors);

            result.Should().BeTrue();
            errors.Should().NotBeNull();
            errors.Should().HaveCount(1);
        }

        [Fact]
        public void Map_Should_ReturnProblemDetails_WithValidationErrors2_ForValidationErrorException()
        {
            var errorsIn = new Dictionary<string, string[]>();
            errorsIn.Add("memberName1", new[] { "error1", "error2" });
            errorsIn.Add("memberName2", new[] { "error1", "error2" });
            var exception = new ValidationErrorException<string>(errorsIn);

            var mapper = TestHelper.CreateProblemDetailsExceptionMapper<Exception>(true);
            var actionResult = mapper.Map(exception, new DefaultHttpContext());

            actionResult.Should().BeOfType<ProblemDetailsResult>();
            var problemDetailsResult = (ProblemDetailsResult)actionResult;

            problemDetailsResult.Value.ShouldNotBeNull(HttpStatusCode.BadRequest);
            problemDetailsResult.Value.Instance.Should().Be(ResponseStatusCodeLink.BadRequest);

            var result = problemDetailsResult.Value.TryGetErrors(out var errors);

            result.Should().BeTrue();
            errors.Should().NotBeNull();
            errors.Should().HaveCount(2);
        }

        [Fact]
        public void Map_Should_ReturnProblemDetails_WithValidationErrors2_ForValidationErrorException_WithComplexErrorObject()
        {
            var errorsIn = new Dictionary<string, ValidationResult[]>();
            errorsIn.Add("memberName1", new[] { new ValidationResult("message", new[] { "memberName1" }) });
            errorsIn.Add("memberName2", new[] { new ValidationResult("message", new[] { "memberName2" }) });
            var exception = new ValidationErrorException<ValidationResult>(errorsIn);

            var mapper = TestHelper.CreateProblemDetailsExceptionMapper<Exception>(true);
            var actionResult = mapper.Map(exception, new DefaultHttpContext());

            actionResult.Should().BeOfType<ProblemDetailsResult>();
            var problemDetailsResult = (ProblemDetailsResult)actionResult;

            problemDetailsResult.Value.ShouldNotBeNull(HttpStatusCode.BadRequest);
            problemDetailsResult.Value.Instance.Should().Be(ResponseStatusCodeLink.BadRequest);

            var result = problemDetailsResult.Value.TryGetErrors(out var errors);

            result.Should().BeTrue();
            errors.Should().NotBeNull();
            errors.Should().HaveCount(2);
        }

        [Fact]
        public void Map_Should_ReturnPropertiesAsProblemDetailsExtensions_ForExceptionWithProblemDetailsAttributeProperties()
        {
            var actionResult = _mapper.Map(new ProblemDetailsAttributeException("ProblemDetailsAttributeException has occurred.") { PropertyA = "AAA", PropertyB = 42, PropertyC = 123L }, new DefaultHttpContext());

            actionResult.Should().BeOfType<ProblemDetailsResult>();
            var problemDetailsResult = (ProblemDetailsResult)actionResult;

            problemDetailsResult.Value.ShouldNotBeNull(HttpStatusCode.Ambiguous);
            problemDetailsResult.Value.Instance.Should().NotBeNull();

            problemDetailsResult.Value.Extensions.Should().HaveCount(2);
            problemDetailsResult.Value.Extensions[nameof(ProblemDetailsAttributeException.PropertyA).ToCamelCase()].ToString().Should().Be("AAA");
            problemDetailsResult.Value.Extensions[nameof(ProblemDetailsAttributeException.PropertyB).ToCamelCase()].ToString().Should().Be("42");
            problemDetailsResult.Value.Extensions.Should().NotContainKey(nameof(ProblemDetailsAttributeException.PropertyC).ToCamelCase());

            var result = problemDetailsResult.Value.TryGetExceptionDetails(out var exception);

            result.Should().BeFalse();
            exception.Should().BeNull();
        }

        [Fact]
        public void MapDetail_Should_ReturnDetail_UsingOptionsExceptionDetailMappingOverride()
        {
            var detail = "ExceptionDetailMapping";

            var optionsMock = TestHelper.CreateHttpExceptionsOptionsMock(false, new Uri("http://www.example.com/help-page"));
            var options = optionsMock.Object;
            options.Value.ExceptionDetailMapping = (Exception ex) =>
            {
                return detail;
            };
            var mapper = new ExposeProtectedProblemDetailsExceptionMapper(options);

            var result = mapper.MapDetail(new ApplicationException(), new DefaultHttpContext());

            result.Should().Be(detail);
        }

        [Fact]
        public void MapDetail_Should_ReturnExceptionMessage()
        {
            var message = "Test exception message.";
            var exception = new ApplicationException(message);
            var result = _mapper.MapDetail(exception, new DefaultHttpContext());

            result.Should().Be(message);
        }

        [Fact]
        public void MapInstance_Should_ReturnUri_UsingOptionsExceptionInstanceMappingOverride()
        {
            var uri = "https://example.com/ExceptionInstanceMapping";

            var optionsMock = TestHelper.CreateHttpExceptionsOptionsMock(false, new Uri("http://www.example.com/help-page"));
            var options = optionsMock.Object;
            options.Value.ExceptionInstanceMapping = (Exception ex) =>
            {
                return uri;
            };
            var mapper = new ExposeProtectedProblemDetailsExceptionMapper(options);

            var result = mapper.MapInstance(new ApplicationException(), new DefaultHttpContext());

            result.Should().Be(uri);
        }

        [Fact]
        public void MapInstance_Should_ReturnExceptionHelpLink()
        {
            var helpLink = "https://docs.microsoft.com/en-us/dotnet/api/system.exception.helplink?view=netcore-2.2";
            var exception = new ApplicationException { HelpLink = helpLink };
            var result = _mapper.MapInstance(exception, new DefaultHttpContext());

            result.Should().Be(helpLink);
        }

        [Fact]
        public void MapInstance_Should_ReturnRequestPath()
        {
            var exception = new ApplicationException();
            var context = new DefaultHttpContext();
            var requestPath = "/test/123";
            context.Request.Path = requestPath;
            var result = _mapper.MapInstance(exception, context);

            result.Should().Be(requestPath);
        }

        [Fact]
        public void MapStatus_Should_ReturnStatus_UsingOptionsExceptionStatusMappingOverride()
        {
            int? status = 9999999;

            var optionsMock = TestHelper.CreateHttpExceptionsOptionsMock(false, new Uri("http://www.example.com/help-page"));
            var options = optionsMock.Object;
            options.Value.ExceptionStatusMapping = (Exception ex) =>
            {
                return status;
            };
            var mapper = new ExposeProtectedProblemDetailsExceptionMapper(options);

            var result = mapper.MapStatus(new ApplicationException(), new DefaultHttpContext());

            result.Should().Be(status);
        }

        [Fact]
        public void MapStatus_Should_ReturnInternalServerError()
        {
            var exception = new ApplicationException();
            var result = _mapper.MapStatus(exception, new DefaultHttpContext());

            result.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        [Fact]
        public void MapStatus_Should_ReturnBadRequest()
        {
            var exception = new HttpException(HttpStatusCode.BadRequest);
            var result = _mapper.MapStatus(exception, new DefaultHttpContext());

            result.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public void MapTitle_Should_ReturnTitle_UsingOptionsExceptionTitleMappingOverride()
        {
            var title = "ExceptionTitleMapping";

            var optionsMock = TestHelper.CreateHttpExceptionsOptionsMock(false, new Uri("http://www.example.com/help-page"));
            var options = optionsMock.Object;
            options.Value.ExceptionTitleMapping = (Exception ex) =>
            {
                return title;
            };
            var mapper = new ExposeProtectedProblemDetailsExceptionMapper(options);

            var result = mapper.MapTitle(new ApplicationException(), new DefaultHttpContext());

            result.Should().Be(title);
        }

        [Fact]
        public void MapTitle_Should_ReturnFormattedExceptionName()
        {
            var exception = new DivideByZeroException();
            var result = _mapper.MapTitle(exception, new DefaultHttpContext());

            result.Should().Be("DivideByZero");
        }

        [Fact]
        public void MapType_Should_ReturnUri_UsingOptionsExceptionTypeMappingOverride()
        {
            var uri = new Uri("https://example.com/ExceptionTypeMapping");

            var optionsMock = TestHelper.CreateHttpExceptionsOptionsMock(false, new Uri("http://www.example.com/help-page"));
            var options = optionsMock.Object;
            options.Value.ExceptionTypeMapping = (Exception ex) =>
            {
                return uri;
            };
            var mapper = new ExposeProtectedProblemDetailsExceptionMapper(options);

            var result = mapper.MapType(new ApplicationException(), new DefaultHttpContext());

            result.Should().Be(uri.ToString());
        }

        [Fact]
        public void MapType_Should_ReturnExceptionHelpLink_UsingDefaultExceptionTypeMapping()
        {
            var helpLink = "https://docs.microsoft.com/en-us/dotnet/api/system.exception.helplink?view=netcore-2.2";
            var exception = new ApplicationException { HelpLink = helpLink };
            var result = _mapper.MapType(exception, new DefaultHttpContext());

            result.Should().Be(helpLink);
        }

        [Fact]
        public void MapType_Should_ReturnDefaultHelpLink_UsingDefaultExceptionTypeMapping_WhenExceptionHelpLinkIsNoValidUri()
        {
            var exception = new ApplicationException { HelpLink = "invalid-link" };
            var result = _mapper.MapType(exception, new DefaultHttpContext());

            result.Should().Be("http://www.example.com/help-page");
        }

        [Fact]
        public void MapType_Should_ReturnHttpStatusCodeInformationLink_UsingDefaultExceptionTypeMapping_ForHttpException()
        {
            var exception = new BadRequestException();
            var result = _mapper.MapType(exception, new DefaultHttpContext());

            result.Should().Be(ResponseStatusCodeLink.BadRequest);
        }

        [Fact]
        public void MapType_Should_ReturnDefaultHelpLink_UsingDefaultExceptionTypeMapping_WhenExceptionHasNoHelpLink()
        {
            var exception = new ApplicationException();
            var result = _mapper.MapType(exception, new DefaultHttpContext());

            result.Should().Be("http://www.example.com/help-page");
        }

        [Fact]
        public void MapType_Should_ReturnFormattedExceptionName_UsingDefaultExceptionTypeMapping_WhenDefaultHelpLinkIsNull()
        {
            var optionsMock = TestHelper.CreateHttpExceptionsOptionsMock(false, null);
            var mapper = new ExposeProtectedProblemDetailsExceptionMapper(optionsMock.Object);

            var exception = new DivideByZeroException();
            var result = mapper.MapType(exception, new DefaultHttpContext());

            result.Should().Be("error:divide-by-zero");
        }

        private class ExposeProtectedProblemDetailsExceptionMapper : ProblemDetailsExceptionMapper<Exception>
        {
            public ExposeProtectedProblemDetailsExceptionMapper(IOptions<HttpExceptionsOptions> options) : base(options) { }

            public new string MapDetail(Exception exception, HttpContext context)
            {
                return base.MapDetail(exception, context);
            }

            public new string MapInstance(Exception exception, HttpContext context)
            {
                return base.MapInstance(exception, context);
            }

            public new int MapStatus(Exception exception, HttpContext context)
            {
                return base.MapStatus(exception, context);
            }

            public new string MapTitle(Exception exception, HttpContext context)
            {
                return base.MapTitle(exception, context);
            }

            public new string MapType(Exception exception, HttpContext context)
            {
                return base.MapType(exception, context);
            }
        }
    }
}
