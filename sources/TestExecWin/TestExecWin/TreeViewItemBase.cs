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

namespace TestExecWin
{
	//MultiSelectTreeViewItem
	public class TreeViewItemBase : INotifyPropertyChanged
	{
		static ImageSource OkIcon = LoadImageFromResource("Ok.png");
		static ImageSource ErrorIcon = LoadImageFromResource("Error.png");

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
				testResult = value;
				if (testResult != null)
				{
                    testResult.PropertyChanged += TestResult_PropertyChanged;
					UpdateIcon();
				}
			}
		}

        private void TestResult_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
			switch (e.PropertyName)
			{
				case "Result":
					UpdateIcon();
					break;
				
				default:
					break;
			}
        }

		public void ReflectTestResultsFromChilds()
		{
			var testFunctions = GetOverallTestFunctions();
			if (testFunctions.All(x => x.TestResult.Result == Result.Success))
			{
				TestResult.Result = Result.Success;
				UpdateIcon();
			}
			else if (testFunctions.Any(x => x.TestResult.Result == Result.Failed))
			{
				TestResult.Result = Result.Failed;
				UpdateIcon();
			}
		}

		public void PropagateTestResultToAllChilds()
		{
			GetOverallChildItems().ToList().ForEach(x => x.TestResult.Result = TestResult.Result);
		}

		private void UpdateIcon()
		{
			if (TestResult != null)
			{
				switch (testResult.Result)
				{
					case Result.Tentative:
						Icon = null;
						break;
					
					case Result.Success:
						Icon = OkIcon;
						break;
					
					case Result.Failed:
						Icon = ErrorIcon;
						break;
					
					default:
						Icon = null;
						break;
				}
			}
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

		public TestFunctionTreeViewItem[] GetOverallTestFunctions()
		{
			var list = new List<TreeViewItemBase>();
			list.AddRange(TreeViewItems.Where(x => x.GetType() == typeof(TestFunctionTreeViewItem)));
			//recursive call for childs
			TreeViewItems.ToList().ForEach(q => list.AddRange(q.GetOverallTestFunctions()));

			return list.Cast<TestFunctionTreeViewItem>().ToArray();
		}

		public TreeViewItemBase[] GetOverallChildItems()
		{
			var list = new List<TreeViewItemBase>();
			list.AddRange(TreeViewItems);
			//recursive call for childs
			TreeViewItems.ToList().ForEach(q => list.AddRange(q.GetOverallChildItems()));

			return list.ToArray();
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
				if (icon != value)
				{
					icon = value;
					OnPropertyChanged();
				}
			}
		}

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
