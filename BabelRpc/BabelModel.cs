using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BabelRpc
{
	public delegate object BabelModelAction<T>(string name, Type itemType, object itemData, T auxData);

	public interface IBabelModel
	{
		/// <summary>
		/// Execute BabelModelAction on every property
		/// </summary>
		/// <typeparam name="T">Type of the auxiliary data</typeparam>
		/// <param name="method">>BabelModelAction to execute</param>
		/// <param name="auxData">Auxiliary data</param>
		/// <param name="runOnAll">true - run BabelModelAction on all properties; false - run BabelModelAction only on nested classes(structures), lists and dictionaries(maps)</param>
		void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true);
		
		/// <summary>
		/// Execute BabelModelAction on the property with specific name
		/// </summary>
		/// <typeparam name="T">Type of the auxiliary data</typeparam>
		/// <param name="name">Name of the property to execute BabelModelAction against</param>
		/// <param name="method">BabelModelAction to execute</param>
		/// <param name="auxData">Auxiliary data</param>
		/// <returns>If property was found</returns>
		bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData);
	}
}
