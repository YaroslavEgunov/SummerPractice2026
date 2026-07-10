using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Unigine;

namespace UnigineApp
{
	class UnigineApp
	{
		[STAThread]
		static void Main(string[] args)
		{
			Engine.Init(args);
			
			AppSystemLogic systemLogic = new AppSystemLogic();
			AppWorldLogic worldLogic = new AppWorldLogic();
			AppEditorLogic editorLogic = new AppEditorLogic();

			Engine.Main(systemLogic, worldLogic, editorLogic);

			Engine.Shutdown();
		}
	}
}