using System;
using System.Collections.Generic;
using System.Text;

namespace Min.Common
{
    public class Context
    {
        public Dictionary<string, string> Header { get; set; }
        public Dictionary<string, string> Parms { get; set; }
        public Dictionary<string, string> Files { get; set; }
        public Context(Dictionary<string, string> header,
            Dictionary<string, string> parms,
            Dictionary<string, string> files)
        {
            this.Header = header;
            this.Parms = parms;
            this.Files = files;
        }
    }
}
