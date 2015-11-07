using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using BabelRpc;

namespace BabelRpc.Demo
{
	// Normally this class would be in its own file
	public partial class RoundtripperController 
	{
		protected override IRoundtripper InitBusinessLogic()
		{
			return new RoundtripperServiceImpl();
		}
	}

	public class RoundtripperServiceImpl : IRoundtripper
	{
		#region IRoundtripperAsync Members
		public Wholesome SendReceive(Wholesome wholesome, bool? alter)
		{
			wholesome.Validate();
			if (alter.HasValue && alter.Value == true)
			{
				wholesome.MyDATETIME = DateTime.UtcNow;
				wholesome.MyINT32 = 22222;
			}
			return wholesome;
		}

		public void Send(Wholesome wholesome)
		{
			//Just validate and do nothing
			wholesome.Validate(); // The exception if thrown will be logged/handled by the framework
		}

		public Wholesome Receive()
		{
			var wholesome = new Wholesome();
			wholesome.MyFLOAT32 = (float)-32.32;
			wholesome.MyDATETIME = DateTime.Now;
			wholesome.MyDECIMAL = (decimal)128.128;
			wholesome.MyBINARY = new byte[] { 65, 66, 67, 68, 69 };
			wholesome.MyJOKE = new Joke() { Question = "Why?", Answer = "Because.", DateAdded = DateTime.Now };
			wholesome.Names.Add("Homer");
			wholesome.Names.Add("Bart");
			wholesome.Names.Add("Lisa");
			wholesome.Names.Add("Maggie");
			wholesome.Names.Add("Marge");
			wholesome.Pokes.Add("Mike", new Joke() { Question = "Why not?", Answer = "Because I said so.", DateAdded = DateTime.Now });
			wholesome.Pokes.Add("Brian", new Joke() { Question = "Ok, but why?", Answer = "I have no idea.", DateAdded = DateTime.Now });

			wholesome.Argh.Add(
				new Dictionary<string, List<Joke>>()
				{
					{
						"Andrew", new List<Joke>()
						{
							new Joke() { Question = "Q1", Answer = "A1", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q2", Answer = "A2", DateAdded = DateTime.Now },
						}
					},
					{
						"Craig", new List<Joke>()
						{
							new Joke() { Question = "Q3", Answer = "A3", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q4", Answer = "A4", DateAdded = DateTime.Now },
						}
					}
				}
			);
			wholesome.MyNULL2 = 0;
			wholesome.Argh.Add(
				new Dictionary<string, List<Joke>>()
				{
					{
						"Viktor", new List<Joke>()
						{
							new Joke() { Question = "Q5", Answer = "A5", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q6", Answer = "A6", DateAdded = DateTime.Now },
						}
					},
					{
						"John", new List<Joke>()
						{
							new Joke() { Question = "Q7", Answer = "A7", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q8", Answer = "A8", DateAdded = DateTime.Now },
						}
					}
				}
			);
			wholesome.Validate();
			return wholesome;
		}

		public DefaultTest ReturnNulledDefault()
		{
			DefaultTest d = new DefaultTest();
			d.X = null;
			return d;
		}

		public bool? AcceptNulledDefault(DefaultTest d)
		{
			if (d == null || d.X.HasValue != true || d.X.Value != 32)
			{
				return false;
			}
			return true;
		}

		public bool? Fail()
		{
			throw new ApplicationException("Oops. Sorry about that.");
		}

		public bool? Validate(Wholesome item)
		{
			List<ValidationResult> results;
			return item.Validate(out results);
		}
		#endregion
	}
}