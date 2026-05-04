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
public class _5016_
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
    public void GjendjaAktiveeMjetit()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";
        string titleXpath = "/html/body/div/main/div[3]/div/div/div/div/h4";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J25730113W");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("5016");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("dpshtrr-merge-not-ams");
        driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
        driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
        driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

        new SelectElement(driver.FindElement(By.Id("ProfileType")))
            .SelectByValue("Individual");

        new SelectElement(driver.FindElement(By.Id("Platform")))
            .SelectByValue("WEB");

        Log("Click LOAD SERVICE");
        driver.FindElement(By.ClassName("load-button")).Click();
        Thread.Sleep(3000);

        Log("Click Aplikimi i Ri");
        SafeClick(By.XPath(aplikimiRiXpath));
        Thread.Sleep(3000);

        Log("Assert Title");
        IWebElement titleElement = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(titleXpath)));
        Log("Title text: " + titleElement.Text.Trim());
        Assert.That(titleElement.Displayed, Is.True, "Titulli nuk eshte visible");


        Log("Zgjidh automjetin");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div/div[1]/div/table/tbody/tr/td[1]/button")).Click();

        Log("Assert gjendja e mjetit");

        IWebElement gjendjaSection = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//div[@class='mt-5' and .//h6[contains(.,'Gjendja e mjetit me targë:')]]")
        ));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            gjendjaSection
        );

        Thread.Sleep(500);

        Log("Assert titulli i seksionit");

        IWebElement TitulliSeksionit = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//div[@class='mt-5' and .//h6[contains(.,'Gjendja e mjetit me targë:')]]//h6[contains(.,'Gjendja e mjetit me targë:')]")
        ));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            TitulliSeksionit
        );

        Thread.Sleep(500);

        Log("Title text: " + TitulliSeksionit.Text.Trim());
        Assert.That(TitulliSeksionit.Text.Trim(), Does.Contain("Gjendja e mjetit me targë:"));
        Assert.That(TitulliSeksionit.Text.Trim(), Does.Contain("AB166DP"));

        Log("Assert header row");
        IWebElement headerRow = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//div[@class='mt-5' and .//h6[contains(.,'Gjendja e mjetit me targë:')]]//table/thead/tr")
        ));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            headerRow
        );

        Thread.Sleep(500);

        var headerCells = headerRow.FindElements(By.XPath("./th"));

        string[] expectedHeaders = new string[]
        {
    "Kartela",
    "Data e lejes",
    "Statusi i mjetit"
        };

        Log($"Numri i header cells: {headerCells.Count}");
        Assert.That(headerCells.Count, Is.EqualTo(expectedHeaders.Length),
            $"Numri i header-ave nuk përputhet. Actual: {headerCells.Count}, Expected: {expectedHeaders.Length}");

        for (int i = 0; i < expectedHeaders.Length; i++)
        {
            string actual = headerCells[i].Text.Trim();
            string expected = expectedHeaders[i];

            Log($"Header[{i}] -> Actual: '{actual}' | Expected: '{expected}'");
            Assert.That(actual, Is.EqualTo(expected), $"Header mismatch në kolonën {i}");
        }

        Log("Assert data row");
        IWebElement dataRow = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//div[@class='mt-5' and .//h6[contains(.,'Gjendja e mjetit me targë:')]]//table/tbody/tr")
        ));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            dataRow
        );

        Thread.Sleep(500);

        var dataCells = dataRow.FindElements(By.XPath("./td"));

        string[] expectedRow = new string[]
        {
    "DRD00857221",
    "17.06.2021",
    ""
        };

        Log($"Numri i data cells: {dataCells.Count}");
        Assert.That(dataCells.Count, Is.EqualTo(expectedRow.Length),
            $"Numri i kolonave nuk përputhet. Actual: {dataCells.Count}, Expected: {expectedRow.Length}");

        for (int i = 0; i < expectedRow.Length; i++)
        {
            string actual = dataCells[i].Text.Trim();
            string expected = expectedRow[i];

            Log($"Cell[{i}] -> Actual: '{actual}' | Expected: '{expected}'");
            Assert.That(actual, Is.EqualTo(expected), $"Cell mismatch në kolonën {i}");
        }

        Log("Assert 'Gjendje e TVMP'");
        IWebElement tvmpStatus = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("tvmpStatus")));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            tvmpStatus
        );
        Thread.Sleep(300);
        Log("tvmpStatus value: " + (tvmpStatus.GetAttribute("value") ?? ""));
        Assert.That(tvmpStatus.GetAttribute("value") ?? "", Is.EqualTo("Mjeti nuk ka detyrime ne taksa"));

        Log("Assert 'Gjendja e gjobës KTV'");
        IWebElement ktvFine = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("ktvFine")));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            ktvFine
        );
        Thread.Sleep(300);
        Log("ktvFine value: " + (ktvFine.GetAttribute("value") ?? ""));
        Assert.That(ktvFine.GetAttribute("value") ?? "", Is.EqualTo("Mjeti nuk ka detyrime ne gjoba"));

        Log("Assert 'Gjendja e gjobave të kontrollit në rrugë'");
        IWebElement roadFine = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("roadFine")));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            roadFine
        );
        Thread.Sleep(300);
        Log("roadFine value: " + (roadFine.GetAttribute("value") ?? ""));
        Assert.That(roadFine.GetAttribute("value") ?? "", Is.EqualTo("Mjeti nuk ka detyrime ne gjoba"));

        Log("Assert 'Gjendja e dosjes'");
        IWebElement fileStatus = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("fileStatus")));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            fileStatus
        );
        Thread.Sleep(300);
        Log("fileStatus value: " + (fileStatus.GetAttribute("value") ?? ""));
        Assert.That(fileStatus.GetAttribute("value") ?? "", Is.EqualTo("Mjeti nuk ka bllokime"));

        Log("Assert 'Statusi'");
        IWebElement vehicleStatus = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("vehicleStatus")));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            vehicleStatus
        );
        Thread.Sleep(300);
        Log("vehicleStatus value: '" + (vehicleStatus.GetAttribute("value") ?? "") + "'");
        Assert.That(vehicleStatus.GetAttribute("value") ?? "", Is.EqualTo(""));

        Log("Assert butoni 'Shkarko dokumentin e vulosur'");
        IWebElement downloadButton = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//div[@class='mt-5' and .//h6[contains(.,'Gjendja e mjetit me targë:')]]//button[contains(.,'Shkarko dokumentin e vulosur')]")
        ));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            downloadButton
        );

        Thread.Sleep(500);

        Log("Download button text: " + downloadButton.Text.Trim());
        Assert.That(downloadButton.Displayed, Is.True, "Butoni 'Shkarko dokumentin e vulosur' nuk eshte visible");
        Assert.That(downloadButton.Text.Trim(), Does.Contain("Shkarko dokumentin e vulosur"));

        Log("TEST PASSED");
    }
}

