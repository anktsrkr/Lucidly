namespace Lucidly.UI.Utils;

public enum ChatModeEnum
{
    Solo,
    Collaborative,
}
public enum MCPServerMode
{
  STDIO,
  HTTP
}


public record McpTool(
    string Name,
    string Description
);