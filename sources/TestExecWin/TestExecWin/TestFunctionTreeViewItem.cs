using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
	public class TestFunctionTreeViewItem : TreeViewItemBase
	{
		public TestFuncEntry TestFuncEntry { get; set; }
		public TestFunctionTreeViewItem(TreeViewItemBase parent, TestFuncEntry testFuncEntry)
			: base(parent)
		{
			TestFuncEntry = testFuncEntry;
		}

        public override string DisplayName 
		{ 
			get => TestFuncEntry.GetDisplayString(); set => throw new NotSupportedException(); 
		}
	}
}
