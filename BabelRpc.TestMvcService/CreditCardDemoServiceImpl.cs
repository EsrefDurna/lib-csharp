using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BabelRpc;
using BabelRpc.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BabelRpc.Demo
{
	/// <summary>
	/// This is the part of controller MVC that is no automatically generated
	/// One should implement abstract InitBusinessLogic method.
	/// </summary>
	public partial class DemoCreditCardServiceController
	{
		/// <summary>
		/// This method should return the implementation of the business logic/service interface
		/// </summary>
		/// <returns></returns>
		protected override IDemoCreditCardService InitBusinessLogic()
		{
			//We can handle headers here
			string testExceptionHeader = Request.Headers["TestExeptionData"];
			return new DemoCreditCardServiceImpl(testExceptionHeader);
		}

		#region Additional exception translation

		/// <summary>
		/// Converts exception to ServiceError
		/// </summary>
		/// <param name="error"></param>
		/// <param name="errorKind"></param>
		/// <returns></returns>
		protected override ServiceError TranslateError(Exception error, out ErrorKind errorKind)
		{
			//AggregateException is very common in async programming in .Net
			var ae = error as AggregateException;
			if (ae != null)
			{
				//More likely to be an unexpected service error
				errorKind = ErrorKind.Unknown;

				//Fill the basic error details
				var se = new ServiceError { Time = DateTime.UtcNow, Details = ae.Message };

				//AggregateException should aggregate something, but it is better check 
				if (ae.InnerExceptions != null)
				{
					//Add an error data for each of the aggregated exceptions
					foreach (var inner in ae.InnerExceptions)
					{
						se.Errors.Add(new ErrorEx("PROCESSING_ERROR", inner.Message));
					}
				}
				return se;
			}

			//Just for a demo
			var knfe = error as KeyNotFoundException;
			if (knfe != null)
			{
				//Let's assume something asked for a wrong element in collection
				errorKind = ErrorKind.InvalidRequest;

				//Fill the basic error details
				var se = new ServiceError { Time = DateTime.UtcNow, Details = knfe.Message };
				se.Errors.Add(new ErrorEx("ITEM_NOT_FOUND", knfe.Message));

				//Typically we don't want to disclose the stack trace, but it might be OK for an internal service
				//se.AddContext("ERROR_INFO", "STACK_TRACE", knfe.StackTrace);
				
				//Handling inner exception if present
				if (knfe.InnerException != null)
				{
					ErrorKind k;
					se.Inner = TranslateError(knfe.InnerException, out k);
				}
				return se;
			}

			//Perform default processing if it is not an AggregateExcaption
			return base.TranslateError(error, out errorKind);
		}
		#endregion
	}


	public class DemoCreditCardServiceImpl : IDemoCreditCardService
	{
		public DemoCreditCardServiceImpl(string testExceptionHeader)
		{
			m_testExceptionHeader = testExceptionHeader;
		}

		private string m_testExceptionHeader;

		const string TEST_VI = "4111111111111111";
		const string TEST_MC = "5105105105105100";
		const string TEST_AX = "378282246310005";
		#region IDemoCC Members

		public void Save(CreditCard cardInfo)
		{
			//This is to test exception mapping
			if (m_testExceptionHeader == "throw_aggregate") throw new AggregateException(new KeyNotFoundException("Inside of AggregateException!"), new ApplicationException("Some other error"));
						

			cardInfo.Validate();
			string number = GetTestCCNumber(cardInfo.Kind);

			if (cardInfo.Number == number)
			{
				//ServiceApplicationException lets you to specify one or more error providing the error code, message template and parameters
				throw new ServiceApplicationException("KNOWN_DEMO_NUMBER", "Number {0} for {1} already known", cardInfo.Number, cardInfo.Kind.ToString());
			}
		}

		public CreditCard GetRandomCard(CCKind? kind)
		{
			var rnd = new Random();
			string number = GetTestCCNumber(kind);

			return new CreditCard { Number = number, Kind = kind, ExpirationMonth = (byte)rnd.Next(1, 12), ExpirationYear = (short)rnd.Next(1990, 2050) };
		}

		private static string GetTestCCNumber(CCKind? kind)
		{
			string number;
			switch (kind)
			{
				case CCKind.Visa: number = TEST_VI; break;
				case CCKind.MasterCard: number = TEST_MC; break;
				case CCKind.Amex: number = TEST_AX; break;
				default: throw new ServiceApplicationException("UNSUPPORTED_CARD_KIND", "Card kind {0} is not supported", kind.ToString());

			}
			return number;
		}
		#endregion
	}
}