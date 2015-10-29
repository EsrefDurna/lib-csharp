using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BabelRpc;
using BabelRpc.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BabelRpc.Demo
{
	public partial class DemoCCController
	{
		protected override IDemoCC InitBusinessLogic()
		{
			//We can handle headers here
			string testExceptionHeader = Request.Headers["TestExeptionData"];
			return new DemoCCServiceImpl(testExceptionHeader);
		}

		#region Additional exception translation
		protected override ServiceError TranslateError(Exception error, out ErrorKind errorKind)
		{
			var ae = error as AggregateException;
			if (ae != null)
			{
				errorKind = ErrorKind.Unknown;
				var se = new ServiceError { Time = DateTime.UtcNow, Details = ae.Message };
				if (ae.InnerExceptions != null)
				{
					foreach (var inner in ae.InnerExceptions)
					{
						se.Errors.Add(new ErrorEx("PROCESSING_ERROR", inner.Message));
					}
				}
				return se;
			}

			return base.TranslateError(error, out errorKind);
		}
		#endregion
	}


	public class DemoCCServiceImpl : IDemoCC
	{
		public DemoCCServiceImpl(string testExceptionHeader)
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
			if (cardInfo.Number == number) throw new ServiceApplicationException("KNOWN_DEMO_NUMBER", "Number {0} for {1} already known", cardInfo.Number, cardInfo.Kind.ToString());
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