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
		public TestGroupEntry TestGroupEntry { get; set; }
		public TestTreeViewItem(TreeViewItemBase parent, TestGroupEntry testGroupEntry)
			: base(parent)
		{
			this.TestGroupEntry = testGroupEntry;
		}

		public string GetCmdString()
		{
			string cmdString = DisplayName;
			return " --run_test=" + cmdString;
		}
	}
}
