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
public class _9768_
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
    public void RegjistrimMartese()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/div[1]/div/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("9768");
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
        IWebElement step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(step1Title.Text.Trim(), Is.EqualTo("TË DHËNA PERSONALE TË APLIKANTIT"));
        Thread.Sleep(1000);

        Log("Assert te dhenat e aplikantit");
        IWebElement NID = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nid")));
        Assert.That(NID.GetAttribute("value").Trim(), Is.EqualTo("J55728107R"));
        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("emri")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));
        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("mbiemri")));
        Assert.That(Mbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));
        IWebElement Datelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[8]/div/input")));
        Assert.That(Datelindja.GetAttribute("value").Trim(), Is.EqualTo("28.07.1995"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/button[2]"));

        Log("Assert mesazhin per fushat e detyrueshme");
        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[1]/div/small")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Id("aplikojSi"))).SelectByValue("3");
        new SelectElement(driver.FindElement(By.Id("vendlindjaShteti"))).SelectByValue("36");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("ADRESA E APLIKANTIT (NË VENDIN E REZIDENCËS)"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[3]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorReqField = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[2]/div/small")));
        Assert.That(msgErrorReqField.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Id("adrShteti"))).SelectByValue("2");
        driver.FindElement(By.Id("qyteti")).SendKeys("test");
        driver.FindElement(By.Id("rruga")).SendKeys("test");
        driver.FindElement(By.Id("kodiPostar")).SendKeys("1001");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[3]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("KONTAKTI"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorContact = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[3]/div/small")));
        Assert.That(msgErrorContact.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Assert te dhenat e kontaktit");
        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));
        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("phoneNumber")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Id("country"))).SelectByValue("2");
        Thread.Sleep(500);
        new SelectElement(driver.FindElement(By.Id("consularOffice"))).SelectByValue("1");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("TË DHËNAT E BASHKËSHORTIT"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorHsb = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[1]/div/small")));
        Assert.That(msgErrorHsb.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.Id("bashkeshortiEmri")).SendKeys("test");
        driver.FindElement(By.Id("bashkeshortiMbiemri")).SendKeys("test");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[3]/div/input")).SendKeys("10.04.1990");
        driver.FindElement(By.Id("bashkeshortiQyteti")).SendKeys("test");
        new SelectElement(driver.FindElement(By.Id("bashkeshortiShteti"))).SelectByValue("82");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step5 title");
        IWebElement Step5Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step5Title.Text.Trim(), Is.EqualTo("TË DHËNAT E BASHKËSHORTES"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorWife = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[1]/div/small")));
        Assert.That(msgErrorWife.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.Id("bashkeshorteEmri")).SendKeys("test");
        driver.FindElement(By.Id("bashkeshorteMbiemri")).SendKeys("test");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[3]/div/input")).SendKeys("10.04.1990");
        driver.FindElement(By.Id("bashkeshorteQyteti")).SendKeys("test");
        new SelectElement(driver.FindElement(By.Id("bashkeshorteShteti"))).SelectByValue("82");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step6 title");
        IWebElement Step6Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step6Title.Text.Trim(), Is.EqualTo("TË DHËNAT PËR REGJISTRIMIN E MARTESËS"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per fushat e detyrueshme");
        IWebElement msgErrorMarriage = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[2]/div/small")));
        Assert.That(msgErrorMarriage.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));


        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[1]/div/input")).SendKeys("10.04.2020");
        driver.FindElement(By.Id("vendiLidhjesMarteses")).SendKeys("test");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step7 title");
        IWebElement Step7Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h2")));
        Assert.That(Step7Title.Text.Trim(), Is.EqualTo("Dokumentacioni"));

        Log("Kliko Dergo button pa ngarkuar dokumentet e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[2]/div/button[2]"));

        Log("Assert mesazhin e errorit per dokumentet e detyrueshme");
        IWebElement msgErrorDocs = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div[1]/div[2]")));
        Assert.That(msgErrorDocs.Text.Trim(), Is.EqualTo("Ju lutem ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dokument jo te sakte");
        string CertifikateMartese = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string PerkthimiCertifikates = @"C:\Users\Kreatx\Downloads\Test_Automation_NIPTWEB_Prezantim.pptx";
        string ID = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";


        Assert.That(File.Exists(CertifikateMartese), Is.True, "File Certifikate Martese nuk ekziston.");
        Assert.That(File.Exists(PerkthimiCertifikates), Is.True, "File Perkthimi Certifikates nuk ekziston.");
        Assert.That(File.Exists(ID), Is.True, "File ID nuk ekziston.");

        IWebElement CertifikateMarteseInputWrong = wait.Until(
          ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Certifikata e martesës')]/following::input[@type='file'][1]"))
        );
        CertifikateMarteseInputWrong.SendKeys(CertifikateMartese);

        IWebElement PerkthimiCertifikatesInputWrong = wait.Until(
          ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Përkthimi i certifikatës së martesës')]/following::input[@type='file'][1]"))
        );
        PerkthimiCertifikatesInputWrong.SendKeys(PerkthimiCertifikates);

        IWebElement IDInputWrong = wait.Until(
          ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Dokumente identifikimi')]/following::input[@type='file'][1]"))
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

        Log("Assert uncorrect doc format");
        IWebElement fileDocFormatError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Formati duhet të jetë:  JPG, JPEG, PNG, PDF, TXT')]"))
        ); Assert.That(fileDocFormatError.Displayed, Is.True);
        Assert.That(
            fileDocFormatError.Text.Trim(),
            Does.Contain("Formati duhet të jetë: JPG, JPEG, PNG, PDF, TXT")
        );

        Log("Assert uncorrect doc size");
        IWebElement fileDocVersionError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Madhësia e dokumentit nuk duhet të jetë më shumë se {{maxSize}} MB 5MB')]"))
        );
        Assert.That(fileDocVersionError.Displayed, Is.True);
        Assert.That(
            fileDocVersionError.Text.Trim(),
            Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se {{maxSize}} MB 5MB")
        );

        Log("Remove uncorrect docs");
        RemoveAllUploadedDocs();
        Thread.Sleep(1500);

        Log("Ngarko dok e sakte");

        CertifikateMartese = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        PerkthimiCertifikates = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        ID = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(CertifikateMartese), Is.True, "File Certifikate Martese nuk ekziston.");
        Assert.That(File.Exists(PerkthimiCertifikates), Is.True, "File Perkthimi Certifikates nuk ekziston.");
        Assert.That(File.Exists(ID), Is.True, "File ID nuk ekziston.");

        IWebElement CertifikateMarteseInput = wait.Until(
         ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Certifikata e martesës')]/following::input[@type='file'][1]"))
       );
        CertifikateMarteseInput.SendKeys(CertifikateMartese);

        IWebElement PerkthimiCertifikatesInput = wait.Until(
          ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Përkthimi i certifikatës së martesës')]/following::input[@type='file'][1]"))
        );
        PerkthimiCertifikatesInput.SendKeys(PerkthimiCertifikates);

        IWebElement IDInput = wait.Until(
          ExpectedConditions.ElementExists(
             By.XPath("//div[contains(.,'Dokumente identifikimi')]/following::input[@type='file'][1]"))
        );
        IDInput.SendKeys(ID);

        Thread.Sleep(2000);



        Log("TEST PASSED");
    }
}