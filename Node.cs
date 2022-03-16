using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CPFL_Interpreter_Console
{
	class QueueObj {
		public Node node;
		public int hd;

		public QueueObj(Node node, int hd)
		{
			this.node = node;
			this.hd = hd;
		}
	}
	class Node
	{
		public Node parent {get; set;}
		public Node left {get; set;}
		public Node right {get; set;}
		public Token token {get; set;}

		public Node(Node parent, Token token)
		{
			this.parent = parent;
			this.token = token;
		}

		public string displayNode()
		{
			StringBuilder output = new StringBuilder();
			displayNode(output, 0);
			return output.ToString();
		}

		private void displayNode(StringBuilder output, int depth)
		{
			if (right != null)
				right.displayNode(output, depth+1);

			output.Append(' ', depth*2);
			output.AppendLine((((int)token.lex) >= 23 && ((int)token.lex) <= 26) || token.lex == Lexeme.IDENTIFIER ? token.literal : token.lex.ToString());


			if (left != null)
				left.displayNode(output, depth+1);

		}
	}
}