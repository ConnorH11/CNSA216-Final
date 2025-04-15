using Final.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProfilePictureServices.Services;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Final.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProfilePictureService _profilePictureService;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public HomeController(ProfilePictureService profilePictureService, ILogger<HomeController> logger, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _profilePictureService = profilePictureService;
            _logger = logger;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        public async Task<IActionResult> Index()
        {
            string username = User.Identity.Name;
            string presignedUrl = await _profilePictureService.GetProfilePictureUrlAsync(username);
            ViewBag.ProfilePictureUrl = presignedUrl;
            return View();
        }

        public async Task<IActionResult> Rules()
        {
            string username = User.Identity.Name;
            string presignedUrl = await _profilePictureService.GetProfilePictureUrlAsync(username);
            ViewBag.ProfilePictureUrl = presignedUrl;
            return View();
        }

        public async Task<IActionResult> Database(string tableName = "Incident", string searchQuery = null)
        {
            var dataLogger = _loggerFactory.CreateLogger<Data>();
            Data da = new Data(_configuration, dataLogger);
            DataTable dt = da.GetData(tableName);
            _logger.LogInformation("Retrieved {RowCount} rows from table {TableName}", dt.Rows.Count, tableName);
            ViewBag.TableName = tableName;

            if (!string.IsNullOrEmpty(searchQuery))
        {
            var filteredRows = dt.AsEnumerable()
            .Where(row => row.ItemArray.Any(field => field.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase)))
            .CopyToDataTable();
        
            dt = filteredRows; // Use filtered data
        }

            string username = User.Identity.Name;
            string presignedUrl = await _profilePictureService.GetProfilePictureUrlAsync(username);
            ViewBag.ProfilePictureUrl = presignedUrl;

            return View("Database", dt);
    }

        public async Task<IActionResult> Graphs()
        {
            // Get the current user's username once
            string username = User.Identity.Name;

            // Retrieve incident data
            DataTable dt = new DataTable();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        CAST(date_time_received AS DATE) AS incident_date, 
                        COUNT(*) AS incident_count 
                    FROM incident 
                    GROUP BY CAST(date_time_received AS DATE) 
                    ORDER BY incident_date;";
                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        dt.Load(reader);
                    }
                }
            }

            // Convert the DataTable to list of dictionaries
            var incidents = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                incidents.Add(dict);
            }

            // Convert to JSON
            string jsonData = JsonConvert.SerializeObject(incidents);
            ViewBag.IncidentData = jsonData;

            string presignedUrl = await _profilePictureService.GetProfilePictureUrlAsync(username);
            ViewBag.ProfilePictureUrl = presignedUrl;

            return View();
        }

        public IActionResult UserManagement()
        {
            return View();
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> AddRow(string tableName, Dictionary<string, string> rowData = null)
        {
            string username = User.Identity.Name;
            string presignedUrl = await _profilePictureService.GetProfilePictureUrlAsync(username);
            ViewBag.ProfilePictureUrl = presignedUrl;

            ViewBag.TableName = tableName;
            var model = new RowAddModel
            {
                Columns = GetColumnsForTable(tableName)
            };

            if (HttpContext.Request.Method == "POST")
            {
                // Validate the input data
                if (string.IsNullOrEmpty(tableName) || rowData == null || rowData.Count == 0)
                {
                    return BadRequest("Invalid data.");
                }

                // Insert the data into the database
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", rowData.Keys)}) VALUES ({string.Join(", ", rowData.Values.Select(v => $"'{v}'"))})";
                    command.ExecuteNonQuery();
                }

                return RedirectToAction("Database", new { tableName });
            }
            else
            {
                return View(model);
            }
        }

        private List<DataColumn> GetColumnsForTable(string tableName)
        {
            // Return the columns for the Incident table
            return new List<DataColumn>
            {
                new DataColumn { ColumnName = "date_time_received" },
                new DataColumn { ColumnName = "date_time_complete" },
                new DataColumn { ColumnName = "call_type" },
                new DataColumn { ColumnName = "responsible_city" },
                new DataColumn { ColumnName = "responsible_state" },
                new DataColumn { ColumnName = "responsible_zip" },
                new DataColumn { ColumnName = "description_of_incident" },
                new DataColumn { ColumnName = "type_of_incident" },
                new DataColumn { ColumnName = "incident_cause" },
                new DataColumn { ColumnName = "injury_count" },
                new DataColumn { ColumnName = "hospitalization_count" },
                new DataColumn { ColumnName = "fatality_count" },
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ProfilePictureMiddleware
    {
        private readonly RequestDelegate _next;

        public ProfilePictureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var profilePictureUrl = context.Session.GetString("ProfilePictureUrl");
            context.Items["ProfilePictureUrl"] = profilePictureUrl;
            await _next(context);
        }
    }
}