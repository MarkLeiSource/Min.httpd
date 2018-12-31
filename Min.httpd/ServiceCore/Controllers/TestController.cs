using Min.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCore.Controllers
{
    public class TestController : RestController
    {
        public string GetTestInfo(Context context)
        {
            return "{\"msg\":\"Hello, pretty new world.\"}";
        }
    }
}
