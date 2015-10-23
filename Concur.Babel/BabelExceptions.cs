using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Concur.Babel.Models;

namespace Concur.Babel
{
	[Serializable]
	public class BabelException : Exception, IBabelException //	500	Thrown for any unexpected errors.	 
	{
		public BabelException(string message, Exception innerException = null)
			: base(message, innerException)
		{
		}


		public static BabelException FromServiceError(ServiceError error, ErrorKind kind) 
		{
			BabelException res;
			switch(kind)
			{
				//case ErrorKind.InvalidArgument: res = new BabelIllegalArgException(error.Details, error.Inner == null ? null : FromServiceError(error.Inner)); break;
				case ErrorKind.InvalidRequest: res = new BabelApplicationException(error.Details, error.Inner == null ? null : FromServiceError(error.Inner, ErrorKind.Unknown)); break; //Inner errors are unknown kind
				default: res = new BabelException(error.Details, error.Inner == null ? null : FromServiceError(error.Inner, ErrorKind.Unknown)); break; //Inner errors are unknown kind
			}
			if(error.Context != null)
			{
				foreach(var kv in error.Context)
				{
					res.Context.Add(kv.Key, kv.Value);
				}
			}
			if(error.Errors != null)
			{
				foreach(var item in error.Errors)
				{
					res.Errors.Add(item);
				}
			}
			return res;
		}
		
		protected BabelException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
			
			string contextStr = info.GetString("Context");
			var ser = new BabelJsonSerializer();
			using(var ms = new System.IO.MemoryStream((new UTF8Encoding(false)).GetBytes(contextStr)))
			{
				m_context = ser.Deserialize<Dictionary<string, Dictionary<string, string>>>(ms);
			}
			
			int cnt = info.GetInt32("ErrorsCnt");
			for(int i = 0; i < cnt; i++)
			{
				string n = i.ToString();
				var err = new Error { Code = info.GetString("ECode" + n), Message = info.GetString("EMessage" + n) };
				int parmCnt = info.GetInt32("EParmsCnt");
				err.Params = new List<string>();
				for(int pidx = 0; pidx < parmCnt; pidx++)
				{
					err.Params.Add(info.GetString("EParm" + n + "-" + pidx));
				}
			}
		}

		[System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			if(info == null) throw new ArgumentNullException("info");

			var ser = new BabelJsonSerializer();
			using(var strm = ser.Serialize(Context))
			{
				info.AddValue("Context", strm.GetString());
			} 
			
			info.AddValue("ErrorsCnt", Errors.Count);
			for(int j = 0; j < Errors.Count; j++)
			{
				string n = j.ToString();
				var err = Errors[j];
				info.AddValue("ECode" + n, err.Code);
				info.AddValue("EMessage" + n, err.Message);
				info.AddValue("EParmsCnt", err.Params.Count);
				for(int pidx = 0; pidx < err.Params.Count; pidx++)
				{
					info.AddValue("EParm" + n + "-" + pidx, err.Params[pidx]);
				}
			}
			base.GetObjectData(info, context);
		}

		public Dictionary<string, string> GetContextCategory(string key)
		{
			Dictionary<string, string> res;
			if(Context.TryGetValue(key, out res))
			{
				res = new Dictionary<string, string>();
				Context[key] = res;
			}

			return res;
		}

		#region IBabelException Members
		public Dictionary<string, Dictionary<string, string>> Context
		{
			get { return m_context; }
		}
		private Dictionary<string, Dictionary<string, string>> m_context = new Dictionary<string, Dictionary<string, string>>();

		public IList<Error> Errors
		{
			get { return m_errors;}
		}
		private List<Error> m_errors = new List<Error>();

		public virtual ErrorKind Kind
		{
			get { return m_kind; }
		}
		protected ErrorKind m_kind = ErrorKind.Unknown;

		public void AddContext(string key, string subKey, string value)
		{
			var cat = GetContextCategory(key);
			cat[subKey] = value;
		}


		public void AddContext(string key, string value)
		{
			AddContext(key, "value", value);
		}

		public void AddContext(string key, IDictionary<string, string> values)
		{
			if (values == null) return;
			var d = GetContextCategory(key);
			foreach(var kv in values)
			{
				d[kv.Key] = kv.Value;
			}
		}
		#endregion
	}

	public class BabelApplicationException : BabelException
	{
		public BabelApplicationException(string message, Exception innerException = null)
			: base(message, innerException)
		{
			m_kind = ErrorKind.InvalidRequest;
		}

		protected BabelApplicationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context) 
		{
			m_kind = ErrorKind.InvalidRequest; 
		}
	}

	/*public class BabelIllegalArgException : BabelException
	{
		public BabelIllegalArgException(string message, Exception innerException = null)
			: base(message, innerException) 
		{ 
			m_kind = ErrorKind.InvalidArgument; 
		}

		protected BabelIllegalArgException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context) 
		{
			m_kind = ErrorKind.InvalidArgument;
		}
	}
	 */

	public class BabelValidionException : BabelApplicationException
	{
		public BabelValidionException(IEnumerable<ValidationResult> vResults)
			: base(string.Join("\r\n", vResults.Select(r => r.ErrorMessage)))
		{
			foreach(var r in vResults)
			{
				var e = new Error { Code = "VALIDATION_ERROR", Message = r.ErrorMessage };
				e.Params.Add(string.Join(", ",r.MemberNames));
				this.Errors.Add(e);
			}
		}
	}
}

