using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucidly.Common.Models
{
    public class AdditionalParameters
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class McpClientConfigModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public MCPTransportMode MCPTransportMode { get; set; } = MCPTransportMode.Sse;

        [Required]
        public McpClientRegistrationMode McpClientRegistrationMode { get; set; } = McpClientRegistrationMode.Dynamic;

        [Required]
        public McpClientAuthenticationMode McpClientAuthenticationMode { get; set; } = McpClientAuthenticationMode.ApiKey;

        public List<AdditionalParameters> AdditionalAuthorizationParameters { get; set; } = [];
        public List<McpTool> Tools { get; set; } = [];
        public string? AccessToken { get; set; } = string.Empty;


        [Required] public string ClientId { get; set; }
        [Required] public string ClientSecrect { get; set; }

        [Required] public string ApiKeyName { get; set; }
        [Required] public string ApiKeyValue { get; set; }

    }

}
