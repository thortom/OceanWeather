using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data.SqlClient;

/*
 * API for the weather database connection.
 * This should be used for accessing the data.
 */
public class DatabaseConnector
{
    string connectionString, database, databaseTable;

	public DatabaseConnector()
	{
        // TODO: This should not be hard coded here
        database = "testDB";
        databaseTable = "testDB.dbo.OceanWeatherData";
        connectionString = String.Format("Server= localhost; Database={0}; Integrated Security=SSPI;", database);
    }


    private void getElements(SqlConnection connection, string month, string data_type)
    {
        /*
         * SELECT TOP 10 *
         * FROM testDB.dbo.OceanWeatherData
         * WHERE time_stamp LIKE '2011-01-01%' and data_type LIKE 'dp'
         */
        string statement = String.Format("SELECT * FROM {0} WHERE time_stamp LIKE '{1}%' AND data_type LIKE '{2}'", databaseTable, month, data_type);
        using (SqlCommand command = new SqlCommand(statement, connection))
        {
            command.ExecuteNonQuery();
        }
    }
}