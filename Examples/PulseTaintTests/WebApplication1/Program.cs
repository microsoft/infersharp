
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using subproj;

// Expect 4 TAINT_ERROR for SQL injection flows.
public class PulseTaintTests
{
    [HttpPost]
    static void sqlBadInt(int InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandBad(InputParameter.ToString());
    }

    [HttpPost]
    static void sqlBadString(string InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandBad(InputParameter);
    }

    [HttpPost]
    static void sqlParameterizedOk(int InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandParameterized(InputParameter.ToString());
    }

    [HttpPost]
    static void sqlStoredProcedureOk(string InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandStoredProcedure(InputParameter.ToString());
    }
}