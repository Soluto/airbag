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
    private string AirbagUrl = System.Environment.GetEnvironmentVariable("AIRBAG_URL");
    // private string AirbagUrl = "http://localhost:5001/";
    private string ValidAuthServerUrl = System.Environment.GetEnvironmentVariable("VALID_AUTH_SERVER_URL");
    // private string ValidAuthServerUrl = "http://localhost:5002/";
    private string AnotherValidAuthServerUrl = System.Environment.GetEnvironmentVariable("ANOTHER_VALID_AUTH_SERVER_URL");
    // private string AnotherValidAuthServerUrl = "http://localhost:5003/";
    private string AuthServerOtherIssuerUrl = System.Environment.GetEnvironmentVariable("AUTH_SERVER_DIFFERENT_ISSUER_URL");
    // private string AuthServerOtherIssuerUrl = "http://localhost:5004/";
    private string AuthServerOtherSignatureUrl = System.Environment.GetEnvironmentVariable("AUTH_SERVER_DIFFERENT_SIGNATURE_URL");
    // private string AuthServerOtherSignatureUrl = "http://localhost:5005/";
    private string AirbagWithoutAudUrl = System.Environment.GetEnvironmentVariable("AIRBAG_WITHOUT_AUD_URL");
    // private string AirbagWithoutAudUrl = "http://localhost:5006/";

    public OidcAuthTests()
    {
    }

    private async Task<TokenClient> GetTokenClient(string authority)
    {
      var discovery = DiscoveryClient.GetAsync(authority).Result;
      return new TokenClient(discovery.TokenEndpoint, "client", "secret");
    }

    private async Task<HttpResponseMessage> SendToAuthWithScope(string authority, string scope, string url = null)
    {
      var tokenClient = await GetTokenClient(authority);
      var tokenResponse = await tokenClient.RequestClientCredentialsAsync(scope);

      return await SendRequest(tokenResponse.AccessToken, url?? AirbagUrl);
    }

    [Fact]
    public async Task RequestWithValidToken_ForwardRequestToBackendContainer()
    {
      var result = await SendToAuthWithScope(ValidAuthServerUrl, "api1");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithInvalidAudience_Return403Forbidden()
    {
      var result = await SendToAuthWithScope(ValidAuthServerUrl, "api2");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithInvalidAudience_AnotherProvider_Return403Forbidden()
    {
      var result = await SendToAuthWithScope(AnotherValidAuthServerUrl, "api2");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithInvalidAudience_AirbagIgnoreAudience_ForwardRequestToBackendContainer()
    {
      var result = await SendToAuthWithScope(ValidAuthServerUrl, "api2", AirbagWithoutAudUrl);
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithValidTokenFromAnotherProvider_ForwardRequestToBackendContainer()
    {
      var result = await SendToAuthWithScope(AnotherValidAuthServerUrl, "api1");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithoutAuthorizationHeader_RouteIsWhitelisted_ForwardRequestToBackendContainer()
    {
      var result = await new HttpClient().GetAsync(AirbagUrl + "/isAlive");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task
        RequestWithoutAuthorizationHeader_RouteIsWhitelistedWithWildCard_ForwardRequestToBackendContainer()
    {
      var result = await new HttpClient().GetAsync(AirbagUrl + "/foo/bar");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task
        RequestWithoutAuthorizationHeader_RouteIsNotWhitelistedButContainsPartialWildcard_Return403Forbidden()
    {
      var result = await new HttpClient().GetAsync(AirbagUrl + "/api/foo/bar");
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
      var tokenClient = await GetTokenClient(ValidAuthServerUrl);
      var tokenResponse = await tokenClient.RequestClientCredentialsAsync("api1");
      var arr = tokenResponse.AccessToken.ToCharArray();
      arr[20] = 'g';
      var temperedToken = arr.ToString();

      var result = await SendRequest(temperedToken, AirbagUrl);
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithWrongIssuer_Return403Forbidden()
    {
      var result = await SendToAuthWithScope(AuthServerOtherIssuerUrl, "api1");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }


    [Fact]
    public async Task RequestWithWrongSignature_Return403Forbidden()
    {
      var result = await SendToAuthWithScope(AuthServerOtherSignatureUrl, "api1");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithWrongAudience_Return403Forbidden()
    {
      var result = await SendToAuthWithScope(AuthServerOtherIssuerUrl, "api2");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithExpiredToken_Return403Forbidden()
    {
      var tokenClient = await GetTokenClient(ValidAuthServerUrl);
      var tokenResponse = await tokenClient.RequestClientCredentialsAsync("api1");

      // the token expiration time is 3 seconds, configured in SampleAuthServer
      await Task.Delay(4000);

      var result = await SendRequest(tokenResponse.AccessToken, AirbagUrl);
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendRequest(string jwtToken, string url)
    {
      var request = new HttpRequestMessage(HttpMethod.Get, url);
      request.SetBearerToken(jwtToken);
      var result = await new HttpClient().SendAsync(request);
      return result;
    }
  }
}