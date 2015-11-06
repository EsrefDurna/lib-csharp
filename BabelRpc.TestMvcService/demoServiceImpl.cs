using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;


namespace BabelRpc.Demo
{
	public partial class LogControlController 
	{
		protected override ILogControlAsync InitBusinessLogic()
		{
			string foo = this.Request.Headers["Foo"];
			string requestId = this.Request.Headers["RequestId"];

			string shouldError = this.Request.Headers["ShouldError"];
			switch(shouldError)
			{
				case "1": throw new ArgumentNullException("userId");
				case "2": throw new ArgumentException("This is ArgumentException");
				case "3": throw new ApplicationException("This is ApplicationException", new KeyNotFoundException("Can't find something"));
				case "4": throw new System.DivideByZeroException();
				case "5": 
					throw new BabelRpc.Mvc.ServiceApplicationException("TEST_EXCEPTION", "Test {0} {1}", "exception", "1");
				case "6":
					throw new BabelRpc.Mvc.ServiceApplicationException(
						new BabelRpc.ErrorEx("TEST_ERROR", "This is a test {0} {1}", "error", "1"),
						new BabelRpc.ErrorEx("TEST_ERROR", "This is a test {0} {1}", "error", "2")
					);
			}

			if (foo != null) foo = Uri.UnescapeDataString(foo);
			if (requestId != null) requestId = Uri.UnescapeDataString(requestId);
			return new DemoServiceImpl(foo, requestId);
		}
	}

	public class DemoServiceImpl : ILogControlAsync
	{
		private static Dictionary<string, State?> m_dict;
		private static List<Joke> m_jokes;
		private static Joke s_status;
		
		static DemoServiceImpl()
		{
			m_dict = new Dictionary<string, State?>();
			m_jokes = new List<Joke>();
			m_jokes.Add(new Joke() { Question = "Why did the chicken code in VBScript?", Answer = "His head was cut off.", DateAdded = DateTime.Now });
			m_jokes.Add(new Joke() { Question = "How many VBScript programmers does it take to screw in a light bulb?", Answer = "Three. One IsObject, one IsArray, and one to figure out the difference.", DateAdded = DateTime.Now });
			m_jokes.Add(new Joke() { Question = "What is the keyword \"set\" for?", Answer = "The code wasn't unreliable enough without the extra keyword.", DateAdded = DateTime.Now });
			s_status = new Joke() { Question = "Status?", Answer = "Foo: RequrestId: ", DateAdded = DateTime.Now };
			m_jokes.Add(s_status);
		}

		public DemoServiceImpl(string foo, string requestId)
		{
			s_status.Answer = string.Format("Foo: {0}; RequrestId: {1}", foo, requestId);
		}

		#region ILogControl Members
		public Task<State?> GetLogStatusAsync(string logName)
		{
			if(!m_dict.ContainsKey(logName))
			{
				Task.FromResult(State.OFF);
			}
			return Task.FromResult(m_dict[logName]);
		}

		public Task SetLogStatusAsync(string logName, State? state)
		{
			if (state == null)
				m_dict.Remove(logName);
			else
				m_dict[logName] = state;
			return Task.Run(() => { });
		}

		public Task<Info> GetLoggingStatusAsync()
		{
			Info result = new Info();
			foreach (var p in m_dict)
			{
				result.Status.Add(p.Key, p.Value);
			}
			result.Jokes = m_jokes;
			return Task.FromResult(result);
		}

		public Task<Info> SetMultiAsync(Info data, int? logLevel)
		{
			if (data != null && data.Status != null)
			{
				foreach (var i in data.Status)
				{
					if (i.Value == null)
						m_dict.Remove(i.Key);
					else
						m_dict[i.Key] = i.Value;
				}
			}
			return GetLoggingStatusAsync().ContinueWith<Info>(t => {
				var infoT = t.Result;
				infoT.LogLevel = logLevel;
				return infoT;
			});
		}

		public Task AddJokeAsync(Joke joke)
		{
			return Task.Run(() => {m_jokes.Add(joke); });
		}
		#endregion
	}
}