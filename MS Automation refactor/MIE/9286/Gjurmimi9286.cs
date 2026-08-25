using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;

[TestFixture]
public class Gjurmimi_9286
{
    private void Log(string message)
    {
        string logLine = $"{DateTime.Now:HH:mm:ss} | {message}";
        TestContext.Progress.WriteLine(logLine);
        TestContext.Out.WriteLine(logLine);
        Console.WriteLine(logLine);
    }

    private void SaveScreenshot(IWebDriver driver, string artifactsFolder, string namePrefix)
    {
        try
        {
            if (driver is ITakesScreenshot screenshotDriver)
            {
                string filePath = Path.Combine(
                    artifactsFolder,
                    $"{namePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                );

                screenshotDriver.GetScreenshot().SaveAsFile(filePath);
                TestContext.AddTestAttachment(filePath, "Failure Screenshot");
                Log("Screenshot saved: " + filePath);
            }
        }
        catch (Exception ex)
        {
            Log("Screenshot error: " + ex.Message);
        }
    }

    private static string InputValue(IWebElement element) =>
        element.GetAttribute("value")?.Trim() ?? string.Empty;

    [Test]
    public void GjurmoAplikim9286()
    {
        var options = new EdgeOptions();
        options.AddArgument("start-maximized");

        string runTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string testName = TestContext.CurrentContext.Test.Name;
        string artifactsFolder = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "TestArtifacts",
            $"{testName}_{runTime}"
        );

        Directory.CreateDirectory(artifactsFolder);

        Log("===== TEST START =====");
        Log("Artifacts folder: " + artifactsFolder);

        using (IWebDriver driver = new EdgeDriver(options))
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            try
            {
                Log("Open Website");
                driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

                Log("Click 'Test Sherbimesh' button");
                wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("/html/body/div/main/div/div[1]/div/a"))).Click();

                Log("Fill in the form fields");
                driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
                driver.FindElement(By.Id("ServiceCode")).SendKeys("9286");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mie_merge");
                driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
                driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
                driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

                new SelectElement(driver.FindElement(By.Id("ProfileType")))
                    .SelectByValue("Individual");

                new SelectElement(driver.FindElement(By.Id("Platform")))
                    .SelectByValue("WEB");

                Log("Click 'Load Service' button");
                driver.FindElement(By.ClassName("load-button")).Click();

                Log("Click 'Gjurmo' button");
                wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div/div/div/div[2]/div/button"))).Click();

                Thread.Sleep(1000);
                Log("Assert Aplikimet për licencë");
                IWebElement AplikimetPerLicence = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[1]/div/h5"))
                );
                Assert.That(AplikimetPerLicence.Text.Trim(), Is.EqualTo("APLIKIMET PËR LICENCË"));

                

                Log("Search Aplication Number");
                driver.FindElement(By.Id(":r0:")).SendKeys("10748");

                Log("Assert search result");
                IWebElement tipiaplikimit = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/table/tbody/tr/td[2]"))
                );
                Assert.That(tipiaplikimit.Text.Trim(), Is.EqualTo("Individ"));

                IWebElement emri = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/table/tbody/tr/td[3]"))
                );
                Assert.That(emri.Text.Trim(), Is.EqualTo("Licence individuale e shkalles se pare"));

                IWebElement NrAplikimit = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/table/tbody/tr/td[5]"))
                );
                Assert.That(NrAplikimit.Text.Trim(), Is.EqualTo("10748"));

                IWebElement StatusiAplikimit = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/table/tbody/tr/td[6]"))
                );
                Assert.That(StatusiAplikimit.Text.Trim(), Is.EqualTo("Aplikim i ri\r\nKoment: Aplikimi i ri u dergua") );

                Log("Click 'Shkarko' button");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/table/tbody/tr/td[7]/div/button")).Click();


                Log("TEST PASSED");
            }
            catch (Exception ex)
            {
                Log("TEST FAILED: " + ex.Message);
                SaveScreenshot(driver, artifactsFolder, "FAILED");
                throw;
            }
            finally
            {
                Log("===== TEST END =====");
            }
        }
    }
}
