# airbag
airbag is a tiny sidecar for your docker containers, meant to handle jwt authentication and basic metrics collection for you.

## configuration
airbag uses environment variables for configuration, and supports the following options:
* BACKEND_HOST_NAME - the name or ip of your service. Requests will be forwarded to this host once authenticated.
* BACKEND_PORT - the port exposed by your service. Requests will be forwarded to this port on the host once authenticated.
* AUTHORITY - the jwt authentication authority to use for authentication and token validation.
* ISSUER - only tokens from this issuer will be accepted and considered valid.
