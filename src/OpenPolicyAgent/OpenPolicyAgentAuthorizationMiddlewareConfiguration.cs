namespace Airbag.OpenPolicyAgent
{
    public class OpenPolicyAgentAuthorizationMiddlewareConfiguration
    {
        public enum AuthorizationMode
        {
            Disabled,
            LogOnly,
            Enabled
        }

        public AuthorizationMode Mode { get; set; }

        public string QueryPath { get; set; }
    }
}
