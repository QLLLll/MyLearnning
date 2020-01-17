using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
namespace TestSqlCon
{
    public  class Test
    {

        public static void test()
        {
            
            //先打开两个类库文件
            SqlConnection con = new SqlConnection();

            // con.ConnectionString = "server=505-03;database=ttt;user=sa;pwd=123";
            con.ConnectionString = "server=.;database=Learnning;uid=sa;pwd=940619.lq";
            con.Open();

            /*
            SqlDataAdapter 对象。 用于填充DataSet （数据集）。
            SqlDataReader 对象。 从数据库中读取流..
            后面要做增删改查还需要用到 DataSet 对象。
            */

            SqlCommand com = new SqlCommand();
            com.Connection = con;
            com.CommandType = CommandType.Text;
            com.CommandText = "select * from tbl_UserKq";
            SqlDataReader dr = com.ExecuteReader();//执行SQL语句
            dr.Read();

            Console.WriteLine($"{dr["UserName"]}");
            
            dr.Close();//关闭执行
            con.Close();//关闭数据库
        }


    }
}
