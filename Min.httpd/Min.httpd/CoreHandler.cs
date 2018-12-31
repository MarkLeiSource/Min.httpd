using Min.Common;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Min.httpd
{
    public class CoreHandler
    {
        public Assembly CoreAssembly { get; set; }
        public List<Type> ControllerTypes { get; set; }
        public CoreHandler()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "ServerCore.dll");
            Console.WriteLine("current server core path is: " + path);
            CoreAssembly = Assembly.LoadFile(path);
            var types = CoreAssembly.GetTypes();
            var appType = types.Where(t => t.GetInterfaces().Contains(typeof(IApp))).First();
            var app = Activator.CreateInstance(appType);
            MethodInfo startMethod = appType.GetMethod("Start");
            //MethodInfo stopMethod = appType.GetMethod("Stop");
            startMethod.Invoke(app, null);

            ControllerTypes = types.Where(t => t.BaseType.Equals(typeof(RestController))).ToList();
        }

        public bool TryGetData(Context context, out string json)
        {
            try
            {
                var uri = context.Header["uri"];
                var splits = uri.Split("/", StringSplitOptions.RemoveEmptyEntries);
                string controllerName = splits[0];
                string actionName;
                if (splits.Count() >= 2)
                {
                    actionName = splits[1];
                }
                else
                {
                    var method = context.Header.ContainsKey("method") ? context.Header["method"] : "Index";
                    actionName = method.Substring(0, 1).ToUpper() + method.Substring(1).ToLower();
                }
                var controllerType = ControllerTypes.FirstOrDefault(t => t.Name.Replace("Controller", string.Empty).Equals(controllerName, StringComparison.CurrentCultureIgnoreCase));
                if (controllerType != null)
                {
                    var controller = Activator.CreateInstance(controllerType);
                    MethodInfo method = controllerType.GetMethod(actionName);
                    if (method != null)
                    {
                        var obj = method.Invoke(controller, new object[] { context });
                        json = obj.ToString();
                        return true;
                    }
                    else
                    {
                        json = null;
                        return false;
                    }
                }
                else
                {
                    json = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                json = null;
                return false;
            }
        }
    }
}
