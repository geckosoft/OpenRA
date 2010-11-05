using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;

namespace OpenRA.Support
{
	public static class HttpClient
	{
		public static string Post(string url, NameValueCollection formData)
		{
			try
			{
				var webClient = new WebClient();

				return Encoding.UTF8.GetString(webClient.UploadValues(url, "POST", formData));
			}
			catch (Exception ex)
			{
				Console.WriteLine("An exception occured: " + ex);
				return null;
			}
		}

		public static string Get(string url)
		{
			try
			{
				var webClient = new WebClient();

				return Encoding.UTF8.GetString(webClient.DownloadData(url));
			}
			catch (Exception ex)
			{
				Console.WriteLine("An exception occured: " + ex);
				return null;
			}
		}
	}
}
