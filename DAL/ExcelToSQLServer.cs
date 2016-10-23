﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class ExcelToSQLServer
    {
        public static DataSet ds;
        private static bool CheckExcelTableTeachers()
        {
            try
            {
                string[] str = { "部门", "工号", "密码", "姓名", "性别", "权限" };
                for (int i = 0; i <= 5; i++)
                {
                    if (ds.Tables["ExcelInfo"].Columns[i].ColumnName.ToString() != str[i])
                        return false;
                }
                return true;
            }
            catch {
                return false;
            }
        }
        private static bool CheckExcelTableCalendar()
        {
            try
            {
                string[] str = { "周次", "起", "止" };
                for (int i = 0; i <= 2; i++)
                {
                    if (ds.Tables["ExcelInfo"].Columns[i].ColumnName.ToString() != str[i])
                        return false;
                }
                return true;
            }
            catch {
                return false;
            }

        }
        private static bool CheckExcelTableCourses()
        {
            try
            {
                string[] str = { "承担单位", "任课教师", "上课时间/地点", "课程", "所属部门" };
                for (int i = 0; i <= 4; i++)
                {
                    if (ds.Tables["ExcelInfo"].Columns[i].ColumnName.ToString() != str[i])
                        return false;
                }
                return true;
            }
            catch {
                return false;
            }
        }
        private static void ExcelToDatabaseByDataReader(string fileName, string strSQL)
        {
            System.GC.Collect();
            string oleStr1 = @"Privider = Microsoft.Jet.OLEDB.4.0;Data Source = " + fileName + ";Extended Properties=Excel8.0";
            OleDbConnection oleConn = new OleDbConnection(oleStr1);
            oleConn.Open();

            OleDbCommand oleCmd = new OleDbCommand(strSQL, oleConn);
            OleDbDataReader oleDr = oleCmd.ExecuteReader();

            string sqlStr1 = ConfigurationManager.ConnectionStrings["SdbiAttentionSystemConnectionString"].ConnectionString;
            SqlConnection sqlConn = new SqlConnection(sqlStr1);
            sqlConn.Open();
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.Connection = sqlConn;
            StringBuilder strconn = new StringBuilder();
            List<string> str = new List<string>();
            while (oleDr.Read())
            {
                str = SplitTeacherIDAndTeacherName(oleDr[1].ToString());
                strconn.Append("insert into TabAllCourses(TeacherDapartment,TeacherID,TeacherName,TimeAndArea,Course,t1,t2,t3,Class,StudentDepartment,StudentID,StudentName,t4,t5,t6,t7)values(");
                strconn.Append("'" + oleDr[0].ToString() + "','" + str[0] + "','" + str[1] + "'");
                for (int j = 2; j <= 14; j++)
                {
                    strconn.Append(",'" + oleDr[j].ToString() + "'");
                }
                strconn.Append(")");
                sqlCmd.CommandText = strconn.ToString();
                sqlCmd.ExecuteNonQuery();
                strconn.Remove(0, strconn.Length);
            }


            sqlConn.Close();
            oleDr.Close();
            oleConn.Close();
        }

        private static List<string> GetSheetName(string fileName)
        {
            string str1 = @"Privider = Microsoft.Jet.OLEDB.4.0;Data Source = " + fileName + ";Extended Properties=Excel8.0";
            OleDbConnection conn = new OleDbConnection(str1);
            conn.Open();

            List<string> SheetNameList = new List<string>();
            DataTable dtExcelSchema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null,"TABLE"});
            string SheetName = "";
            for (int i = 0; i < dtExcelSchema.Rows.Count; i++)
            {
                SheetName = dtExcelSchema.Rows[i]["TANLE_NAME"].ToString();
                SheetNameList.Add(SheetName);
            }
            conn.Close();
            conn.Dispose();
            return SheetNameList;
        }
        public static void ReadExcelToDataSet(string fileName, string strSQL)
        {
            string str1 = @"Privider = Microsoft.Jet.OLEDB.4.0;Data Source = " + fileName + ";Extended Properties=Excel8.0";
            OleDbConnection conn = new OleDbConnection(str1);
            conn.Open();
            OleDbDataAdapter da = new OleDbDataAdapter(strSQL, conn);
            da.SelectCommand.CommandTimeout = 600;

            ds = new DataSet();
            da.Fill(ds, "ExcellInfo");
            conn.Close();
            conn.Dispose();
        }
        public static string ReadCoursesExcel(string fileName, string identity)
        {
            List<string> SheetName = new List<string>();
            SheetName = GetSheetName(fileName);
            string strSQL = "";
            if (SheetName[0] != identity + "$")
            {
                return"指定的Excel文件的工作表名不为"+identity+",当前的表名为"+SheetName[0];
            }
            strSQL = "select * from [" + SheetName[0] + "]";
            ReadExcelToDataSet(fileName, strSQL);
            if (CheckExcelTableCourses())
            {
                CoursesTOSQLServer(identity);
                return "文件导入成功";
            }
            else
                return "选择的Excel文件中的内容与数据库要求不匹配。请确认！";
        }
        public static string ReadCalendarExcel(string fileName, string identity)
        {
            List<string> SheetName = new List<string>();
            SheetName = GetSheetName(fileName);
            string strSQL = "";

            if (SheetName[0] != "Sheet1$")
            {
                return "指定的Excel文件的工作表不为“Sheet1”,当前的表名为" + SheetName[0];
            }
            strSQL = "select * from [Sheet1$]";
            ReadExcelToDataSet(fileName, strSQL);

            if (CheckExcelTableCalendar())
            {
                CalenderToSQLServer(identity);
                return "文件导入成功";
            }
            else {
                return "选择的Excel文件中的内容与数据与数据库中的要求不匹配，请确认！";
            }
        }
        public static string ReadTeachersExcel(string fileName, string identity)
        {
            List<string> SheetName = new List<string>();
            SheetName = GetSheetName(fileName);
            string strSQL = "";

            if (SheetName[0] != "Sheet1$")
            {
                return "指定的Excel文件的工作表名不为“Sheet1”,当前的表明为"+SheetName[0];
            }
            strSQL = "select * from [Sheet1$]";
            ReadExcelToDataSet(fileName, strSQL);

            if (CheckExcelTableTeachers())
            {
                TeachersToSQLServer(identity);
                return "文件导入成功";
            }
            else
            {
                return "选择的Excel文件中的内容与数据与数据库中的要求不匹配，请确认！";
            }
        }
        public static void CoursesTOSQLServer(string identity)
        {
            string str1 = ConfigurationManager.ConnectionStrings["SdbiAttentionSystemConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(str1);
            conn.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            StringBuilder strconn = new StringBuilder();
            List<string> str = new List<string>();

            for (int i = 0; i < ds.Tables["ExcelInfo"].Rows.Count; i++)
            {
                str = SplitTeacherIDAndTeacherName(ds.Tables["ExcelInfo"].Rows[i].ItemArray[1].ToString());
                strconn.Append("insert into TabAllCourses(TeacherDapartment,TeacherID,TeacherName,TimeAndArea,Course,t1,t2,t3,Class,StudentDepartment,StudentID,StudentName,t4,t5,t6,t7)values(");
                strconn.Append("'"+ds.Tables["ExcelInfo"].Rows[i].ItemArray[0].ToString() + "','" + str[0] + "','" + str[1] + "'");
                for (int j = 2; j <= 14; j++)
                {
                    strconn.Append(",'" + ds.Tables["ExcelInfo"].Rows[i].ItemArray[j].ToString() +"'");
                }
                strconn.Append(")");
                string str2 = strconn.ToString();
                cmd.CommandText = str2;
                str2 = string.Empty;
                cmd.ExecuteNonQuery();
                strconn.Remove(0, strconn.Length);
                System.GC.Collect();
            }
            conn.Close();
            conn.Dispose();
        }
        private static List<string> SplitTeacherIDAndTeacherName(string str)
        {
            List<string> strSplit = new List<string>();
            string[] newStr = str.Split(new char[]
                { '[',']'}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < newStr.Length; i++)
            {
                strSplit.Add(newStr[i]);
            }
            return strSplit;
        }
        public static void CalenderToSQLServer(string identity)
        {
            string str1 = ConfigurationManager.ConnectionStrings["SdbiAttentionSystemConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(str1);
            conn.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            StringBuilder strconn = new StringBuilder();
            for (int i = 0; i < ds.Tables["ExcelInfo"].Rows.Count; i++)
            {
                strconn.Append("insert into"+identity+"(WeekNumber,StartWeek,EndWeek)values(");
                for (int j = 0; j <= 1; j++)
                {
                    strconn.Append("'" + ds.Tables["ExcelInfo"].Rows[i].ItemArray[j].ToString() + "',");
                }
                strconn.Append("'"+ds.Tables["ExcelInfo"].Rows[i].ItemArray[2]+"')");
                string str2 = strconn.ToString();
                cmd.CommandText = str2;
                cmd.ExecuteNonQuery();
                strconn.Remove(0, strconn.Length);
            }
            conn.Close();
            conn.Dispose();
        }
        public static void TeachersToSQLServer(string identity)
        {
            string str1 = ConfigurationManager.ConnectionStrings["SdbiAttentionSystemConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(str1);
            conn.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            StringBuilder strconn = new StringBuilder();
            for (int i = 0; i < ds.Tables["ExcelInfo"].Rows.Count; i++)
            {
                strconn.Append("insert into"+identity+"(Department,UserID,UserPWD,Sex,Role)values(");
                for (int j = 0; j <=4; j++)
                {
                    strconn.Append("'" + ds.Tables["ExcelInfo"].Rows[i].ItemArray[j].ToString() + "',");
                }
                strconn.Append("'" + ds.Tables["ExcelInfo"].Rows[i].ItemArray[5] + "')");
                string str2 = strconn.ToString();
                cmd.CommandText = str2;
                cmd.ExecuteNonQuery();
                strconn.Remove(0, strconn.Length);
            }
            conn.Close();
            conn.Dispose();
        }
    }
}
