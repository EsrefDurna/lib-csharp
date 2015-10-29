using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace BabelRpc
{
	public static class BabelModelExtensions
	{
		#region Compare
		/// <summary>
		/// Perform deep comparison of two model instances
		/// </summary>
		/// <param name="obj">First object to compare</param>
		/// <param name="value">Second object to compare</param>
		/// <returns>If both objects are equal</returns>
		public static bool Compare(this IBabelModel obj, IBabelModel value)
		{
			string s;
			return Compare(obj, value, out s);
		}

		/// <summary>
		/// Perform deep comparison of two model instances
		/// </summary>
		/// <param name="obj">First object to compare</param>
		/// <param name="value">Second object to compare</param>
		/// <param name="path">Location of the non-equal element found first</param>
		/// <returns>If both objects are equal</returns>
		public static bool Compare(this IBabelModel obj, IBabelModel value, out string path)
		{
			if (obj == null) 
			{
				if (value == null)
				{
					path = null;
					return true;
				}
				else
				{
					path = "";
					return false;
				}
			}
			var ci = new CompareInfo { Model = value, Path = null, AreEqual = true};
			obj.RunOnChildren<CompareInfo>(CompareInternal, ci);
			path = ci.Path;
			return ci.AreEqual;
		}


		static object CompareInternal(string name, System.Type itemType, object itemData, CompareInfo auxData)
		{
			if (!auxData.AreEqual) 
			{
				return itemData; //No reason to proceed
			}

			var data = new DataWrapper();
			auxData.Model.RunOnChild<DataWrapper>(name, GetProperty, data); //Get named element from the second model

			auxData.AreEqual = CompareInternal(name, itemData, data.Data, out auxData.Path);
			return itemData;
		}

		static bool CompareInternal(string name, object data1, object data2, out string path)
		{
			if (data1 == null)
			{
				if (data2 == null)
				{
					path = null;
					return true;
				}
				else 
				{
					path = name;
					return false;
				}
			}

			if (data2 == null) //data1 != null
			{
				path = name;
				return false;
			}

			var dict1 = data1 as IDictionary;
			if(dict1 != null)
			{
				var dict2 = data2 as IDictionary;
				if (dict2 == null) 
				{
					path = name;
					return false;
					//"Both or none supposed to be dictionaries: " + name);
				}

				if(dict1.Count != dict2.Count)
				{
					path = name;
					return false;
				}

				foreach(DictionaryEntry kv in dict1)
				{
					if (!CompareInternal(string.Format("{0}[{1}]", name, kv.Key), kv.Value, dict2[kv.Key], out path))
					{
						return false;
					}
				}

				path = null;
				return true; 
			}
			else
			{
				var ba1 = data1 as byte[];
				if(ba1 != null)
				{
					var ba2 = data2 as byte[];
					if (ba2 == null || ba1.Length != ba2.Length)
					{
						path = name;
						return false;
					}

					for(int i = 0; i < ba1.Length; i++)
					{
						if (ba1[i] != ba2[i])
						{
							path = string.Format("{0}[{1}]", name, i);
							return false;
						}
					}

					path = null;
					return true; 
			}
				
				else 
				{
					var list1 = data1 as IList;
					if(list1 != null)
					{
						var list2 = data2 as IList;
						if(list2 == null || list1.Count != list2.Count)
						{
							path = name;
							return false;
						}

						for(int i = 0; i < list1.Count; i++)
						{
							if(!CompareInternal(string.Format("{0}[{1}]", name, i), list1[i], list2[i], out path))
							{
								return false;
							}
						}

						path = null;
						return true;
					}
					else
					{
						if(data1 is DateTime)
						{
							if (!(data2 is DateTime))
							{
								path = name;
								return false;
							}

							if(Math.Abs((((DateTime)data1) - ((DateTime)data2)).TotalMilliseconds) > 5)
							{
								path = name;
								return false;
							}
							else
							{
								path = null;
								return true; 
							}

						}
						else
						{
							var m1 = data1 as IBabelModel;
							if(m1 != null)
							{
								var m2 = data2 as IBabelModel;
								if (m2 == null)
								{
										path = name;
										return false;
								}

								var cd = new CompareInfo { Model = m2, AreEqual = true };
								m1.RunOnChildren<CompareInfo>(CompareInternal, cd);
								path = cd.AreEqual ? null : name + "/" + cd.Path;
								return cd.AreEqual;
							}
							else
							{
								if (data1.Equals(data2))
								{
									path = null;
									return true;
								}
								else 
								{
									path = name;
									return false;
								}
							}
						}
					}
				}
			}
		}

		sealed class CompareInfo
		{
			public IBabelModel Model;
			public bool AreEqual;
			public string Path;
		}

		sealed class DataWrapper
		{
			public object Data;
		}

		static object GetProperty(string name, System.Type itemType, object itemData, DataWrapper auxData)
		{
			auxData.Data = itemData;
			return itemData;
		}
		#endregion

		#region Deep copy
		public static IBabelModel DeepCopy(this IBabelModel obj)
		{
			if(obj == null)
			{
				return null;
			}

			Type tp = obj.GetType();
			var res = (IBabelModel)Activator.CreateInstance(tp);

			obj.RunOnChildren<IBabelModel>(CopyInternal, res);

			return res;
		}

		static object CopyInternal(string name, System.Type itemType, object data, IBabelModel result)
		{
			object newVal = CopyInternal(data);
			result.RunOnChild<object>(name, SetProperty, newVal);
			return data;
		}

		static object CopyInternal(object data)
		{
			if (data == null)
			{
				return null;
			}
				

			var dict = data as IDictionary;
			if(dict != null)
			{
				var resD = (IDictionary)Activator.CreateInstance(data.GetType());

				foreach(DictionaryEntry kv in dict)
				{
					resD[kv.Key] = CopyInternal(kv.Value);
				}
				return resD;
			}
			else
			{
				var ba = data as byte[];
				if(ba != null)
				{
					int len = ba.Length;
					var resB = new byte[len];
					Array.Copy(ba, resB, len);
					return resB;
				}
				else
				{
					var list = data as IList;
					if(list != null)
					{
						var resL = (IList)Activator.CreateInstance(data.GetType());
						for(int i = 0; i < list.Count; i++)
						{
							resL.Add(CopyInternal(list[i]));
						}
						return resL;
					}
					else
					{
						var m = data as IBabelModel;
						if(m != null)
						{
							var resM = (IBabelModel)Activator.CreateInstance(data.GetType());
							m.RunOnChildren<IBabelModel>(CopyInternal, resM);
							return resM;
						}
						else
						{
							return data; //Should be all the value types and strings
						}
					}
				}
			}
		}


		static object SetProperty(string name, System.Type itemType, object data, object result)
		{
			return result;
		}
		#endregion

		#region Validate

		
		/// <summary>
		/// Validates model
		/// </summary>
		/// <param name="obj">Model to validate</param>
		/// <param name="results">Validation error details</param>
		/// <returns>True - validation success, false - failure</returns>
		public static bool Validate(this IBabelModel obj, out List<ValidationResult> results)
		{
			results = new List<ValidationResult>();
			if (obj == null) return true; //?or false
			var vd = new ValidationData {Results = results, IsValid = true, Prefix = ""};
			ValidateModel(obj, vd);
			return vd.IsValid;
		}

		/// <summary>
		/// Validates model throwing exception on validation error
		/// </summary>
		/// <param name="obj">Model to validate</param>
		public static void Validate(this IBabelModel obj)
		{
			List<ValidationResult> errors;
			bool res = Validate(obj, out errors);
			if(!res)
			{
				throw new BabelValidionException(errors);
			}
		}

		private static void ValidateModel(IBabelModel obj, ValidationData vd)
		{
			var vc = new ValidationContext(obj, null, null);
			var res = new List<ValidationResult>();
			vd.IsValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(obj, vc, res, true) & vd.IsValid;
			if(vd.Prefix.Length > 0)
			{
				foreach(var r in res)
				{
						string[] members = (r.MemberNames == null) ? null : r.MemberNames.Select(m => vd.Prefix + "/" + m).ToArray();
						vd.Results.Add(new ValidationResult(r.ErrorMessage, members));
				}
			}
			else
			{
				vd.Results.AddRange(res);
			}
			obj.RunOnChildren<ValidationData>(ValidateProperty, vd, false);
		}


		sealed class ValidationData
		{
			public List<ValidationResult> Results;
			public bool IsValid;
			public string Prefix;
		}

		private static object ValidateProperty(string name, Type itemType, object itemData, ValidationData vd)
		{
			if (itemData == null) return null;
			var m = itemData as IBabelModel;
			if (m!= null)
			{
				string prefix = vd.Prefix;
				vd.Prefix = (vd.Prefix.Length > 0) ? prefix + "/" + name : name;
				ValidateModel(m, vd);
				vd.Prefix = prefix;
				return itemData;
			}

			var l = itemData as IList;
			if (l!= null)
			{
				for(int i = 0; i < l.Count; i ++)
				{
					ValidateProperty(name + "[" + i + "]", null, l[i], vd);
				}
				return itemData;
			}

			var d = itemData as IDictionary;
			if (d!= null)
			{
				foreach(DictionaryEntry kv in d)
				{
					ValidateProperty(name + "[" + ((kv.Key == null) ? "null" : kv.Key.ToString()) + "]", null, kv.Value, vd);
				}
				return itemData;
			}

			return itemData;
		}
		#endregion
	}
}

