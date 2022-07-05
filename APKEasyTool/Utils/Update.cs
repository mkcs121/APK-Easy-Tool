using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace APKEasyTool
{
    public class Updates
    {
        public static string RemoteVersion()
        {
            if (NetworkInterface.GetIsNetworkAvailable() == true)
            {
                try
                {
                    string getupdatedata;
                    WebClient wc = new WebClient();

                    getupdatedata = wc.DownloadString("https://evildog1.bitbucket.io/apkeasytool/aet1.txt");

                    if (!String.IsNullOrEmpty(getupdatedata))
                    {
                        return getupdatedata;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    return "ERROR";
                }
            }
            return null;
        }
    }
}
