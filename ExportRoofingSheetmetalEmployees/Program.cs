using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Configuration;

namespace ExportRoofingSheetmetalEmployees
{
   class Program
   {
      static void Main(string[] args)
      {
         // The array of names to be exported
         var names = new List<String>();

         // Connect to the SQL server
         var connectionString = ConfigurationManager.ConnectionStrings["dottime"].ConnectionString;
         var dataSource = new SqlConnection(connectionString);
         dataSource.Open();

         using (dataSource)
         {
            var sqlText = "select distinct vw_ActiveEmployees.Name from vw_ActiveEmployees, vw_EmployeePolicyGroup where vw_ActiveEmployees.EmployeeGUID = vw_EmployeePolicyGroup.EmployeeID and vw_EmployeePolicyGroup.PolicyDescription in ('Roofing','Sheetmetal')";
            var sqlCommand = new SqlCommand(sqlText, dataSource);

            SqlDataReader sqlReader = sqlCommand.ExecuteReader();

            while (sqlReader.Read())
            {
               String name = sqlReader["Name"].ToString().Trim();
               names.Add(name);
            }
         }

         // Convert the names to a JSON object parsable by the webservice
         var json = JsonConvert.SerializeObject(new { names = names });
         byte[] byteArray = Encoding.UTF8.GetBytes(json);

         WebRequest request = WebRequest.Create("http://apps.advancedroofing.com/webservices/employees-roofers-sheetmetal/create");
         var username = ConfigurationManager.AppSettings["ApiUsername"];
         var password = ConfigurationManager.AppSettings["ApiPassword"];
         request.Credentials = new NetworkCredential(username, password);
         request.ContentType = "application/json";
         request.ContentLength = byteArray.Length;
         request.Method = "POST";

         Stream dataStream = request.GetRequestStream();
         dataStream.Write(byteArray, 0, byteArray.Length);
         dataStream.Close();

         WebResponse response = request.GetResponse();
         Console.WriteLine(((HttpWebResponse)response).StatusDescription);

         dataStream = response.GetResponseStream();
         StreamReader reader = new StreamReader(dataStream);

         Console.WriteLine(reader.ReadToEnd());
      }
   }
}
