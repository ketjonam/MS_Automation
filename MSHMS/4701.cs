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
public class _4701_
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
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

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
        TestContext.Out.WriteLine(logLine);
        Console.WriteLine(logLine);
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
                TestContext.AddTestAttachment(file, "Failure Screenshot");
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
            TestContext.AddTestAttachment(file, "Failure Page Source");
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

    private void BlurActiveElement()
    {
        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript(
                "if(document.activeElement){document.activeElement.blur();}"
            );
        }
        catch (Exception ex)
        {
            Log("BlurActiveElement error: " + ex.Message);
        }
    }

    private void ClearFilterInput(By locator)
    {
        Log("Clear filter input with Ctrl+A + Delete");
        IWebElement input = wait.Until(ExpectedConditions.ElementIsVisible(locator));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            input
        );

        input.Click();
        Thread.Sleep(300);

        input.SendKeys(Keys.Control + "a");
        Thread.Sleep(200);
        input.SendKeys(Keys.Delete);
        Thread.Sleep(500);

        string currentValue = input.GetAttribute("value") ?? string.Empty;
        Log("Filter value after keyboard clear: '" + currentValue + "'");

        if (!string.IsNullOrEmpty(currentValue))
        {
            Log("Keyboard clear nuk mjaftoi, provoj me JS");
            ((IJavaScriptExecutor)driver).ExecuteScript(@"
                const el = arguments[0];
                el.value = '';
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
            ", input);

            Thread.Sleep(500);
        }

        BlurActiveElement();
        Thread.Sleep(800);

        input = wait.Until(ExpectedConditions.ElementIsVisible(locator));
        currentValue = input.GetAttribute("value") ?? string.Empty;
        Log("Filter value final: '" + currentValue + "'");
    }

    [Test]
    public void KontrolliMjekesorBaze()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div[1]/main/div[3]/div/div/div/div/div/div/div/div/button/div/div[1]/svg";
        string titleXpath = "/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/h4";
        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("G35511058E");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("4435");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("msh-merge-v2");
        driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
        driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
        driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

        new SelectElement(driver.FindElement(By.Id("ProfileType")))
            .SelectByValue("Individual");

        new SelectElement(driver.FindElement(By.Id("Platform")))
            .SelectByValue("WEB");

        Log("Click LOAD SERVICE");
        driver.FindElement(By.ClassName("load-button")).Click();
        Thread.Sleep(5000);

        Log("Mbyll popup mbi te dhenat e mjekut");
        SafeClick(By.XPath("/html/body/div[2]/div/div/div[3]/button"));

        Log("Click Aplikimi i Ri");
        SafeClick(By.XPath(aplikimiRiXpath));
        Thread.Sleep(3000);

        Log("Assert Title");
        IWebElement titleElement = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(titleXpath)));
        Log("Title text: " + titleElement.Text.Trim());
        Assert.That(titleElement.Displayed, Is.True, "Titulli nuk eshte visible");

        Log("kerko per intervist");
        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/div/div[1]/div[2]/div/input")).SendKeys("intervist");

        Log("Assert mesazhin se nuk ka rezultate");
        IWebElement mesazhi = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/div/div[2]/div/div/table/tbody/tr/td")
        ));
        Assert.That(mesazhi.Text.Trim(), Is.EqualTo("Nuk ka rezultate"));

        Log("Clear filter input");
        ClearFilterInput(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/div/div[1]/div[2]/div/input"));

        Log("Shkruaj 'barkodin' ne filter");
        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/div/div[1]/div[2]/div/input")).SendKeys("G0002551675I");

        Log("Assert se ka rezultate");
        IWebElement dataResult = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/div/div[2]/div/div/table/tbody/tr")
        ));
        Assert.That(dataResult.Text.Trim(), Is.Not.EqualTo("Nuk ka rezultate"), "Duhet te kete rezultate per daten 17.12.2022");

        Log("Clear filter input");
        ClearFilterInput(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/div/div[1]/div[2]/div/input"));

        Log("kliko shfaq button");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div/div/div[2]/div/div/table/tbody/tr[1]/td[8]/div/button[1]"));

        Log("TEST PASSED");
    }
}


