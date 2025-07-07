using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Lucidly.UI.McpServer.Tools
{
    [McpServerToolType]
    public class EchoTool(IHttpContextAccessor contextAccessor)
    {
        private readonly IHttpContextAccessor _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));


        [McpServerTool, Description("Echoes the message back to the client.")]
        public string Echo(string message)
        {
            return $"hello {message}, {_contextAccessor.HttpContext?.Request.Path ?? "(no HttpContext)"}";
        }

        [McpServerTool, Description("Gets the current weather for the specified city and specified date time.")]
        public static string GetWeatherForCity(string cityName, string currentDateTimeInUtc)
        {
            return cityName switch
            {
                "Boston" => "61 and rainy",
                "London" => "55 and cloudy",
                "Miami" => "80 and sunny",
                "Paris" => "60 and rainy",
                "Tokyo" => "50 and sunny",
                "Sydney" => "75 and sunny",
                "Tel Aviv" => "80 and sunny",
                _ => "31 and snowing",
            };
        }
        [McpServerTool, Description("Retrieves the current date time in UTC.")]
        public static string GetCurrentDateTimeInUtc()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
