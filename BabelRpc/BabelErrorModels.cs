using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BabelRpc
{
	public interface IErrorLogger
	{
		string LogError(Exception err);
	}

	public interface IBabelException
	{
		Dictionary<string, Dictionary<string, string>> Context { get; }
		void AddContext(string key, string subKey, string value);
		void AddContext(string key, string value);
		void AddContext(string key, IDictionary<string, string> values);
		IList<Error> Errors { get; }
		ErrorKind Kind { get; }
	}

	public enum ErrorKind { Unknown, /*InvalidArgument,*/ InvalidRequest }

	/// <summary>
	/// Class to facilitate create of Error object
	/// </summary>
	public class ErrorEx : Error
	{
		public ErrorEx() { }

		/// <summary>
		/// Creates error info
		/// </summary>
		/// <param name="code">The service-specific error code. 
		/// May be used by a localization system to generate error message</param>
		/// <param name="message">The text of the error in US-English</param>
		/// <param name="parameters">he list of parameters to the error message. 
		/// This could be used by localization systems to generate messages based on the error code.</param>
		public ErrorEx(string code, string message, params	string[] parameters ) 
		{ 
			Code = code;
			Message = (parameters == null || parameters.Length == 0) ? message : string.Format(message, parameters);
			if (parameters != null) Params.AddRange(parameters);
		}

		/// <summary>
		/// Creates error info
		/// </summary>
		/// <param name="code">The service-specific error code. 
		/// May be used by a localization system to generate error message</param>
		/// <param name="message">The text of the error in US-English</param>
		/// <param name="parameters">he list of parameters to the error message. 
		/// This could be used by localization systems to generate messages based on the error code.</param>
		public ErrorEx(string code, string message, IEnumerable<string> parameters)
		{
			Code = code;
			Message = (parameters == null || !parameters.Any()) ? message : string.Format(message, parameters.ToArray());
			if (parameters != null) Params.AddRange(parameters);
		}

	}

	/// <summary>
	/// Delegate type to perform custom error translation
	/// </summary>
	/// <param name="error">All the exceptions that service can throw</param>
	/// <param name="errorKind">Should return the exception category</param>
	/// <returns>ServiceError structure that will be sent to client</returns>
	public delegate ServiceError ServiceErrorTranslationDelegate(Exception error, out ErrorKind errorKind);

	/// <summary>
	/// ServiceErrorHelper defines helper methods on ServiveErrror
	/// </summary>
	public static class ServiceErrorHelper
	{
		//internal const string BAD_ARGUMENT = "BAD_ARGUMENT";
		internal const string INVALID_REQUEST = "INVALID_REQUEST";

		
		/// <summary>
		/// Converts exception to the ServiceError
		/// </summary>
		/// <param name="error"></param>
		/// <param name="errorKind"></param>
		/// <returns></returns>
		public static ServiceError FromException(Exception error, out ErrorKind errorKind)
		{
			ErrorKind temp;
			errorKind = ErrorKind.Unknown;
			var res = new ServiceError
			{
				Inner = error.InnerException == null ? null : FromException(error.InnerException, out temp),
				Time = DateTime.UtcNow,
				Details = error.Message
			};
			var be = error as IBabelException;
			if(be != null)
			{
				
				res.Context = be.Context;
				res.Errors = be.Errors.ToList();
				errorKind = be.Kind;
				return res;
			}

			string code = "INTERNAL_ERROR";
			/*if(error is ArgumentException)
			{
				code = BAD_ARGUMENT;
				errorKind = ErrorKind.InvalidArgument;
			}
			else*/
			if(error is ArgumentException ||error is ApplicationException || error is NotImplementedException)
			{
				code = INVALID_REQUEST;
				errorKind = ErrorKind.InvalidRequest;
			}

			res.Errors.Add(new Error { Code = code, Message = error.Message });
			return res;
		}
	}

	public static class ServiceErrorExtentions
	{
		public static Dictionary<string, string> GetContextCategory(this ServiceError @this, string key)
		{
			Dictionary<string, string> res;
			if(!@this.Context.TryGetValue(key, out res))
			{
				res = new Dictionary<string, string>();
				@this.Context[key] = res;
			}

			return res;
		}

		public static void AddContext(this ServiceError @this, string key, string subKey, string value)
		{
			var cat = GetContextCategory(@this, key);
			cat[subKey] = value;
		}
	}
}



