using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lucidly.API.Utils
{
    public class OAuthHandler
    {
        private readonly IJSRuntime _js;
        private readonly OAuthFlowService _flowService;

        public OAuthHandler( IJSRuntime JS
, OAuthFlowService flowService)
        {
            _js = JS;
            _flowService = flowService;
        }

        public Task<string?> HandleAuthorizationUrlAsync(Uri authorizationUrl, Uri redirectUri, CancellationToken cancellationToken)
        {   
            var token = _flowService.StartFlow();
            var authorizationUrlWithToken = authorizationUrl + "&state=" + token;

            // Navigate the user to the OAuth provider
            _js.InvokeAsync<object>("open", authorizationUrlWithToken, "_blank");

            // Wait for the code to be set by the callback page
            return _flowService.WaitForCodeAsync(token);
        }
    }
}
