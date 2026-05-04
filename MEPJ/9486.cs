using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

[TestFixture]
public class _9486_
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

    private void WaitUntilOptionExists(By selectLocator, string optionValue)
    {
        wait.Until(driver =>
        {
            try
            {
                var selectElement = new SelectElement(driver.FindElement(selectLocator));
                return selectElement.Options.Any(o =>
                    string.Equals(
                        (o.GetAttribute("value") ?? string.Empty).Trim(),
                        optionValue,
                        StringComparison.OrdinalIgnoreCase
                    ));
            }
            catch
            {
                return false;
            }
        });
    }

    private void SelectByValueSafe(By selectLocator, string optionValue)
    {
        WaitUntilOptionExists(selectLocator, optionValue);

        IWebElement dropdown = wait.Until(ExpectedConditions.ElementIsVisible(selectLocator));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            dropdown
        );

        Thread.Sleep(500);

        var select = new SelectElement(dropdown);

        Log($"Po zgjedh value '{optionValue}' tek {selectLocator}");
        foreach (var option in select.Options)
        {
            Log($"Option Text = '{option.Text.Trim()}', Value = '{option.GetAttribute("value")}'");
        }

        select.SelectByValue(optionValue);
        Thread.Sleep(1000);
    }

    private void RemoveAllUploadedDocs()
    {
        Log("Hiq dok jo te sakta");

        int safetyCounter = 0;

        while (true)
        {
            var deleteButtons = driver.FindElements(By.CssSelector("button[aria-label='Delete file']"));

            Log("Nr. i butonave Delete file: " + deleteButtons.Count);

            var deleteBtn = deleteButtons.FirstOrDefault(b =>
            {
                try
                {
                    return b.Displayed && b.Enabled;
                }
                catch
                {
                    return false;
                }
            });

            if (deleteBtn == null)
            {
                Log("Nuk ka me dokumente per te hequr");
                break;
            }

            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({ block: 'center' });",
                    deleteBtn
                );

                Thread.Sleep(300);

                try
                {
                    deleteBtn.Click();
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", deleteBtn);
                }

                Log("U hoq nje dokument jo i sakte");
                Thread.Sleep(1000);
            }
            catch (StaleElementReferenceException)
            {
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                Log("Gabim gjate heqjes se dokumentit: " + ex.Message);
                break;
            }

            safetyCounter++;
            if (safetyCounter >= 10)
            {
                Log("Ndalo heqjen e dokumenteve per shkak te safetyCounter");
                break;
            }
        }

        Log("Te gjitha dok jo te sakta u hoqen");
    }

    [Test]
    public void VertetimCertifikateMartese()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/div[1]/div/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("9486");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("mepj");
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

        Log("Assert Step 1 Title");
        IWebElement step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(step1Title.Text.Trim(), Is.EqualTo("TË DHËNA PERSONALE TË APLIKANTIT"));
        Thread.Sleep(1000);

        Log("Assert te dhenat e aplikantit");
        IWebElement NID = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nid")));
        Assert.That(NID.GetAttribute("value").Trim(), Is.EqualTo("J55728107R"));
        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("emri")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));
        IWebElement Atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("atesia")));
        Assert.That(Atesia.GetAttribute("value").Trim(), Is.EqualTo("Mersin"));
        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("mbiemri")));
        Assert.That(Mbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));
        IWebElement Datelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("datelindja")));
        Assert.That(Datelindja.GetAttribute("value").Trim(), Is.EqualTo("28.07.1995"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert mesazhin per fushat e detyrueshme");
        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[1]/div[9]/div")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Name("vendlindjaShteti"))).SelectByValue("1");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("ADRESA E APLIKANTIT (NË VENDIN E REZIDENCËS)"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorReqField = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[1]/div[2]/span")));
        Assert.That(msgErrorReqField.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Name("shteti"))).SelectByValue("2");
        driver.FindElement(By.Name("rruga")).SendKeys("test");
        driver.FindElement(By.Name("qyteti")).SendKeys("test");
        driver.FindElement(By.Name("kodiPostar")).SendKeys("1001");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("KONTAKTI"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert te dhenat e kontaktit");
        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));
        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("phoneNumber")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Name("country"))).SelectByValue("Emiratet e Bashkuara Arabe");
        Thread.Sleep(500);
        new SelectElement(driver.FindElement(By.Name("consularOffice"))).SelectByValue("1");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(3000); 

        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("TË DHËNAT E BASHKËSHORTIT"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorSpouse = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[1]/span")));
        Assert.That(msgErrorSpouse.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.Name("firstName")).SendKeys("test");
        driver.FindElement(By.Name("lastName")).SendKeys("test");
        driver.FindElement(By.Name("birthDate")).SendKeys("10.04.2026");
        driver.FindElement(By.Name("birthPlace")).SendKeys("test");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step5 title");
        IWebElement Step5Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step5Title.Text.Trim(), Is.EqualTo("TË DHËNAT E BASHKËSHORTES"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorSpouse2 = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[2]/span")));
        Assert.That(msgErrorSpouse2.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.Name("firstName")).SendKeys("test");
        driver.FindElement(By.Name("lastName")).SendKeys("test");
        driver.FindElement(By.Name("birthDate")).SendKeys("10.04.2026");
        driver.FindElement(By.Name("birthPlace")).SendKeys("test");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step6 title");
        IWebElement Step6Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step6Title.Text.Trim(), Is.EqualTo("DETAJET E MARTESËS"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorMarriage = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[1]/span")));
        Assert.That(msgErrorMarriage.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.Name("date")).SendKeys("10.04.2026");
        driver.FindElement(By.Name("place")).SendKeys("test");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step7 title");
        IWebElement Step7Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h2")));
        Assert.That(Step7Title.Text.Trim(), Is.EqualTo("Dokumentacioni"));

        Log("Kliko Dergo button pa ngarkuar dokumentet e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[4]/div/button[2]"));

        Log("Assert mesazhin e errorit per dokumentet e detyrueshme");
        IWebElement msgErrorDocs = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div/div[2]")));
        Assert.That(msgErrorDocs.Text.Trim(), Is.EqualTo("Ju lutem ngarkoni dokumentin e kërkuar"));

        Log("Ngarko dokument jo te sakte");
        string ID = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";

        Assert.That(File.Exists(ID), Is.True, "File ID nuk ekziston.");

        IWebElement IDInputWrong = wait.Until(
          ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Dokument identifikimi')]/following::input[@type='file'][1]"))
        );
        IDInputWrong.SendKeys(ID);

        Log("Assert uncorrect doc name");
        IWebElement fileDocNameError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Emri i dokumentit është i pavlefshëm')]"))
        );
        Assert.That(fileDocNameError.Displayed, Is.True);
        Assert.That(
            fileDocNameError.Text.Trim(),
            Does.Contain("Emri i dokumentit është i pavlefshëm")
        );

        Log("Remove uncorrect docs");
        RemoveAllUploadedDocs();
        Thread.Sleep(1500);

        Log("Ngarko dok e sakte");

        ID = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(ID), Is.True, "File ID nuk ekziston.");

        IWebElement IDInput = wait.Until(
         ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Dokument identifikimi')]/following::input[@type='file'][1]"))
       );
        IDInput.SendKeys(ID);
        

        Log("Kliko checkbox ");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/span"));


        Log("TEST PASSED");
    }
}