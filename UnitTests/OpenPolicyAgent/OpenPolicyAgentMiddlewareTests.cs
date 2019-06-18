using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Airbag.OpenPolicyAgent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using static Airbag.OpenPolicyAgent.OpenPolicyAgentAuthorizationMiddlewareConfiguration;

namespace Airbag.UnitTests.OpenPolicyAgent
{
    public class OpenPolicyAgentMiddlewareTests
    {
        private Mock<IOpenPolicyAgent> mOpenPolicyAgent;
        private OpenPolicyAgentAuthorizationMiddlewareConfiguration mConfiguration;
        private OpenPolicyAgentAuthorizationMiddleware mTarget;
        private DefaultHttpContext mHttpContext;

        public OpenPolicyAgentMiddlewareTests()
        {
            mOpenPolicyAgent = new Mock<IOpenPolicyAgent>();
            mConfiguration = new OpenPolicyAgentAuthorizationMiddlewareConfiguration {QueryPath = "dummy"};
            var logger = new LoggerFactory().CreateLogger<OpenPolicyAgentAuthorizationMiddleware>();

            mTarget = new OpenPolicyAgentAuthorizationMiddleware(
                new Mock<RequestDelegate>().Object,
                mOpenPolicyAgent.Object,
                logger,
                mConfiguration);

            mHttpContext = new DefaultHttpContext();
        }

        [Fact]
        public async Task InvokeAsync_OpaDisabled_NotCallingOPA()
        {
            mConfiguration.Mode = AuthorizationMode.Disabled;

            await mTarget.InvokeAsync(mHttpContext);

            mOpenPolicyAgent.Verify(
                x => x.Query(It.IsAny<string>(), It.IsAny<OpenPolicyAgentQueryRequest>()),
                Times.Never);

            Assert.Equal(200, mHttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_CallingOPA_WithAllRelevantRequestParams()
        {
            mConfiguration.Mode = AuthorizationMode.Enabled;

            var queryList = new Dictionary<string, StringValues>
            {
                {"a", new StringValues(new[] {"b", "c"})}
            };
            var captures = new List<OpenPolicyAgentQueryRequest>();

            mHttpContext.User =
                new ClaimsPrincipal(new ClaimsIdentity(new[] {new Claim("a", "b"), new Claim("a", "c")}));

            mHttpContext.Request.Path = "/api/v1/someController";
            mHttpContext.Request.Method = "GET";
            mHttpContext.Request.Query = new QueryCollection(queryList);

            mOpenPolicyAgent.Setup(x => x.Query(mConfiguration.QueryPath, Capture.In(captures)))
                .Returns(Task.FromResult(new OpenPolicyAgentQueryResponse {Result = true}));

            await mTarget.InvokeAsync(mHttpContext);

            mOpenPolicyAgent.Verify(
                x => x.Query(mConfiguration.QueryPath, It.IsAny<OpenPolicyAgentQueryRequest>()),
                Times.Once);

            var opaRequest = captures.Single();

            Assert.Equal("GET", opaRequest.Input.Method);
            Assert.Equal(new[] {"api", "v1", "someController"}, opaRequest.Input.Path);

            var queryItem = opaRequest.Input.Query.Single();

            Assert.Equal("a", queryItem.Key);
            Assert.Equal(new[] {"b", "c"}, queryItem.Value);

            var claimItem = opaRequest.Input.Claims.Single();

            Assert.Equal("a", claimItem.Key);
            Assert.Equal(new[] {"b", "c"}, claimItem.Value);
        }

        [Theory]
        [InlineData(AuthorizationMode.Enabled, true, 200)]
        [InlineData(AuthorizationMode.Enabled, false, 403)]
        [InlineData(AuthorizationMode.LogOnly, true, 200)]
        [InlineData(AuthorizationMode.LogOnly, false, 200)]
        public async Task InvokeAsync_OpaReturnsFalse_RequestDenied(AuthorizationMode mode, bool opaReturnValue,
            int expectedStatusCode)
        {
            mConfiguration.Mode = mode;
            mOpenPolicyAgent.Setup(x => x.Query(mConfiguration.QueryPath, It.IsAny<OpenPolicyAgentQueryRequest>()))
                .Returns(Task.FromResult(new OpenPolicyAgentQueryResponse {Result = opaReturnValue}));

            await mTarget.InvokeAsync(mHttpContext);

            Assert.Equal(expectedStatusCode, mHttpContext.Response.StatusCode);
        }

        [Theory]
        [InlineData(AuthorizationMode.Enabled, 403)]
        [InlineData(AuthorizationMode.LogOnly, 200)]
        public async Task InvokeAsync_OpaRequestFailed_FailureHandled(
            AuthorizationMode mode,
            int expectedStatusCode)
        {
            mConfiguration.Mode = mode;
            var exception = new Exception("");
            mOpenPolicyAgent.Setup(x => x.Query(mConfiguration.QueryPath, It.IsAny<OpenPolicyAgentQueryRequest>()))
                .Throws(exception);

            await mTarget.InvokeAsync(mHttpContext);

            Assert.Equal(expectedStatusCode, mHttpContext.Response.StatusCode);
        }
    }
}