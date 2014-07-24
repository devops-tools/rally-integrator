using System;
using System.Net;
using System.Threading;

namespace RallyIntegrator.Library
{
    public static class WebHelper
    {
        public static string DownloadString(string url, string username, string password)
        {
            var attmptCount = 0;
            while (attmptCount <= 3)
            {
                if(attmptCount > 0)
                    Thread.Sleep(2000);
                try
                {
                    var webClient = (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        ? new WebClient {Credentials = new NetworkCredential(username, password)}
                        : new WebClient();
                    return webClient.DownloadString(url);
                }
                catch (Exception ex)
                {
                    if (attmptCount == 3)
                        throw;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to download data from {0}.", url);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    var message = ex.Message;
                    if (ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message))
                        message = string.Concat(message, " ", ex.InnerException.Message);
                    Console.WriteLine(message);
                    Console.ResetColor();
                    Console.WriteLine("Retrying...");
                }
                finally
                {
                    attmptCount++;
                }
            }
            return null;
        }
    }
}
