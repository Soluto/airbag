# airbag
[![Build Status](https://travis-ci.org/Soluto/airbag.svg?branch=master)](https://travis-ci.org/Soluto/airbag)   
   
airbag is a tiny sidecar for your docker containers, meant to handle jwt authentication and basic metrics collection for you.

## How to use it
1. Deploy airbag next to your container (In kubernetes, you would usually put them in the same pod).  
2. Configure airbag's BACKEND_HOST_NAME (If airbag and your container are in the same pod, set this to localhost) and BACKEND_SERVICE_PORT (the port your service is listening on). 
3. Route traffic directed for your container to airbag 

## Configuration
airbag uses environment variables for configuration, and supports the following options:
* **BACKEND_HOST_NAME** - The name or ip of your service. Requests will be forwarded to this host once authenticated. **Defaults to localhost**.
* **BACKEND_SERVICE_PORT** - The port exposed by your service. Requests will be forwarded to this port on the host once authenticated. **Defaults to 80**.
* **AUTHORITY** - The jwt authentication authority to use for authentication and token validation.
* **AUDIENCE** - Only tokens for this audience will be accepted and considered valid.
* **ISSUER** - Only tokens from this issuer will be accepted and considered valid.
* **UNAUTHENTICATED_ROUTES** - Backend routes that shouldn't be authenticated (for example, a health-check endpoint). Seperate the routes with `,`.  
example: `/isAlive,/health,/something/anonymous`  
If a route contains a wildcard ( * ) then all matching routes will not be authenticated (For example - `/swagger/*` will cause all routes which start with `/swagger/` to be unauthenticated.   
* **COLLECT_METRICS** - Enable or disable metrics collection. Metrics are collected using [AppMetrics](https://github.com/AppMetrics/AppMetrics)

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