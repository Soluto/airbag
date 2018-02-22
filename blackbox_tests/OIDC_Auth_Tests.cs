using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Configuration;
using System.Net;
using IdentityModel.Client;
using System.Text;
using Newtonsoft.Json;

namespace BlackboxTests
{
    // these tests assume they run in a docker-compose environment
    // they assume that other than the tests container, the other containers also running are airbag and nginx
    // see the blackbox_tests/docker-compose file for details
    public class OIDC_Auth_Tests
    {
        private static DiscoveryResponse validDiscovery;
        private static DiscoveryResponse invalidDiscovery;
        private static TokenClient validTokenClient;
        private static TokenClient invalidTokenClient;
        private const string airbagUrl = "http://localhost:5001/";

        public OIDC_Auth_Tests()
        {
            invalidDiscovery = DiscoveryClient.GetAsync("http://localhost:5004").Result;
            invalidTokenClient = new TokenClient(invalidDiscovery.TokenEndpoint, "client", "secret");

            validDiscovery = DiscoveryClient.GetAsync("http://localhost:5002").Result;
            validTokenClient = new TokenClient(validDiscovery.TokenEndpoint, "client", "secret");
        }

        [Fact]
        public async Task RequestWithValidToken_ForwardRequestToBackendContainer()
        {
            var tokenResponse = await validTokenClient.RequestClientCredentialsAsync("api1");

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithoutAuthorizationHeader_RouteIsWhitelisted_ForwardRequestToBackendContainer()
        {
            var result = await new HttpClient().GetAsync(airbagUrl + "isAlive");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithoutAuthorizationHeader_RouteIsNotWhitelisted_Return403Forbidden()
        {
            var result = await new HttpClient().GetAsync(airbagUrl);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithModifiedJwtToken_Return403Forbidden()
        {
            var tokenResponse = await validTokenClient.RequestClientCredentialsAsync("api1");
            var arr = tokenResponse.AccessToken.ToCharArray();
            arr[20] = 'g';
            var temperedToken = arr.ToString();

            HttpResponseMessage result = await SendRequest(temperedToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithWrongIssuer_Return403Forbidden()
        {
            var tokenResponse = await invalidTokenClient.RequestClientCredentialsAsync("api1");

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithWrongAudeince_Return403Forbidden()
        {
            var tokenResponse = await invalidTokenClient.RequestClientCredentialsAsync("api2");

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task RequestWithExpiredToken_Return403Forbidden()
        {
            var tokenResponse = await validTokenClient.RequestClientCredentialsAsync("api1");

            // the token expiration time is 3 seconds, configured in sample_auth_server
            await Task.Delay(4000);

            var result = await SendRequest(tokenResponse.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        private static async Task<HttpResponseMessage> SendRequest(string jwtToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, airbagUrl);
            request.SetBearerToken(jwtToken);
            var result = await new HttpClient().SendAsync(request);
            return result;
        }
    }
}
