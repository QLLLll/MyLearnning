using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace ConsoleAppReadHatch
{
    class Program
    {
        static void Main(string[] args)
        {
            //string s=File.ReadAllText(Directory.GetCurrentDirectory() + "\\acad.pat");

            StreamReader sr = new StreamReader(File.OpenRead(Directory.GetCurrentDirectory() + "\\acad.pat"),Encoding.Default);

            StringBuilder sb = new StringBuilder();

            string  str = string.Empty;
            str = sr.ReadLine();

            int i = 1;
            int m = 0;
            while (!string.IsNullOrEmpty(str)||m==0)
            {

                if (!str.StartsWith("*"))
                {
                    str = sr.ReadLine();
                    if (str.IndexOf("*") == 0)
                    {
                        m=1;
                    }
                    continue;
                }
                m = 1;
                int start = str.IndexOf("*");
                int end = str.IndexOf(",");

                if (i++ % 5 == 0)
                {
                    sb.Append("\n");
                }
                if(start>=0&&end!=-1)
                sb.Append(str.Substring(start+1, end-1)+" ");

                str = sr.ReadLine();

            }
            sr.Close();

            Console.WriteLine(sb.ToString());

            File.WriteAllText(Directory.GetCurrentDirectory() + "\\a.txt", sb.ToString());

            Console.ReadKey();
        }
    }
}
