// ClientPortalBrowserLogin.cs — Automated browser login for Client Portal Gateway

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AutoRevOption.Shared.Portal;

/// <summary>
/// Automates browser login to Client Portal Gateway using headless Chrome
/// Keeps browser session alive in background to avoid repeated 2FA
/// </summary>
public class ClientPortalBrowserLogin : IDisposable
{
    private IWebDriver? _driver;
    private bool _disposed;
    private bool _keepSessionAlive;

    /// <summary>
    /// Perform automated login to Client Portal Gateway
    /// </summary>
    /// <param name="username">IBKR username</param>
    /// <param name="password">IBKR password</param>
    /// <param name="headless">Run browser in headless mode (default: true)</param>
    /// <param name="twoFactorTimeoutMinutes">How long to wait for 2FA approval (default: 2 minutes)</param>
    /// <param name="keepSessionAlive">Keep browser running to maintain session (default: true)</param>
    public async Task<bool> LoginAsync(string username, string password, bool headless = true, int twoFactorTimeoutMinutes = 2, bool keepSessionAlive = true)
    {
        try
        {
            Console.WriteLine("[Browser] Starting automated login...");

            // Configure Chrome options
            var options = new ChromeOptions();
            if (headless)
            {
                options.AddArgument("--headless=new");
            }
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");

            // Create driver
            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            Console.WriteLine("[Browser] Navigating to Client Portal Gateway...");
            _driver.Navigate().GoToUrl("https://localhost:5000");

            // Wait for page to load
            await Task.Delay(2000);

            Console.WriteLine("[Browser] Analyzing page structure...");
            Console.WriteLine($"[Browser] Current URL: {_driver.Url}");
            Console.WriteLine($"[Browser] Page Title: {_driver.Title}");

            // Wait for login page to load
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

            // Try to find username field with multiple selectors
            Console.WriteLine("[Browser] Looking for username field...");
            IWebElement? usernameField = null;

            try
            {
                usernameField = wait.Until(driver =>
                {
                    // Try different possible selectors
                    try { return driver.FindElement(By.Id("user_name")); } catch { }
                    try { return driver.FindElement(By.Id("username")); } catch { }
                    try { return driver.FindElement(By.Name("username")); } catch { }
                    try { return driver.FindElement(By.CssSelector("input[type='text']")); } catch { }
                    try { return driver.FindElement(By.CssSelector("input[name*='user']")); } catch { }
                    return null;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Browser] Could not find username field: {ex.Message}");
                Console.WriteLine("[Browser] Page source (first 1000 chars):");
                var pageSource = _driver.PageSource;
                Console.WriteLine(pageSource.Substring(0, Math.Min(1000, pageSource.Length)));
                throw;
            }

            if (usernameField == null)
            {
                throw new Exception("Username field not found on page");
            }

            Console.WriteLine("[Browser] Found username field, entering username...");
            usernameField.SendKeys(username);

            // Find password field
            Console.WriteLine("[Browser] Looking for password field...");
            IWebElement? passwordField = null;

            try { passwordField = _driver.FindElement(By.Id("password")); } catch { }
            if (passwordField == null)
            {
                try { passwordField = _driver.FindElement(By.Name("password")); } catch { }
            }
            if (passwordField == null)
            {
                try { passwordField = _driver.FindElement(By.CssSelector("input[type='password']")); } catch { }
            }

            if (passwordField == null)
            {
                throw new Exception("Password field not found on page");
            }

            Console.WriteLine("[Browser] Entering password...");
            passwordField.SendKeys(password);

            // Find and click login button
            Console.WriteLine("[Browser] Looking for login button...");
            IWebElement? loginButton = null;

            try { loginButton = _driver.FindElement(By.Id("submitForm")); } catch { }
            if (loginButton == null)
            {
                try { loginButton = _driver.FindElement(By.CssSelector("button[type='submit']")); } catch { }
            }
            if (loginButton == null)
            {
                try { loginButton = _driver.FindElement(By.CssSelector("input[type='submit']")); } catch { }
            }
            if (loginButton == null)
            {
                try { loginButton = _driver.FindElement(By.XPath("//button[contains(text(), 'Log') or contains(text(), 'log')]")); } catch { }
            }

            if (loginButton == null)
            {
                throw new Exception("Login button not found on page");
            }

            Console.WriteLine("[Browser] Clicking login button...");
            loginButton.Click();

            // Wait for 2FA prompt or successful login
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"[Browser] ⚠️  2FA APPROVAL REQUIRED");
            Console.WriteLine($"[Browser] Waiting for 2FA approval (timeout: {twoFactorTimeoutMinutes} minutes)...");
            Console.WriteLine("[Browser] 📱 Please approve the login on your IBKR mobile app");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            // Play system beep to alert user (Windows only)
            try
            {
                Console.Beep(800, 300);  // 800Hz for 300ms
                await Task.Delay(100);
                Console.Beep(1000, 300); // 1000Hz for 300ms
                Console.WriteLine("[Browser] 🔔 Audio alert sent - Check your phone for 2FA notification");
            }
            catch
            {
                Console.WriteLine("[Browser] 🔔 Check your phone for 2FA notification");
            }

            var twoFactorWait = new WebDriverWait(_driver, TimeSpan.FromMinutes(twoFactorTimeoutMinutes));

            try
            {
                // Wait for either:
                // 1. Successful redirect to main portal page
                // 2. Error message
                twoFactorWait.Until(driver =>
                {
                    var currentUrl = driver.Url;

                    // Check if we've been redirected away from login page (success)
                    if (currentUrl.Contains("/sso/Login") == false && currentUrl.Contains("https://localhost:5000"))
                    {
                        return true;
                    }

                    // Check for error messages
                    try
                    {
                        var errorElement = driver.FindElement(By.ClassName("error"));
                        if (!string.IsNullOrEmpty(errorElement.Text))
                        {
                            Console.WriteLine($"[Browser] Error: {errorElement.Text}");
                            return true;
                        }
                    }
                    catch
                    {
                        // No error element found, continue waiting
                    }

                    return false;
                });

                // Check if login was successful
                var finalUrl = _driver.Url;
                if (!finalUrl.Contains("/sso/Login"))
                {
                    Console.WriteLine("[Browser] ✅ Login successful!");

                    // Store flag to keep session alive
                    _keepSessionAlive = keepSessionAlive;

                    if (_keepSessionAlive)
                    {
                        Console.WriteLine("[Browser] 🔒 Keeping browser session alive INDEFINITELY");
                        Console.WriteLine("[Browser]    Session persists indefinitely - no auto-cleanup");
                        Console.WriteLine("[Browser]    Call ResetSession() to force logout and re-authentication");
                    }

                    // Wait for session to establish
                    await Task.Delay(5000);

                    return true;
                }
                else
                {
                    Console.WriteLine("[Browser] ❌ Login failed");
                    return false;
                }
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"[Browser] ❌ 2FA timeout after {twoFactorTimeoutMinutes} minutes");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Browser] Error during login: {ex.Message}");
            return false;
        }
        finally
        {
            // Close browser - session is already established
            // (we already waited in the success path)
        }
    }

    /// <summary>
    /// Check if browser session is still active
    /// </summary>
    public bool IsSessionAlive()
    {
        if (_driver == null || _disposed) return false;

        try
        {
            // Try to access current URL - if this works, session is alive
            _ = _driver.Url;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reset session - forces logout and cleanup
    /// Call this when you want to invalidate the current session and require fresh login with 2FA
    /// </summary>
    public void ResetSession()
    {
        Console.WriteLine("[Browser] 🔄 Resetting session - closing browser and clearing authentication");
        Console.WriteLine("[Browser]    Next login will require fresh 2FA approval");
        Dispose();
        
        // Reset flags to allow fresh login
        _keepSessionAlive = false;
        _disposed = false;
        _driver = null;
    }
 
    /// <summary>
    /// Close browser and end session
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (_driver != null)
            {
                Console.WriteLine("[Browser] Closing browser session...");
                _driver.Quit();
                _driver.Dispose();
            }
        }
        catch
        {
            // Ignore disposal errors
        }

        _disposed = true;
    }
}
