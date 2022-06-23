# airbag
[![Codefresh build status]( https://g.codefresh.io/api/badges/pipeline/soluto/airbag%2Ftest?type=cf-1&key=eyJhbGciOiJIUzI1NiJ9.NTkwOTg1MmQ2ZDAxYjcwMDA2Yjc1ODBm.fODYFsnTAGVNVeEAA6lI0g-sTAfHjh5B9BWrOtDvSSE)]( https://g.codefresh.io/pipelines/edit/new/builds?id=5e25741c6ec1ec2de7cb9068&pipeline=test&projects=airbag&projectId=5e134c726e1ebe541cd3811b)

airbag is a tiny sidecar for your docker containers, meant to handle jwt authentication and basic metrics collection for you.

## How to use it
1. Deploy airbag next to your container (In kubernetes, you would usually put them in the same pod).  
   airbag's image can be found at https://hub.docker.com/r/soluto/airbag/
2. Configure airbag's BACKEND_HOST_NAME (if airbag and your container are in the same pod, set this to localhost) and BACKEND_SERVICE_PORT (the port your service is listening on). 
3. Route traffic directed to your container to airbag. 

## Configuration
airbag uses environment variables for configuration, and supports the following options:
* **BACKEND_HOST_NAME** - The name or ip of your service. Requests will be forwarded to this host once authenticated. **Defaults to localhost**.
* **BACKEND_SERVICE_PORT** - The port exposed by your service. Requests will be forwarded to this port on the host once authenticated. **Defaults to 80**.
* **AUTHORITY** - The jwt authentication authority to use for authentication and token validation.
* **AUDIENCE** - Only tokens for this audience will be accepted and considered valid.
* **ISSUER** - Only tokens from this issuer will be accepted and considered valid.
* **UNAUTHENTICATED_ROUTES** - Backend routes that shouldn't be authenticated (for example, a health-check endpoint). Seperate the routes with `,`.  
Example: `/isAlive,/health,/something/anonymous`  
If a route contains a wildcard ( * ) then all matching routes will not be authenticated (for example - `/swagger/*` will cause all routes that start with `/swagger/` to be unauthenticated).   
* **COLLECT_METRICS** - Enable or disable metrics collection. Metrics are collected using [AppMetrics](https://github.com/AppMetrics/AppMetrics).
Metrics will be available under `/airbag/metrics`
* **AUTHORIZED_ROUTES_ENABLED** - Enable or disable route white-listing.

### Using with multiple auth providers
To use multiple auth providers, provide these parmaters for every provider as a prefix:
* **AUTHORITY_{{providerName}}**
* **AUDIENCE_{{providerName}}**
* **ISSUER_{{providerName}}**

#### Example
```yaml
# Foo provider parameters
- AUTHORITY_FOO=http://foo_auth_server
- ISSUER_FOO=http://foo
- AUDIENCE_FOO=foo_api
# Bar provider parameters
- AUTHORITY_BAR=http://bar_auth_server
- ISSUER_BAR=http://bar
- AUDIENCE_BAR=bar_api
```

### External Authorization with Open Policy Agent
airbag support authorization of incoming request with [Open Policy Agent](https://www.openpolicyagent.org)(OPA). 
If enabled, airbag will query OPA for a decision on each incoming request, and based on OPA decision approve or deny the request.
Approved requests will be passed to upstream, denied request will return to client with a 403 status code.

#### Configuration
To use OPA, you need to configure the following environment variables:
* OPA_URL - The URL of the OPA server.
* OPA_QUERY_PATH - The path used for OPA query. This path should return a single boolean result.
* OPA_MODE - The mode of OPA authorization. Supports 3 values:
  * Disabled - Do not query OPA at all. This is the default value.
  * LogOnly - Query OPA, but only log result - do not perform authorization. 
  * Enabled - Query OPA and perform authorization.
