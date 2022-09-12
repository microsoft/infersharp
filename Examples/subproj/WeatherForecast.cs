using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;

namespace subproj
{
    public class WeatherForecast
    {
        public static void runSqlCommandBad(string input)
        {
            var command = new SqlCommand()
            {
                CommandText = "SELECT ProductId FROM Products WHERE ProductName = '" + input + "'",
                CommandType = CommandType.Text
            };
        }
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }

        public static void runSqlCommandParameterized(string input)
        {
            var command = new SqlCommand()
            {
                CommandText = "SELECT ProductId FROM Products WHERE ProductName = @productName",
                CommandType = CommandType.Text,
            };
            command.Parameters.Add("@productName", SqlDbType.NVarChar, 128).Value = input;

        }

        public static void runSqlCommandStoredProcedure(string input)
        {
            var command = new SqlCommand()
            {
                CommandText = "sp_GetProductIdFromName",
                CommandType = CommandType.StoredProcedure,

            };
            command.Parameters.Add("@productName", SqlDbType.NVarChar, 128).Value = input;

        }
    }
}
