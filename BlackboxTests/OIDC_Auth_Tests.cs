using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Xunit;

namespace BlackboxTests
{
    // these tests assume they run in a docker-compose environment
    // they assume that other than the tests container, the other containers also running are airbag and nginx
    // see the BlackboxTests/docker-compose file for details
    public class OidcAuthTests
    {
        private static TokenClient _validTokenClient;
        private static TokenClient _differentIssuerTokenClient;
        private static TokenClient _otherSignatureTokenClient;
        private const string AirbagUrl = "http://localhost:5001/";

        public OidcAuthTests()
        {
            var validDiscovery = DiscoveryClient.GetAsync("http://localhost:5002").Result;
            _validTokenClient = new TokenClient(validDiscovery.TokenEndpoint, "client", "secret");

            var otherIssuerDiscovery = DiscoveryClient.GetAsync("http://localhost:5004").Result;
            _differentIssuerTokenClient = new TokenClient(otherIssuerDiscovery.TokenEndpoint, "client", "secret");

            var otherSignatureDiscovery = DiscoveryClient.GetAsync("http://localhost:5005").Result;
            _otherSignatureTokenClient = new TokenClient(otherSignatureDiscovery.TokenEndpoint, "client", "secret");
        }

        [Fact]
        public async Task RequestWithValidToken_ForwardRequestToBackendContainer()
        {
            var tokenResponse = await _validTokenClient.RequestClientCredentialsAsync("api1");

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithoutAuthorizationHeader_RouteIsWhitelisted_ForwardRequestToBackendContainer()
        {
            var result = await new HttpClient().GetAsync(AirbagUrl + "isAlive");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task
            RequestWithoutAuthorizationHeader_RouteIsWhitelistedWithWildCard_ForwardRequestToBackendContainer()
        {
            var result = await new HttpClient().GetAsync(AirbagUrl + "foo/bar");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task
            RequestWithoutAuthorizationHeader_RouteIsNotWhitelistedButContainsPartialWildcard_Return403Forbidden()
        {
            var result = await new HttpClient().GetAsync(AirbagUrl + "api/foo/bar");
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithoutAuthorizationHeader_RouteIsNotWhitelisted_Return403Forbidden()
        {
            var result = await new HttpClient().GetAsync(AirbagUrl);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithModifiedJwtToken_Return403Forbidden()
        {
            var tokenResponse = await _validTokenClient.RequestClientCredentialsAsync("api1");
            var arr = tokenResponse.AccessToken.ToCharArray();
            arr[20] = 'g';
            var temperedToken = arr.ToString();

            HttpResponseMessage result = await SendRequest(temperedToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithWrongIssuer_Return403Forbidden()
        {
            var tokenResponse = await _differentIssuerTokenClient.RequestClientCredentialsAsync("api1");

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }


        [Fact]
        public async Task RequestWithWrongSignature_Return403Forbidden()
        {
            var tokenResponse = await _otherSignatureTokenClient.RequestClientCredentialsAsync("api1");

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithWrongAudeince_Return403Forbidden()
        {
            var tokenResponse = await _differentIssuerTokenClient.RequestClientCredentialsAsync("api2");

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithExpiredToken_Return403Forbidden()
        {
            var tokenResponse = await _validTokenClient.RequestClientCredentialsAsync("api1");

            // the token expiration time is 3 seconds, configured in SampleAuthServer
            await Task.Delay(4000);

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        private static async Task<HttpResponseMessage> SendRequest(string jwtToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, AirbagUrl);
            request.SetBearerToken(jwtToken);
            var result = await new HttpClient().SendAsync(request);
            return result;
        }
    }
}