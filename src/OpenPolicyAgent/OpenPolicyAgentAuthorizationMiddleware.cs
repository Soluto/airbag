using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static Airbag.OpenPolicyAgent.OpenPolicyAgentAuthorizationMiddlewareConfiguration;

namespace Airbag.OpenPolicyAgent
{
    public class OpenPolicyAgentAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOpenPolicyAgent mOpenPolicyAgent;
        private readonly ILogger<OpenPolicyAgentAuthorizationMiddleware> mLogger;
        private readonly OpenPolicyAgentAuthorizationMiddlewareConfiguration mConfiguration;

        public OpenPolicyAgentAuthorizationMiddleware(
            RequestDelegate next,
            IOpenPolicyAgent openPolicyAgent,
            ILogger<OpenPolicyAgentAuthorizationMiddleware> logger,
            OpenPolicyAgentAuthorizationMiddlewareConfiguration configuration)
        {
            _next = next;
            mOpenPolicyAgent = openPolicyAgent;
            mLogger = logger;
            mConfiguration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (mConfiguration.Mode == AuthorizationMode.Disabled)
            {
                await _next(context);
                return;
            }

            mLogger.LogDebug("Running OPA query");

            var request = BuildRequest(context);
            var response = await ExecuteQuery(mConfiguration.QueryPath, request);

            if (ShouldReturn403Forbidden(response))
            {
                context.Response.StatusCode = 403;
                if (response.DecisionId != null)
                {
                    context.Response.Headers["X-Decision-Id"] = response.DecisionId;
                }

                await context.Response.WriteAsync("Forbidden");
            }
            else
            {
                await _next(context);
            }
        }

        private async Task<OpenPolicyAgentQueryResponse> ExecuteQuery(string path, OpenPolicyAgentQueryRequest query)
        {
            try
            {
                var response = await mOpenPolicyAgent.Query(path, query);

                mLogger.LogInformation("OPA returned {result}, decision id: {decisionId}", response.Result,
                    response.DecisionId);

                return response;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Error while invoking OPA");

                return new OpenPolicyAgentQueryResponse
                {
                    Result = null
                };
            }
        }

        private static OpenPolicyAgentQueryRequest BuildRequest(HttpContext context)
        {
            var path = context.Request.Path.Value.Split("/").Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var method = context.Request.Method;

            var claims = context.Request.HttpContext.User.Claims
                .GroupBy(c => c.Type)
                .Select(g => KeyValuePair.Create(g.Key, g.Select(c => c.Value).ToArray()));

            return new OpenPolicyAgentQueryRequest
            {
                Input = new OpenPolicyAgentInput
                {
                    Path = path,
                    Method = method,
                    Query = context.Request.Query.Select(x => KeyValuePair.Create(x.Key, x.Value.ToArray())),
                    Claims = claims
                }
            };
        }

        private bool ShouldReturn403Forbidden(OpenPolicyAgentQueryResponse response = null)
        {
            switch (mConfiguration.Mode)
            {
                case AuthorizationMode.Disabled:
                case AuthorizationMode.LogOnly:
                    return false;
                case AuthorizationMode.Enabled when response == null:
                    return true;
                case AuthorizationMode.Enabled:
                    return !response.Result.HasValue || response.Result.Value == false;
                default:
                    throw new ArgumentOutOfRangeException("This should never happen");
            }
        }
    }
}