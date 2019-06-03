using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestEase;

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
            if(mConfiguration.Mode 
                == OpenPolicyAgentAuthorizationMiddlewareConfiguration.AuthorizationMode.Disabled)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value.Split("/").Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var method = context.Request.Method;

            var claims = context.Request.HttpContext.User.Claims
                .GroupBy(c => c.Type)
                .Select(g => KeyValuePair.Create(g.Key, g.Select(c => c.Value).ToArray()));

            var request = new OpenPolicyAgentQueryRequest
            {
                Input = new OpenPolicyAgentInput
                {
                    Path = path,
                    Method = method,
                    Query = context.Request.Query.Select(x => KeyValuePair.Create(x.Key, x.Value.ToArray())),
                    Claims = claims
                }
            };

            mLogger.LogDebug("Running OPA query");

            OpenPolicyAgentQueryResponse response = null;

            try
            {
                response = await mOpenPolicyAgent.Query(mConfiguration.QueryPath, request);
            }catch(ApiException e)
            {
                var decisionId = Guid.NewGuid().ToString();
                mLogger.LogError("OPA request failed {excpetion}, decision id: {decisionId}", e, decisionId);
                response = new OpenPolicyAgentQueryResponse
                {
                    Result = null,
                    DecisionId = decisionId
                };
            }
            
            mLogger.LogInformation("OPA returned {result}, decision id: {decisionId}",
                response.Result,
                response.DecisionId);
                
            if ((response.Result == null || response.Result == false) && 
                (mConfiguration.Mode == 
                    OpenPolicyAgentAuthorizationMiddlewareConfiguration.AuthorizationMode.Enabled))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            await _next(context);
        }
    }
}
