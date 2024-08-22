using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Zwo.Launcher.Utils
{
    class ProxyManager
    {
        public static HttpClient GetHttpClient()
        {
            ConfigurationManager.LoadConfiguration();
            var handler = new HttpClientHandler();
            if (ConfigurationManager.Config.ProxySettings.IsEnabled)
            {
                handler.UseProxy = true;
                if (ConfigurationManager.Config.ProxySettings.IsUseSystemSettings)
                {
                    handler.Proxy = WebRequest.GetSystemWebProxy();
                }
                else
                {
                    handler.Proxy = new WebProxy(ConfigurationManager.Config.ProxySettings.ProxyServerAddress);
                }
            }
            else
            {
                handler.UseProxy = false;
            }

            return new HttpClient(handler);
        }
    }
}
