using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
    public class NodeList<T>
    {
        public HashSet<NodeList<T>> childs;
        private NodeList<T> parent;
        public T Value { get; set; }

        public HashSet<NodeList<T>> Childs
        {
            get { return childs; }
            set
            {
                childs = value;
                foreach (var child in childs)
                {
                    child.Parent = this;
                }
            }
        }

        public bool HasChild(T child)
        {
            return childs.Any(x => x.Value.Equals(child));
        }

        public NodeList<T> GetChild(T child)
        {
            return childs.FirstOrDefault(x => x.Value.Equals(child));
        }

        public NodeList<T> AddChildNode(NodeList<T> child)
        {
            if (!childs.Contains(child))
            {
                childs.Add(child);
            }
            child.Parent = this;
            return child;
        }

        public bool RemoveChild(NodeList<T> child)
        {
            return childs.Remove(child);
        }

        public void Remove()
        {
            if (parent != null)
            {
                parent.RemoveChild(this);
            }
        }

        public NodeList<T> Parent
        {
            get { return parent; }
            set
            {
                //remove this from old parent 
                if (parent != value)
                {
                    if (parent != null)
                    {
                        parent.RemoveChild(this);
                    }

                    parent = value;
                    if (parent != null)
                    {
                        parent.AddChildNode(this);
                    }
                }
            }
        }

        public NodeList(T value)
        {
            Value = value;
            childs = new HashSet<NodeList<T>>();
            Parent = null;
        }

        public NodeList(T value, NodeList<T> parent)
            : this(value)
        {
            this.Parent = parent;
        }

        public string GetPath()
        {
            var sb = new StringBuilder();
            var current = this;
            while (true)
            {
                sb.Insert(0, "/");
                sb.Insert(0, current.Value);
                if (current.Parent == null)
                {
                    break;
                }
                current = current.Parent;
            }

            return sb.ToString();
        }
    }
}
