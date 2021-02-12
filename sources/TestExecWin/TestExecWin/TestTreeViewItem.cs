using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
	public class TestTreeViewItem : TreeViewItemBase
	{
		public TestTreeViewItem(TreeViewItemBase parent)
			: base(parent)
		{
		}

		public string GetCmdString()
		{
			string cmdString = DisplayName;
			return "--run_test=" + cmdString;
		}
	}
}
