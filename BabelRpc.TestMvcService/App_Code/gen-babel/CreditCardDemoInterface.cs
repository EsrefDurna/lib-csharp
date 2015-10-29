// <auto-generated />
// AUTO-GENERATED FILE - DO NOT MODIFY
// Generated from CreditCardDemo.babel
// Test Case
// Fake storing and manipulations with demo credit card numbers


using System;
using System.Collections.Generic;

namespace BabelRpc.Demo
{ 
	/// <summary>
	///  Manipulates with demo credit card info
	/// </summary>
	[System.CodeDom.Compiler.GeneratedCode("Babel", "")]
	public interface IDemoCC
	{ 
		/// <summary>
		/// Validates CC info and pretends to save it
		/// </summary>
		/// <param name="cardInfo"></param>
		void Save(CreditCard cardInfo);

		/// <summary>
		/// Gets random demo CC info of given kind
		/// </summary>
		/// <param name="kind"></param>
		CreditCard GetRandomCard(CCKind? kind);
	}
	/// <summary>
	///  Manipulates with demo credit card info
	/// </summary>
	[System.CodeDom.Compiler.GeneratedCode("Babel", "")]
	public interface IDemoCCAsync
	{ 
		/// <summary>
		/// Validates CC info and pretends to save it
		/// </summary>
		/// <param name="cardInfo"></param>
		System.Threading.Tasks.Task SaveAsync(CreditCard cardInfo);

		/// <summary>
		/// Gets random demo CC info of given kind
		/// </summary>
		/// <param name="kind"></param>
		System.Threading.Tasks.Task<CreditCard> GetRandomCardAsync(CCKind? kind);
	}
}
