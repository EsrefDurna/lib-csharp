// <auto-generated />
// AUTO-GENERATED FILE - DO NOT MODIFY
// Generated from demo.babel

using System;
using System.Collections.Generic;
using BabelRpc;

namespace BabelRpc.Demo
{ 
	/// <summary>
	///  The LogControl service allows you to fetch information about a log, set the logging status, or
	///  see the logging status for all logs
	/// </summary>
	[System.CodeDom.Compiler.GeneratedCode("Babel", "")]
	public partial class LogControlController : BabelRpc.Mvc.BabelController<ILogControlAsync>
	{ 
		class GetLogStatusRequest : BabelRpc.Mvc.IBabelRequest
		{ 
			/// <summary>
			///  The name of the log
			/// </summary>
			public string LogName;

			#region IBabelRequest
			public void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
			{
				if(runOnAll) LogName = (string) method("logName", typeof(string), LogName, auxData);
			}

			public bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
			{
				switch(name)
				{
					case "logName": LogName = (string) method("logName", typeof(string), LogName, auxData); return true;
					default: return false;
				}
			}

			public void SetDefaults()
			{
			}
			#endregion
		}

		/// <summary>
		/// Gets the status of a single log
		/// </summary>
		/// <param name="logName"> The name of the log</param>
		public System.Threading.Tasks.Task<State?> GetLogStatus()
		{
			var requestData = DeserializeRequest<GetLogStatusRequest>();
			return m_businessLogic.GetLogStatusAsync(requestData.LogName);
		}

		class SetLogStatusRequest : BabelRpc.Mvc.IBabelRequest
		{ 
			/// <summary>
			///  Name of the log
			/// </summary>
			public string LogName;

			/// <summary>
			///  state to assign
			/// </summary>
			public State? State;

			#region IBabelRequest
			public void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
			{
				if(runOnAll) LogName = (string) method("logName", typeof(string), LogName, auxData);
				if(runOnAll) State = (State?) method("state", typeof(State?), State, auxData);
			}

			public bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
			{
				switch(name)
				{
					case "logName": LogName = (string) method("logName", typeof(string), LogName, auxData); return true;
					case "state": State = (State?) method("state", typeof(State?), State, auxData); return true;
					default: return false;
				}
			}

			public void SetDefaults()
			{
			}
			#endregion
		}

		/// <summary>
		/// Sets the status for a single log
		/// </summary>
		/// <param name="logName"> Name of the log</param>
		/// <param name="state"> state to assign</param>
		public System.Threading.Tasks.Task SetLogStatus()
		{
			var requestData = DeserializeRequest<SetLogStatusRequest>();
			return m_businessLogic.SetLogStatusAsync(requestData.LogName, requestData.State);
		}

		class SetMultiRequest : BabelRpc.Mvc.IBabelRequest
		{ 
			/// <summary>
			///  data structure containing map of statuses to set
			/// </summary>
			public Info Data;

			public int? LogLevel;

			#region IBabelRequest
			public void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
			{
				Data = (Info) method("data", typeof(Info), Data, auxData);
				if(runOnAll) LogLevel = (int?) method("logLevel", typeof(int?), LogLevel, auxData);
			}

			public bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
			{
				switch(name)
				{
					case "data": Data = (Info) method("data", typeof(Info), Data, auxData); return true;
					case "logLevel": LogLevel = (int?) method("logLevel", typeof(int?), LogLevel, auxData); return true;
					default: return false;
				}
			}

			public void SetDefaults()
			{
				if (LogLevel == null) LogLevel = 1;
			}
			#endregion
		}

		/// <summary>
		/// Set multiple statuses at once.
		/// </summary>
		/// <param name="data"> data structure containing map of statuses to set</param>
		/// <param name="logLevel"></param>
		public System.Threading.Tasks.Task<Info> SetMulti()
		{
			var requestData = DeserializeRequest<SetMultiRequest>();
			return m_businessLogic.SetMultiAsync(requestData.Data, requestData.LogLevel);
		}


		/// <summary>
		/// Returns the status of all logs
		/// </summary>
		public System.Threading.Tasks.Task<Info> GetLoggingStatus()
		{
			return m_businessLogic.GetLoggingStatusAsync();
		}

		class AddJokeRequest : BabelRpc.Mvc.IBabelRequest
		{ 
			/// <summary>
			///  A nice work-friendly joke
			/// </summary>
			public Joke Joke;

			#region IBabelRequest
			public void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
			{
				Joke = (Joke) method("joke", typeof(Joke), Joke, auxData);
			}

			public bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
			{
				switch(name)
				{
					case "joke": Joke = (Joke) method("joke", typeof(Joke), Joke, auxData); return true;
					default: return false;
				}
			}

			public void SetDefaults()
			{
			}
			#endregion
		}

		/// <summary>
		/// Add a joke
		/// </summary>
		/// <param name="joke"> A nice work-friendly joke</param>
		public System.Threading.Tasks.Task AddJoke()
		{
			var requestData = DeserializeRequest<AddJokeRequest>();
			return m_businessLogic.AddJokeAsync(requestData.Joke);
		}
	}
}
