using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBinaryTree
{



    class Program
    {
        static void Main(string[] args)
        {
            MBinTree<int> tree = new MBinTree<int>(8);

            tree.Add(3);
            tree.Add(5);
            tree.Add(1);
            tree.Add(2);
            tree.Add(4);

            tree.Print();
            Console.ReadKey();

        }
    }
}
