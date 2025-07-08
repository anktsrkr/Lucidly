using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucidly.Common
{
    public class ToolDetails
    {
        public string PluginName { get; set; }
        public string FunctionName { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object?>? FunctionArgs { get; set; }
        public object? Result { get; set; }
    }
}
