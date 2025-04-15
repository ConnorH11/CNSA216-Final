using System;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
public class RowAddModel
{
    public List<DataColumn> Columns { get; set; }
}

public class Data
{
    private readonly string connectionString;
    private readonly ILogger<Data> _logger;

    public Data(IConfiguration configuration, ILogger<Data> logger)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public DataTable GetData(string tableName)
    {
        DataTable dt = new DataTable();
        try
        {
            using (NpgsqlConnection con = new NpgsqlConnection(connectionString))
            {
                string query = $"SELECT * FROM {tableName}";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
                {
                    using (NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd))
                    {
                        con.Open();
                        sda.Fill(dt);
                        _logger.LogInformation("Data retrieved successfully from table {TableName}", tableName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from table {TableName}", tableName);
        }
        return dt;
    }
}