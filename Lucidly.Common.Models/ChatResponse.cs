namespace Lucidly.Common.Models;

public record ChatBubble(string BubbleId, string User, string Message, string Type  = "chat", PendingFunctionCall PendingFunctionCall=null);

