using System;
using System.Collections.Generic;

namespace CPFL_Interpreter_Console
{
	class Program
	{
		static void Main(string[] args)
		{
			// if(args.Length == 0)
			// {
			// 	Console.WriteLine("Please specifiy file");
			// 	return;
			// }
			Interpreter inter = new Interpreter("test.cpfl");
			inter.Run();
		}
	}
}
