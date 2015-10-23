using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Concur.Babel.Test
{
	public class TestTracer : ITracer
	{
		List<string> m_messages = new List<string>();
		int m_level = 0;

		public void Reset()
		{
			m_messages.Clear();
			m_level = 0;
		}

		public string[] GetEntries()
		{
			return m_messages.ToArray();
		}

		#region ITracer Members
		
		public void Log(string message)
		{
			m_messages.Add(string.Format("{0:HH:mm:ss.fff}:{2}{1}", DateTime.Now, message, new string('\t', m_level))); 
		}

		public ITraceScope Start(string eventName)
		{
			Log(">> " + eventName);
			m_level++;
			return new TestTracerScope(this);
		}

		internal void ScopeEnds()
		{
			m_level--;
			if(m_level < 0) m_level = 0;
		}

		#endregion
	}

	public class TestTracerScope : ITraceScope
	{
		public TestTracerScope(TestTracer tracer)
		{
			m_tracer = tracer;
		}

		private TestTracer m_tracer;

		#region ITraceScope Members

		public void End()
		{
			m_tracer.ScopeEnds();
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			End();
		}

		#endregion
	}
}
