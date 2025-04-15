using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Final.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult RegistryPage()
        {
            return View();
        }

        public IActionResult LoginPage()
        {
            return View();
        }

        // Handles account creation 
        [HttpPost]
        public IActionResult CreateAccount()
        {
            string username = Request.Form["username"];
            string password = Request.Form["password"];
            string confirmPassword = Request.Form["confirm-password"];

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View("RegistryPage");
            }

            // Generate a 16-byte salt.
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Convert the password to a byte array and combine it with the salt
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] passwordWithSalt = new byte[passwordBytes.Length + salt.Length];
            Buffer.BlockCopy(passwordBytes, 0, passwordWithSalt, 0, passwordBytes.Length);
            Buffer.BlockCopy(salt, 0, passwordWithSalt, passwordBytes.Length, salt.Length);

            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(passwordWithSalt);
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"INSERT INTO Users (Username, PasswordHash, Salt) 
                                   VALUES (@username, @passwordhash, @salt)";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("username", username);
                        cmd.Parameters.AddWithValue("passwordhash", hash);
                        cmd.Parameters.AddWithValue("salt", salt);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating account: " + ex.Message);
                return View("RegistryPage");
            }

            return RedirectToAction("LoginPage");
        }

        // Handles login form submission.
        [HttpPost]
        public async Task<IActionResult> LoginUser()
        {
            string username = Request.Form["username"];
            string password = Request.Form["password"];

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // Query for the user record by username.
                    string sql = "SELECT PasswordHash, Salt FROM Users WHERE Username = @username";
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ModelState.AddModelError("", "Invalid username or password.");
                                return View("LoginPage");
                            }

                            // Retrieve the stored hash and salt.
                            byte[] storedHash = (byte[])reader["PasswordHash"];
                            byte[] storedSalt = (byte[])reader["Salt"];

                            // Combine the provided password with the stored salt.
                            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                            byte[] passwordWithSalt = new byte[passwordBytes.Length + storedSalt.Length];
                            Buffer.BlockCopy(passwordBytes, 0, passwordWithSalt, 0, passwordBytes.Length);
                            Buffer.BlockCopy(storedSalt, 0, passwordWithSalt, passwordBytes.Length, storedSalt.Length);

                            byte[] computedHash;
                            using (SHA256 sha256 = SHA256.Create())
                            {
                                computedHash = sha256.ComputeHash(passwordWithSalt);
                            }

                            // Compare the computed hash with the stored hash.
                            if (!computedHash.SequenceEqual(storedHash))
                            {
                                ModelState.AddModelError("", "Invalid username or password.");
                                return View("LoginPage");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return View("LoginPage");
            }

            // Create claims for the authenticated user.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                // Optionally set properties such as IsPersistent, ExpiresUtc, etc.
            };

            // Sign in the user.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect to Home (or any protected resource).
            return RedirectToAction("Index", "Home");
        }
    }
}
