using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Concur.Babel
{
	public static class ObjectExtensions
	{
		static readonly BabelXmlSerializer s_babelXmlSerilaizer = new BabelXmlSerializer();
		/// <summary>
		/// Serializes object to an xml byte array
		/// Do NOT use it to serialize to standalone XML for the classes containing Nullable - this will create not declared namespace in the document
		/// </summary>
		/// <param name="obj">object to be serialized</param>
		/// <returns>Xml string</returns>
		public static byte[] ToXmlBytes(this object obj)
		{
			if(obj == null) return null;
			//this is for backward compatibility with ToXml extension
			//var exception = obj as Exception;
			//if(exception != null) return OTError.ExceptionToXmlBytes(exception);


			using (var ms = s_babelXmlSerilaizer.Serialize(obj))
			{
				return ((MemoryStream)ms).ToArray();
			}
		}

		static InvalidCastException GetConvertionError(string input, string toType, Exception innerError = null)
		{
			return new InvalidCastException(string.Format("Cannot cast value \"{0}\" of the type string to the {1}", input, toType), innerError);
		}

		internal static object ConvertString(Type targetType, string input)
		{
			if(targetType == typeof(string) || targetType == typeof(object)) //object is a special case used for don't know type/extra element
			{
				return input;
			}

			//We don't process xsi:nil="true" so just let's assume the empty node to be null if result is not a string
			bool isNullable = targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>);

			//Empty, whitespace only string and null handling is the same for anything except string->string conversions
			if(string.IsNullOrEmpty(input))
			{
				if(targetType.IsValueType && !isNullable)
				{
					throw new InvalidCastException("Cannot cast null value to type " + targetType.Name);
				}
				return null;
			}


			if(targetType == typeof(byte[]))
			{
				try
				{
					return Convert.FromBase64String(input);
				}
				catch(Exception err)
				{
					throw GetConvertionError(input, "byte[]", err);
				}
			}

			Type castBaseType = isNullable ? targetType.GetGenericArguments()[0] : targetType;
			if(castBaseType == typeof(bool))
			{
				string temp = input.ToLowerInvariant();
				if(temp == "true" || temp == "1") return true;
				if(temp == "false" || temp == "0") return false;
				throw GetConvertionError(input, "boolean"); 
			}

			if(castBaseType == typeof(DateTime))
			{
				try
				{
					return System.Xml.XmlConvert.ToDateTime(input, System.Xml.XmlDateTimeSerializationMode.RoundtripKind); //This handles timezones correctly
				}
				catch(Exception err)
				{
					throw GetConvertionError(input, "DateTime", err); 
				}
			}

			if(castBaseType == typeof(decimal))
			{
				decimal d;
				
				if (decimal.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) //This will handle exponent format
				{
					return d;
				}

				int idx = input.IndexOfAny(new [] {'e', 'E'}); //The parse still failing on 0e8 style numbers. 0e-8 and 1.234e5 numners are parsed fine 
				if(idx > 0)
				{
					string s = input.Substring(0, idx);
					if(decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d))
					{
						if(d == 0.0m) return 0.0m; //Zero with any exponent is still zero
					}
				}
				throw GetConvertionError(input, "decimal");
			}

			if(castBaseType.IsEnum)
			{
				//If it is string - parse as item name
				//Enum nullable doesn't work properly with generic parameters
				try
				{
					return System.Enum.Parse(castBaseType, input);
				}
				catch(Exception err)
				{
					throw GetConvertionError(input, "enum type " + castBaseType.Name, err);
				}
			}

			try
			{
				return Convert.ChangeType(input, castBaseType, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch(Exception err)
			{
				throw GetConvertionError(input, castBaseType.Name, err);
			}
		}

	}
}
