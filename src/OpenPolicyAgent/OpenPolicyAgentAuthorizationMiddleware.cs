using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestEase;
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

            switch (mConfiguration.Mode)
            {
                case AuthorizationMode.Disabled:
                case AuthorizationMode.LogOnly:
                case AuthorizationMode.Enabled when response == OpaQueryResult.Allowed:
                    await _next(context);
                    break;
                case AuthorizationMode.Enabled when response == OpaQueryResult.CriticalError:
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Internal Server Error");
                    break;
                case AuthorizationMode.Enabled:
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    break;
            }
        }

        private async Task<OpaQueryResult> ExecuteQuery(string path, OpenPolicyAgentQueryRequest query)
        {
            try
            {
                var response = await mOpenPolicyAgent.Query(path, query);

                mLogger.LogInformation("OPA returned {result}, decision id: {decisionId}", response.Result,
                    response.DecisionId);

                return response.Result == null ? OpaQueryResult.Unknown
                    : response.Result.Value ? OpaQueryResult.Allowed
                    : OpaQueryResult.Denied;
            }
            catch (ApiException ex) when ((int) ex.StatusCode >= 400 && (int) ex.StatusCode < 500)
            {
                mLogger.LogError(ex, "HTTP error while invoking OPA");
                
                return OpaQueryResult.Error;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Critical error while invoking OPA");

                return OpaQueryResult.CriticalError;
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
    }

    internal enum OpaQueryResult
    {
        Allowed,
        Denied,
        Error,
        CriticalError,
        Unknown
    }
}