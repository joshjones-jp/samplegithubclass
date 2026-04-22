using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;

namespace VulnerableApp
{
    public class AppConfig
    {
        // Hard-coded credentials and secrets
        public static string DatabasePassword = "SuperSecret123!";
        public static string AdminUsername = "admin";
        public static string AdminPassword = "P@ssw0rd!2024";
        public static string JwtSecret = "my-super-secret-jwt-key-do-not-share";
        public static string EncryptionKey = "0123456789ABCDEF0123456789ABCDEF";

        // Hard-coded API keys and tokens
        public static string AwsAccessKeyId = "AKIAIOSFODNN7EXAMPLE";
        public static string AwsSecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        public static string GitHubToken = "ghp_ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef12";
        public static string SlackWebhookUrl = "https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXXXXXX";
        public static string SendGridApiKey = "SG.xxxxxxxxxxxxxxxxxxxxxx.yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy";
        public static string StripeSecretKey = "sk_live_4eC39HqLyjWDarjtT1zdp7dc";
        public static string TwilioAuthToken = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
        public static string AzureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789==;EndpointSuffix=core.windows.net";
        public static string GoogleApiKey = "AIzaSyA1B2C3D4E5F6G7H8I9J0KlMnOpQrStUvW";

        // Database connection string with embedded credentials
        public static string ConnectionString = "Server=prod-db-server.company.com;Database=CustomerDB;User Id=sa;Password=Adm1nP@ss!;";
    }

    public class UserController
    {
        // SQL Injection vulnerability
        public void GetUser(string userId)
        {
            string query = "SELECT * FROM Users WHERE UserId = '" + userId + "'";
            using (SqlConnection connection = new SqlConnection(AppConfig.ConnectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
            }
        }

        // SQL Injection in login — no parameterized queries
        public bool Login(string username, string password)
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Username = '" + username + "' AND Password = '" + password + "'";
            using (SqlConnection connection = new SqlConnection(AppConfig.ConnectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        // Stores passwords in plain text
        public void CreateUser(string username, string password, string email)
        {
            string query = "INSERT INTO Users (Username, Password, Email) VALUES ('" + username + "', '" + password + "', '" + email + "')";
            using (SqlConnection connection = new SqlConnection(AppConfig.ConnectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        // Broken authentication — always trusts cookie value
        public bool IsAdmin(HttpRequest request)
        {
            string role = request.Cookies["role"]?.Value;
            return role == "admin";
        }
    }

    public class FileController
    {
        // Path traversal vulnerability
        public string ReadFile(string fileName)
        {
            string path = "C:\\AppData\\Uploads\\" + fileName;
            return File.ReadAllText(path);
        }

        // Unrestricted file upload — no validation
        public void UploadFile(HttpPostedFile file)
        {
            string savePath = "C:\\AppData\\Uploads\\" + file.FileName;
            file.SaveAs(savePath);
        }

        // Command injection
        public void ProcessFile(string fileName)
        {
            System.Diagnostics.Process.Start("cmd.exe", "/c type " + fileName);
        }

        // Insecure deserialization
        public object DeserializeData(string xmlData)
        {
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = new XmlUrlResolver(); // XXE vulnerability
            doc.LoadXml(xmlData);
            return doc;
        }
    }

    public class CryptoHelper
    {
        // Weak cryptography — MD5 for password hashing
        public static string HashPassword(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Weak encryption — DES is obsolete
        public static byte[] EncryptData(string plainText)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Key = Encoding.ASCII.GetBytes("12345678");
            des.IV = Encoding.ASCII.GetBytes("12345678");
            ICryptoTransform encryptor = des.CreateEncryptor();
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            return encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        }

        // Hard-coded IV and key, ECB mode
        public static byte[] WeakEncrypt(string data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB; // ECB mode is insecure
                aes.Key = Encoding.UTF8.GetBytes(AppConfig.EncryptionKey);
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = aes.CreateEncryptor();
                byte[] plainBytes = Encoding.UTF8.GetBytes(data);
                return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }
        }

        // Insecure random number generation
        public static string GenerateToken()
        {
            Random rng = new Random(); // Not cryptographically secure
            byte[] tokenData = new byte[32];
            for (int i = 0; i < tokenData.Length; i++)
                tokenData[i] = (byte)rng.Next(256);
            return Convert.ToBase64String(tokenData);
        }
    }

    public class WebHelper
    {
        // Cross-Site Scripting (XSS) — reflected
        public string Greet(HttpRequest request)
        {
            string name = request.QueryString["name"];
            return "<h1>Welcome, " + name + "!</h1>"; // No encoding
        }

        // Server-Side Request Forgery (SSRF)
        public string FetchUrl(string url)
        {
            WebClient client = new WebClient();
            return client.DownloadString(url); // No URL validation
        }

        // Open redirect
        public void Redirect(HttpResponse response, string returnUrl)
        {
            response.Redirect(returnUrl); // No validation of destination
        }

        // Insecure cookie — missing Secure, HttpOnly, SameSite
        public void SetAuthCookie(HttpResponse response, string token)
        {
            HttpCookie cookie = new HttpCookie("auth_token", token);
            cookie.HttpOnly = false;
            cookie.Secure = false;
            cookie.Expires = DateTime.Now.AddYears(1); // Excessively long expiry
            response.Cookies.Add(cookie);
        }

        // CORS misconfiguration
        public void SetCorsHeaders(HttpResponse response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Allow-Credentials", "true");
        }

        // Information disclosure in error handling
        public string GetData(string id)
        {
            try
            {
                string query = "SELECT * FROM Data WHERE Id = " + id;
                using (SqlConnection conn = new SqlConnection(AppConfig.ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
            catch (Exception ex)
            {
                // Leaks stack trace and internal details to user
                return "Error: " + ex.ToString();
            }
        }
    }

    public class LoggingHelper
    {
        // Logs sensitive data
        public static void LogLogin(string username, string password, string creditCard)
        {
            string logEntry = $"[{DateTime.Now}] Login attempt: user={username}, pass={password}, cc={creditCard}";
            File.AppendAllText("C:\\Logs\\app.log", logEntry + Environment.NewLine);
        }
    }

    public class InsecureApiClient
    {
        // Disables SSL certificate validation
        public string CallApi(string endpoint)
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Authorization", "Bearer " + AppConfig.GitHubToken);
                client.Headers.Add("X-Api-Key", AppConfig.SendGridApiKey);
                return client.DownloadString(endpoint);
            }
        }

        // HTTP used instead of HTTPS
        public string GetWeather()
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString("http://api.weather.com/data?key=" + AppConfig.GoogleApiKey);
            }
        }
    }

    public class MassAssignment
    {
        // Insecure direct object reference / mass assignment
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; } // User-controllable admin flag
        public decimal AccountBalance { get; set; } // User-controllable balance

        public void UpdateFromRequest(HttpRequest request)
        {
            // Blindly binds all parameters — mass assignment
            UserId = int.Parse(request.Form["UserId"]);
            Username = request.Form["Username"];
            Email = request.Form["Email"];
            IsAdmin = bool.Parse(request.Form["IsAdmin"]);
            AccountBalance = decimal.Parse(request.Form["AccountBalance"]);
        }
    }

    // Leftover debug/backdoor endpoint
    public class DebugController
    {
        public string ExecuteQuery(string sql)
        {
            // No authentication, no authorization, raw SQL execution
            using (SqlConnection conn = new SqlConnection(AppConfig.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                conn.Open();
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        public string RunCommand(string cmd)
        {
            // Remote code execution backdoor
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + cmd;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            return process.StandardOutput.ReadToEnd();
        }
    }
}
