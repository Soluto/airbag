# airbag
airbag is a tiny sidecar for your docker containers, meant to handle jwt authentication and basic metrics collection for you.

## Configuration
airbag uses environment variables for configuration, and supports the following options:
* BACKEND_HOST_NAME - The name or ip of your service. Requests will be forwarded to this host once authenticated.
* BACKEND_PORT - The port exposed by your service. Requests will be forwarded to this port on the host once authenticated.
* AUTHORITY - The jwt authentication authority to use for authentication and token validation.
* AUDIENCE - Only tokens for this audience will be accepted and considered valid.
* ISSUER - Only tokens from this issuer will be accepted and considered valid.
* WHITELISTED_ROUTES - Backend routes that shouldn't be authenticated (for example, a health-check endpoint). Seperate the routes with `,`.  
example: `/isAlive,/health,/something/anonymous`
* COLLECT_METRICS - Enable or disable metrics collection. Metrics are collected using [AppMetrics](https://github.com/AppMetrics/AppMetrics)

