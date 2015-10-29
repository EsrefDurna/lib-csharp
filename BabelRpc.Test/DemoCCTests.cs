using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

namespace BabelRpc.Demo
{
	[TestClass]
	public class DemoCCTests
	{
		static DemoCCClient GetClient()
		{
			return new DemoCCClient("http://localhost/BabelRpcTestMvcService/");
		}

		[TestMethod]
		public void TestGetVI()
		{
			var visa = GetClient().GetRandomCard(CCKind.Visa);
			var errs = new List<ValidationResult>();
			Assert.IsTrue(visa.Validate(out errs), "Validation failed");
			Assert.AreEqual(0, errs.Count, "No errors expected");
			Assert.AreEqual(16, visa.Number.Length, "Visa number is 16 digits");
		}


		[TestMethod]
		public void TestSetVI()
		{
			var cc = new CreditCard { Number = "4012888888881881", Kind = CCKind.Visa, ExpirationYear = 2000, ExpirationMonth = 1 };
			GetClient().Save(cc);
		}

		[TestMethod]
		public void TestSetExistingVI()
		{
			var cc = new CreditCard { Number = "4111111111111111", Kind = CCKind.Visa, ExpirationYear = 2000, ExpirationMonth = 1 };
			try
			{
				GetClient().Save(cc);
				Assert.Fail("Validation should fail");
			}
			catch (BabelException err)
			{
				Assert.AreEqual("Number 4111111111111111 for Visa already known", err.Message, "Unexpected message");
				Assert.AreEqual(1, err.Errors.Count, "Unexpected errors count");
				Assert.AreEqual("Number 4111111111111111 for Visa already known", err.Errors[0].Message, "Unexpected internal message");
				Assert.AreEqual("KNOWN_DEMO_NUMBER", err.Errors[0].Code, "Unexpected internal code");
				Assert.AreEqual(2, err.Errors[0].Params.Count, "Unexpected number of internal parameters");
				Assert.AreEqual("4111111111111111", err.Errors[0].Params[0], "Unexpected internal parameter 0");
				Assert.AreEqual("Visa", err.Errors[0].Params[1], "Unexpected internal parameter 1");
			}
		}

		[TestMethod]
		public void TestSetBadYearAndNumberVI()
		{
			var cc = new CreditCard { Number = "4111111111111112", Kind = CCKind.Visa, ExpirationYear = 1000, ExpirationMonth = 1 };
			try
			{
				GetClient().Save(cc);
				Assert.Fail("Validation should fail");
			}
			catch (BabelApplicationException err)
			{
				Assert.AreEqual("The Number field is not a valid credit card number.\r\nThe field ExpirationYear must be between 1980 and 2100.", err.Message, "Unexpected message");
				Assert.AreEqual(2, err.Errors.Count, "Unexpected errors count");
				Assert.AreEqual("The Number field is not a valid credit card number.", err.Errors[0].Message, "Unexpected internal message 0");
				Assert.AreEqual("VALIDATION_ERROR", err.Errors[0].Code, "Unexpected internal code");
				Assert.AreEqual(1, err.Errors[0].Params.Count, "Unexpected number of internal parameters 0");
				Assert.AreEqual("Number", err.Errors[0].Params[0], "Unexpected value of internal parameter 0");

				Assert.AreEqual("The field ExpirationYear must be between 1980 and 2100.", err.Errors[1].Message, "Unexpected internal message 1");
				Assert.AreEqual("VALIDATION_ERROR", err.Errors[1].Code, "Unexpected internal code 1");
				Assert.AreEqual(1, err.Errors[1].Params.Count, "Unexpected number of internal parameters 1");
				Assert.AreEqual("ExpirationYear", err.Errors[1].Params[0], "Unexpected value of internal parameter 1");
				
			}
		}

		[TestMethod]
		public void TestAggregateExceptionMapping()
		{
			var cc = new CreditCard { Number = "4111111111111111", Kind = CCKind.Visa, ExpirationYear = 2000, ExpirationMonth = 1 };
			try
			{

				var client = GetClient();
				client.Headers.Add("TestExeptionData", "throw_aggregate");
				client.Save(cc);
				Assert.Fail("Service should error");
			}
			catch (BabelException err)
			{
				Assert.AreEqual("One or more errors occurred.", err.Message, "Unexpected message");
				Assert.AreEqual(2, err.Errors.Count, "Unexpected errors count");
	
				Assert.AreEqual("Inside of AggregateException!", err.Errors[0].Message, "Unexpected internal message 0");
				Assert.AreEqual("PROCESSING_ERROR", err.Errors[0].Code, "Unexpected internal code 0");

				Assert.AreEqual("Some other error", err.Errors[1].Message, "Unexpected internal message 1");
				Assert.AreEqual("PROCESSING_ERROR", err.Errors[1].Code, "Unexpected internal code 1");
			}
		}
	}
}
