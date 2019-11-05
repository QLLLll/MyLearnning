using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBinaryTree
{
    public class MBinTree<T> where T : IComparable
    {
        private MBinTree<T> Left;
        private MBinTree<T> Right;
        private MBinTree<T> Root;

        private T data;

        public MBinTree()
        {

        }

        public MBinTree(T v)
        {

            if (Root == null)
            {
                this.Root = new MBinTree<T>();
                this.Root.data = v;
            }
        }


        public void AddNode(MBinTree<T> node)
        {
            
                if (node.data.CompareTo(this.data) < 0)

                {
                    if (this.Left!=null)
                    {
                        this.Left.AddNode(node);

                    }
                    else
                    {
                        this.Left = node;
                    }
                }
                else
                {
                    if (Right != null)
                    {
                       
                            this.Right.AddNode(node);
                    }
                    else
                    {
                        this.Right = node;
                    }
                }
            

        }

        private void PrintNode()
        {
            if (this.Left != null)
            {
                this.Left.PrintNode();
            }
            Console.WriteLine(this.data+" ");
            if (this.Right != null)
            {
                this.Right.PrintNode();
            }
        }

        public void Print()
        {
            this.Root.PrintNode();
        }

        public void Add( T v)
        {

            MBinTree<T> node = new MBinTree<T>();

            node.data = v;
                this.Root.AddNode(node);
            
        }

    }
}
