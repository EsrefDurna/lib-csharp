using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BabelRpc;

namespace BabelRpc.Mvc
{
	/// <summary>
	/// Exception to report one or many errors. There is a mapping in the Babel services controllers for this exception.
	/// </summary>
	public class ServiceApplicationException : BabelException 
	{
		/// <summary>
		/// Creates exception to report one or many errors. There is a mapping in the Babel services controllers for this exception.
		/// </summary>
		/// <param name="errorCode">Error code</param>
		/// <param name="message">Human readable short error description. If parameters provided can be a format string</param>
		/// <param name="parameters">Error parameters</param>
		/// <param name="innerException">Optional inner exception</param>
		public ServiceApplicationException(string errorCode, string message, IEnumerable<string> parameters, Exception innerException = null) 
			: this(new ErrorEx(errorCode, message, parameters))
		{
		}

		/// <summary>
		/// Creates exception to report one or many errors. There is a mapping in the Babel services controllers for this exception.
		/// </summary>
		/// <param name="errorCode">Error code</param>
		/// <param name="message">Human readable short error description. If parameters provided can be a format string</param>
		/// <param name="parameters">Error parameters</param>
		public ServiceApplicationException(string errorCode, string message, params string[] parameters)
			: this(new ErrorEx(errorCode, message, parameters))
		{
		}

		/// <summary>
		/// Creates exception to report one or many errors. There is a mapping in the Babel services controllers for this exception.
		/// </summary>
		/// <param name="errorCode">Error code</param>
		/// <param name="message">Human readable short error description. If parameters provided can be a format string</param>
		/// <param name="parameters">Error parameters</param>
		/// <param name="innerException">Optional inner exception</param>
		public ServiceApplicationException(Exception innerException, string errorCode, string message, params string[] parameters)
			: this(innerException, new ErrorEx(errorCode, message, parameters))
		{
		}

		/// <summary>
		/// Creates exception to report one or many errors. There is a mapping in the Babel services controllers for this exception.
		/// </summary>
		/// <param name="errors">Individual errors</param>
		/// <param name="innerException">Optional inner exception</param>
		public ServiceApplicationException(IEnumerable<Error> errors, Exception innerException = null)
			: this(innerException, errors == null ? null : errors.ToArray())
		{
		}

		/// <summary>
		/// Creates exception to report one or many errors. There is a mapping in the Babel services controllers for this exception.
		/// </summary>
		/// <param name="errors">Individual errors</param>
		public ServiceApplicationException(params Error [] errors) : this(null, errors)
		{
			if (errors != null)
			{
				base.m_errors.AddRange(errors);
			}
			m_errors = errors == null ? new List<Error>() : new List<Error>(errors);
		}

		/// <summary>
		/// Creates exception to report one or many errors. There is a mapping in the Babel services controllers for this exception.
		/// </summary>
		/// <param name="errors">Individual errors</param>
		/// <param name="innerException">Optional inner exception</param>
		public ServiceApplicationException( Exception innerException, params Error[] errors) : base(ErrorsToMessage(errors), innerException)
		{
		}

		static string ErrorsToMessage(Error[] errors)
		{
			if(errors == null) return "";
			return string.Join(", ", errors.Select(e => e.Message));
		}

		/// <summary>
		/// Adds error info to the existing exception
		/// </summary>
		/// <param name="message">Human readable short error description. If parameters provided can be a format string</param>
		/// <param name="parameters">Error parameters</param>
		/// <param name="errorCode">Error code</param>
		public void AddError(string errorCode, string message, IEnumerable<string> parameters)
		{
			m_errors.Add(new ErrorEx(errorCode, message, parameters));
		}

		/// <summary>
		/// Adds error info to the existing exception
		/// </summary>
		/// <param name="message">Human readable short error description. If parameters provided can be a format string</param>
		/// <param name="parameters">Error parameters</param>
		/// <param name="errorCode">Error code</param>
		public void AddError(string errorCode, string message, params string[] parameters)
		{
			m_errors.Add(new ErrorEx(errorCode, message, parameters));
		}
	}
}
