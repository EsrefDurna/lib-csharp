using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Concur.Babel;
using Concur.Babel.Models;

namespace Concur.Babel.Mvc
{
	public class ControllerHelper
	{
		public static BabelActionResult AutoSerializeResponse(object actionReturnValue, System.Collections.Specialized.NameValueCollection headers)
		{
			if(headers == null)
			{
				throw new ArgumentNullException("headers");
			}

			string accept = headers["Accept"];
			if(string.IsNullOrWhiteSpace(accept))
			{
				accept = headers["Content-Type"];
			}

			if(ParseAccepted(accept, "json", "xml") == "xml")
			{
				return new BabelXmlResult(actionReturnValue);
			}
			else
			{
				return new BabelJsonResult(actionReturnValue);
			}
		}

		public static string ParseAccepted(string requested, params string [] canProduce)
		{
			if(string.IsNullOrWhiteSpace(requested))
			{
				return canProduce[0];
			}

			double maxQ = 0;
			int maxQCPIdx = 0;
			foreach(var token in requested.ToLowerInvariant().Split(','))
			{
				string [] stokens = token.Split(';');
				double q = 1;
				string acceptType = stokens[0];
				for(int cpIdx = 0; cpIdx < canProduce.Length; cpIdx ++)
				{
					if(acceptType.Contains(canProduce[cpIdx]))
					{
						for(int i = 1; i < stokens.Length; i++)
						{
							string[] sstoken = stokens[i].Split('=');
							if(sstoken.Length == 2 && sstoken[0].Trim().ToLowerInvariant() == "q")
							{
								if(!double.TryParse(sstoken[1], out q))
								{
									q = 1;
								}
							}
						}
						if(q == 1.0) return canProduce[cpIdx];
						if(q > maxQ)
						{
							maxQCPIdx = cpIdx;
							maxQ = q;
						}
					}
				}
			}
			return canProduce[maxQCPIdx];
		}

		public static IErrorLogger ErrorLogger;
	}
}
