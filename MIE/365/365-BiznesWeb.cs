using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;

[TestFixture]
public class BiznesWeb365
{
    private IWebDriver driver;
    private WebDriverWait wait;
    private string artifactsFolder;

    [SetUp]
    public void Setup()
    {
        var options = new EdgeOptions();
        options.AddArgument("start-maximized");

        driver = new EdgeDriver(options);
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        string runTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string testName = TestContext.CurrentContext.Test.Name;

        artifactsFolder = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "TestArtifacts",
            $"{testName}_{runTime}"
        );

        Directory.CreateDirectory(artifactsFolder);

        Log("===== TEST START =====");
        Log($"Test: {testName}");
        Log($"Artifacts folder: {artifactsFolder}");
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            Log($"Test status: {status}");

            if (status == TestStatus.Failed)
            {
                SaveScreenshot("FAILED");
                SavePageSource("FAILED");
            }
        }
        catch (Exception ex)
        {
            Log("TearDown error: " + ex.Message);
        }
        finally
        {
            try
            {
                driver?.Quit();
                driver?.Dispose();
            }
            catch (Exception ex)
            {
                Log("Driver dispose error: " + ex.Message);
            }

            Log("===== TEST END =====");
        }
    }

    private void Log(string message)
    {
        string logLine = $"{DateTime.Now:HH:mm:ss} | {message}";
        TestContext.Progress.WriteLine(logLine);
    }

    private void SaveScreenshot(string name)
    {
        try
        {
            if (driver is ITakesScreenshot screenshotDriver)
            {
                string file = Path.Combine(
                    artifactsFolder,
                    $"{name}_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                );

                screenshotDriver.GetScreenshot().SaveAsFile(file);
                TestContext.AddTestAttachment(file);
                Log("Screenshot saved: " + file);
            }
        }
        catch (Exception ex)
        {
            Log("Screenshot error: " + ex.Message);
        }
    }

    private void SavePageSource(string name)
    {
        try
        {
            string file = Path.Combine(
                artifactsFolder,
                $"{name}_PageSource_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            );

            File.WriteAllText(file, driver.PageSource);
            TestContext.AddTestAttachment(file);
            Log("PageSource saved: " + file);
        }
        catch (Exception ex)
        {
            Log("PageSource error: " + ex.Message);
        }
    }

    private void SafeClick(By locator)
    {
        IWebElement element = wait.Until(ExpectedConditions.ElementExists(locator));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            element
        );

        Thread.Sleep(500);

        try
        {
            element = wait.Until(ExpectedConditions.ElementToBeClickable(locator));
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            element = driver.FindElement(locator);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }
    }

    [Test]
    public void NIPTWeb365()
    {
        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("/html/body/div/main/div/div[1]/div/a"))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("365");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("mieinstitution-mie-institution-1");
        driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
        driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
        driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

        new SelectElement(driver.FindElement(By.Id("ProfileType")))
            .SelectByValue("Organisation");

        new SelectElement(driver.FindElement(By.Id("Platform")))
            .SelectByValue("WEB");

        Log("Click LOAD SERVICE");
        driver.FindElement(By.ClassName("load-button")).Click();
        Thread.Sleep(3000);

        Log("Click Aplikimi i Ri");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div"));

        Log("Click Afisho pa kontrate");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/div[2]/div[2]/div/button"));

        Log("Assert Kujdes modal");
        IWebElement alertModal = wait.Until(
            ExpectedConditions.ElementIsVisible(By.CssSelector(".alert-modal-container"))
        );
        Assert.That(alertModal.Displayed, Is.True, "Alert modal nuk u shfaq.");

        IWebElement alertTitle = driver.FindElement(By.CssSelector(".alert-modal-title"));
        Assert.That(alertTitle.Text, Is.EqualTo("Kujdes!"));

        IWebElement alertDescription = driver.FindElement(By.CssSelector(".alert-modal-description"));
        Assert.That(alertDescription.Text, Does.Contain("Plotësoni fushën"));

        Log("Close Kujdes modal");
        SafeClick(By.CssSelector(".alert-modal-button--primary"));

        wait.Until(ExpectedConditions.InvisibilityOfElementLocated(
            By.CssSelector(".alert-modal-container")));

        Log("Insert contract");
        driver.FindElement(By.Id("contractNumber")).SendKeys("189915-1");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/div[2]/div[2]/div/button"));

        Log("Assert table row");
        IWebElement row = wait.Until(
            ExpectedConditions.ElementExists(By.XPath("//table/tbody/tr"))
        );

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            row
        );

        Assert.That(row.Displayed, Is.True, "Rreshti i tabelës nuk u shfaq.");

        Thread.Sleep(2000);

        Log("Click Ruaj kontraten");
        SafeClick(By.CssSelector(".btn.btn-outline-secondary.px-3.py-2"));

        Thread.Sleep(500);

        Log("Fill save contract popup");
        driver.FindElement(By.Id("contractNumber")).SendKeys("189915-1");
        driver.FindElement(By.Id("description")).SendKeys("Test Automation");
        SafeClick(By.XPath("//button[normalize-space()='Ruaj']"));

        Thread.Sleep(2000);

        Log("Assert success modal");
        IWebElement successModal = wait.Until(
            ExpectedConditions.ElementIsVisible(By.CssSelector(".alert-modal-container"))
        );
        Assert.That(successModal.Displayed, Is.True, "Success modal nuk u shfaq.");

        IWebElement successTitle = driver.FindElement(By.CssSelector(".alert-modal-title"));
        Assert.That(successTitle.Text, Is.EqualTo("Sukses"));

        IWebElement successDescription = driver.FindElement(By.CssSelector(".alert-modal-description"));
        Assert.That(successDescription.Text, Is.EqualTo("Shtimi u krye me sukses"));

        Log("Close success modal");
        SafeClick(By.CssSelector(".alert-modal-button--primary"));

        wait.Until(ExpectedConditions.InvisibilityOfElementLocated(
            By.CssSelector(".alert-modal-container")));

        Log("Select existing contract");
        SafeClick(By.Id("existingContract"));

        Thread.Sleep(2000);

        Log("Choose existing contract from dropdown");
        SelectElement contractSelect = new SelectElement(
            wait.Until(ExpectedConditions.ElementExists(By.Id("contractSelect")))
        );
        contractSelect.SelectByValue("01234325235");

        Log("Click Afisho for existing contract");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div/div[2]/div[2]/div[2]/div/button"));

        Log("Assert error message");
        IWebElement errorMessage = wait.Until(
            ExpectedConditions.ElementIsVisible(By.CssSelector("p.text-danger"))
        );

        Assert.That(errorMessage.Displayed, Is.True, "Mesazhi i gabimit nuk u shfaq.");
        Assert.That(
            errorMessage.Text,
            Is.EqualTo("Kodi i klientit nuk ekziston. Ju lutemi vendosni kodin e saktë të klientit")
        );

        Log("TEST PASSED");
    }
}