using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
    public class Node<T>
    {
        private Node<T> child;
        private Node<T> parent;

        public bool HasChild()
        { 
            return Child != null;
        }

        public Node<T> Child
        {
            get { return child; }
            set
            {
                if (child != value)
                {
                    child = value;
                    if (child != null)
                    {
                        child.Parent = this;
                    }
                }
            }
        }

        public Node<T> Parent
        {
            get { return parent; }
            set
            {
                if (parent != value)
                {
                    parent = value;
                    if (parent != null)
                    {
                        parent.Child = this;
                    }
                }
            }
        }

        public T Value { get; set; }
        public Node(T value)
        {
            Value = value;
            Child = null;
            Parent = null;
        }

        public Node(T value, Node<T> child)
            : this(value)
        {
            Child = child;
            Child.Parent = this;
        }
        public Node<T> GetLeaf()
        {
            var current = this;
            while (true)
            {
                if (current.Child == null)
                {
                    return current;
                }
                current = current.Child;
            }
        }

        public Node<T> GetRoot()
        {
            var current = this;
            while (true)
            {
                if (current.Parent == null)
                {
                    return current;
                }
                current = current.Parent;
            }
        }

        public string GetPath()
        {
            var sb = new StringBuilder();
            var current = this;
            while (true)
            {
                sb.Append(current.Value);
                if (current.Child == null)
                {
                    break;
                }
                sb.Append('/');
                current = current.Child;
            }

            return sb.ToString();
        }
    }
}
