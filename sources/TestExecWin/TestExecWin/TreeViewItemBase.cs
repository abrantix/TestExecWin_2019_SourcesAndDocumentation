using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace TestExecWin
{
	//MultiSelectTreeViewItem
	public class TreeViewItemBase : INotifyPropertyChanged
	{
		static ImageSource OkIcon = LoadImageFromResource("Ok.png");
		static ImageSource ErrorIcon = LoadImageFromResource("Error.png");
		static ImageSource DisabledIcon = LoadImageFromResource("Cancel_bw.png");
		static ImageSource TentativeIcon = LoadImageFromResource("Help.png");

		public ObservableCollection<TreeViewItemBase> TreeViewItems { get; set; }
		public TestResult testResult;

		private ImageSource icon;

		public event PropertyChangedEventHandler PropertyChanged;

		public TreeViewItemBase TreeViewParent { get; set; }

		public virtual string DisplayName { get; set; }

		public TreeViewItemBase(TreeViewItemBase parent)
		{
			this.TreeViewItems = new ObservableCollection<TreeViewItemBase>();
			this.TreeViewParent = parent;
			TestResult = new TestResult();
		}

		public TestResult TestResult
		{ get
			{ return testResult; }
			set
			{
				Debug.WriteLine("TestResult setter");
				testResult = value;
				if (testResult != null)
				{
                    //testResult.PropertyChanged += TestResult_PropertyChanged;
					//UpdateIcon();
				}
				Debug.WriteLine("TestResult setter end");
			}
		}

        private void TestResult_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
			Debug.WriteLine("TestResult_PropertyChanged");
			switch (e.PropertyName)
			{
				case "Result":
					//UpdateIcon();
					break;
				
				default:
					break;
			}
			Debug.WriteLine("TestResult_PropertyChanged end");
		}

		public void ReflectTestResultsFromChilds()
		{
			var testFunctions = GetOverallTestFunctions();
			if (testFunctions.Any(x => x.TestResult.Result == Result.Failed))
			{
				TestResult.Result = Result.Failed;
			}
			else if (testFunctions.Any(x => x.TestResult.Result == Result.Tentative))
			{
				TestResult.Result = Result.Tentative;
			}
			else if (testFunctions.Any(x => x.TestResult.Result == Result.Success))
			{
				TestResult.Result = Result.Success;
			}
			else
            {
				TestResult.Result = Result.Disabled;
			}
		}

		public void PropagateTestResultToAllChilds()
		{
			GetOverallChildItems().ForEach(x => x.TestResult.Result = TestResult.Result);
		}

		private void UpdateIcon()
		{
			Debug.WriteLine("UpdateIcon");
			if (TestResult != null)
			{
				switch (testResult.Result)
				{
					case Result.Tentative:
						//Icon = TentativeIcon;
						break;
					
					case Result.Success:
						//Icon = OkIcon;
						break;
					
					case Result.Failed:
						//Icon = ErrorIcon;
						break;

					case Result.Disabled:
						//Icon = DisabledIcon;
						break;

					default:
						Icon = null;
						break;
				}
			}
			Debug.WriteLine("UpdateIcon end");
		}

		public int OverallTestFunctionCount
		{
			get
			{
				int childItemCount = TreeViewItems.Count(x => x.GetType() == typeof(TestFunctionTreeViewItem));
				//recursive call for childs
				TreeViewItems.ToList().ForEach(q => childItemCount += q.OverallTestFunctionCount);

				return childItemCount;
			}
		}

		public TestTreeViewItem[] GetOverallTestGroups()
		{
			var list = new List<TreeViewItemBase>();
			list.AddRange(TreeViewItems.Where(x => x.GetType() == typeof(TestTreeViewItem)));
			//recursive call for childs
			TreeViewItems.ToList().ForEach(q => list.AddRange(q.GetOverallTestGroups()));

			return list.Cast<TestTreeViewItem>().ToArray();
		}

		public TestFunctionTreeViewItem[] GetOverallTestFunctions()
		{
			var list = new List<TreeViewItemBase>();
			list.AddRange(TreeViewItems.Where(x => x.GetType() == typeof(TestFunctionTreeViewItem)));
			//recursive call for childs
			TreeViewItems.ToList().ForEach(q => list.AddRange(q.GetOverallTestFunctions()));

			return list.Cast<TestFunctionTreeViewItem>().ToArray();
		}

		public List<TreeViewItemBase> GetOverallChildItems()
		{
			var list = new List<TreeViewItemBase>();
			list.AddRange(TreeViewItems);
			//recursive call for childs
			TreeViewItems.ToList().ForEach(q => list.AddRange(q.GetOverallChildItems()));

			return list;
		}

		public List<TreeViewItemBase> GetAllAncestors()
		{
			var list = new List<TreeViewItemBase>();
			if (TreeViewParent != null)
            {
				list.AddRange(TreeViewParent.GetAllAncestors());
				list.Add(TreeViewParent);
			}
			return list;
		}

		public TestTreeViewItem[] GetMainTestGroups()
		{
			var list = new List<TreeViewItemBase>();
			list.AddRange(TreeViewItems.Where(x => x.GetType() == typeof(TestTreeViewItem)));

			return list.Cast<TestTreeViewItem>().ToArray();
		}

		public void OverallSortAllChilds(SortOrder sortOrder)
		{
			switch (sortOrder)
			{
				case SortOrder.None:
					break;
				
				case SortOrder.Ascending:
					TreeViewItems = new ObservableCollection<TreeViewItemBase>(TreeViewItems.AsEnumerable().OrderBy(x => x.DisplayName));
					//recursive call
					TreeViewItems.ToList().ForEach(x => x.OverallSortAllChilds(sortOrder));
					break;
				
				case SortOrder.Descending:
					TreeViewItems = new ObservableCollection<TreeViewItemBase>(TreeViewItems.AsEnumerable().OrderByDescending(x => x.DisplayName));
					//recursive call
					TreeViewItems.ToList().ForEach(x => x.OverallSortAllChilds(sortOrder));
					break;

				case SortOrder.Reverse:
					TreeViewItems = new ObservableCollection<TreeViewItemBase>(TreeViewItems.AsEnumerable().Reverse());
					//recursive call
					TreeViewItems.ToList().ForEach(x => x.OverallSortAllChilds(sortOrder));
					break;

				default:
					throw new NotImplementedException();
			}
			
		}

		public ImageSource Icon
		{
			get { return icon; }
			set
			{
				Debug.WriteLine("Icon setter");
				if (icon != value)
				{
					icon = value;
					//OnPropertyChanged();
				}
				Debug.WriteLine("Icon setter end");
			}
		}

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			Debug.WriteLine("OnPropertyChanged");
			try
			{
				//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			} catch {
				int a = 0;
			}
			Debug.WriteLine("OnPropertyChanged end");
		}

		private static BitmapImage LoadImageFromResource(string name)
		{
			string bitmapPath = $"pack://application:,,,/TestExecWin;component/Resources/{name}";

			var bitmapImage = new BitmapImage(new Uri(bitmapPath, UriKind.Absolute));
			bitmapImage.Freeze();
			return bitmapImage;
		}

	}
}
