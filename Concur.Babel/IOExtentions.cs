using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Concur.Babel
{
	/// <summary>
	/// Extention methods for Stream
	/// </summary>
	public static class StreamExtentions
	{
		/// <summary>
		/// Reads a string from a Stream. Closes stream internally!
		/// </summary>
		public static string GetString(this Stream input, Encoding streamEncoding = null, bool closeStream = false)
		{
			if(streamEncoding == null) streamEncoding = Encoding.UTF8;

			StreamReader readStream = null;
			try
			{
				long currentPos = 0;
				if(input.CanSeek)
				{
					currentPos = input.Position;
					if(input.Position == input.Length) input.Position = 0;
				}


				readStream = new System.IO.StreamReader(input, streamEncoding);
				string res = readStream.ReadToEnd();
				if(input.CanSeek) input.Position = currentPos;
				return res;
			}
			finally
			{
				if(closeStream && readStream != null) readStream.Close();
			}
		}

		/// <summary>
		/// Converts Stream to array of bytes
		/// </summary>
		public static byte[] GetBytes(this Stream input)
		{
			const int CHUNK_SZ = 1024;
			int len;
			int totalLen = 0;
			List<byte[]> resList = new List<byte[]>();
			List<int> resListLen = new List<int>();
			do
			{
				byte[] bt = new byte[CHUNK_SZ];
				len = input.Read(bt, 0, CHUNK_SZ);
				if(len > 0)
				{
					resList.Add(bt);
					resListLen.Add(len);
					totalLen += len;
				}
			}
			while(len > 0);

			byte[] result = new byte[totalLen];
			int pos = 0;
			for(int i = 0; i < resList.Count; i++)
			{
				len = resListLen[i];
				Array.Copy(resList[i], 0, result, pos, len);
				pos += len;
			}

			if(input.CanSeek) input.Position = 0;
			return result;
		}

		/// <summary>
		/// Creates a copy of the stream data
		/// </summary>
		/// <param name="sourceStream"></param>
		/// <returns></returns>
		public static MemoryStream DeepCopy(this MemoryStream sourceStream)
		{
			return new MemoryStream(sourceStream.GetBytes());
		}

		/// <summary>
		/// Creates an XmlReader on a stream.  If the stream supports seek, stream can optionally be rewound to the
		/// beginning before creating
		/// </summary>
		/// <param name="input">The stream to create the XmlReader over</param>
		/// <returns>The XmlReader instance</returns>
		public static System.Xml.XmlReader GetXmlReader(this Stream input)
		{
			if(input.CanSeek && input.Position != 0)
			{
				input.Seek(0, SeekOrigin.Begin);
			}

			//this is the dotnet 4 version
			return XmlReader.Create(input, new XmlReaderSettings { CloseInput = false, DtdProcessing = DtdProcessing.Parse });

			//this is the dotnet 2/3.5 version
			//return XmlReader.Create(Input, new XmlReaderSettings { CloseInput = false, ProhibitDtd=false});
		}

		/// <summary>
		/// Creates XmlDocument from a stream
		/// </summary>
		public static System.Xml.XmlDocument GetXmlDocument(this Stream input)
		{
			if(input.Position != 0 && input.CanSeek)
			{
				input.Seek(0, SeekOrigin.Begin);
			}
			System.Xml.XmlDocument result = new System.Xml.XmlDocument();
			result.Load(input.GetXmlReader());
			return result;
		}

		/// <summary>
		/// Creates an XPathDocument on a stream.  If the stream supports seek, stream can optionally be rewound to the
		/// beginning before creating
		/// </summary>
		/// <param name="input">The stream to create the XPathDocument over</param>
		/// <returns>The XPath document</returns>
		public static System.Xml.XPath.XPathDocument GetXPathDocument(this Stream input)
		{
			return new System.Xml.XPath.XPathDocument(input.GetXmlReader());
		}
	}
}


