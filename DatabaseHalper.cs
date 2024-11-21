using System;
using System.Data;
using System.Data.SqlClient;

public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Method to execute a non-query command (e.g., INSERT, UPDATE, DELETE)
    public int ExecuteNonQuery(string query, params SqlParameter[] parameters)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }
    }

    // Method to execute a query that returns a single value (e.g., COUNT, MAX)
    public object ExecuteScalar(string query, params SqlParameter[] parameters)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteScalar();
            }
        }
    }

    // Method to execute a query that returns a DataTable
    public DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(command))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return dataTable;
                }
            }
        }
    }

    // Method to execute a stored procedure
    public DataTable ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(command))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return dataTable;
                }
            }
        }
    }
}
