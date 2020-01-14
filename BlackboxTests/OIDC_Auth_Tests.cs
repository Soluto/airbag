using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Xunit;

namespace BlackboxTests
{
  public class OidcAuthTests
  {
    private string AirbagUrl = System.Environment.GetEnvironmentVariable("AIRBAG_URL");
    private string ValidAuthServerUrl = System.Environment.GetEnvironmentVariable("VALID_AUTH_SERVER_URL");
    private string AnotherValidAuthServerUrl = System.Environment.GetEnvironmentVariable("ANOTHER_VALID_AUTH_SERVER_URL");
    private string AuthServerOtherIssuerUrl = System.Environment.GetEnvironmentVariable("AUTH_SERVER_DIFFERENT_ISSUER_URL");
    private string AuthServerOtherSignatureUrl = System.Environment.GetEnvironmentVariable("AUTH_SERVER_DIFFERENT_SIGNATURE_URL");
    private string AirbagWithoutAudUrl = System.Environment.GetEnvironmentVariable("AIRBAG_WITHOUT_AUD_URL");

    public OidcAuthTests()
    {
    }

    private async Task<string> GetToken(string authority, string scope) {
      var client = new HttpClient();
      var response = await client.RequestTokenAsync(new TokenRequest
      {
        Address = authority + "/connect/token",
        GrantType = OidcConstants.GrantTypes.ClientCredentials,
        ClientId = "client",
        ClientSecret = "secret",

        Parameters =
        {
          {"scope", scope}
        }
      });
      return response.AccessToken;
    }

    private async Task<HttpResponseMessage> SendRequestWithAuth(string authority, string scope, string url = null)
    {
      var token = await GetToken(authority, scope);
      return await SendRequest(token, url?? AirbagUrl);
    }

    [Fact]
    public async Task RequestWithValidToken_ForwardRequestToBackendContainer()
    {
      var result = await SendRequestWithAuth(ValidAuthServerUrl, "api1");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithInvalidAudience_Return403Forbidden()
    {
      var result = await SendRequestWithAuth(ValidAuthServerUrl, "api2");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithInvalidAudience_AnotherProvider_Return403Forbidden()
    {
      var result = await SendRequestWithAuth(AnotherValidAuthServerUrl, "api2");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithInvalidAudience_AirbagIgnoreAudience_ForwardRequestToBackendContainer()
    {
      var result = await SendRequestWithAuth(ValidAuthServerUrl, "api2", AirbagWithoutAudUrl);
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithValidTokenFromAnotherProvider_ForwardRequestToBackendContainer()
    {
      var result = await SendRequestWithAuth(AnotherValidAuthServerUrl, "api1");
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
      var token = await GetToken(ValidAuthServerUrl, "api1");
      var arr = token.ToCharArray();
      arr[20] = 'g';
      var temperedToken = arr.ToString();

      var result = await SendRequest(temperedToken, AirbagUrl);
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithWrongIssuer_Return403Forbidden()
    {
      var result = await SendRequestWithAuth(AuthServerOtherIssuerUrl, "api1");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }


    [Fact]
    public async Task RequestWithWrongSignature_Return403Forbidden()
    {
      var result = await SendRequestWithAuth(AuthServerOtherSignatureUrl, "api1");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithWrongAudience_Return403Forbidden()
    {
      var result = await SendRequestWithAuth(AuthServerOtherIssuerUrl, "api2");
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task RequestWithExpiredToken_Return403Forbidden()
    {
      var token = await GetToken(ValidAuthServerUrl, "api1");

      // the token expiration time is 3 seconds, configured in SampleAuthServer
      await Task.Delay(4000);

      var result = await SendRequest(token, AirbagUrl);
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