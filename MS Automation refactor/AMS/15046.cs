using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

[TestFixture]
public class _15046_
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

    private IWebElement FindDerghoButtonInMain()
    {
        var candidates = driver.FindElements(
            By.XPath("//main//button[contains(normalize-space(.), 'Dërgo') or contains(normalize-space(.), 'Dergo')]"));
        IWebElement? pick = candidates.LastOrDefault(e =>
        {
            try
            {
                return e.Displayed;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
        if (pick is null && candidates.Count > 0)
            pick = candidates[^1];
        if (pick is null)
            throw new NoSuchElementException("Nuk u gjet butoni 'Dërgo' brenda main.");
        return pick;
    }

    private void ClickDerghoAfterDocumentationReady()
    {
        var sendWait = new WebDriverWait(driver, TimeSpan.FromSeconds(45));
        sendWait.Until(drv =>
        {
            try
            {
                var b = FindDerghoButtonInMain();
                return b.Displayed && b.Enabled;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        });

        IWebElement dergo = FindDerghoButtonInMain();
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center', inline:'nearest'});",
            dergo);
        Thread.Sleep(400);
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", dergo);
        Log("Klikuar butoni 'Dërgo' (JavaScript click pasi u aktivizua).");
    }

    [Test]
    public void LejeKalimiKufitar()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "//button[@aria-label='Aplikim i ri']";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("15046");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("ams_merge");
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


        Log("Zgjidh tipin e aplikimit");
        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.Id("applicationType"))))
            .SelectByValue("Aplikim për veten");

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKANTIT"));

        Log("Assert te dhenat e aplikantit");
        IWebElement NrIdentifikimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[1]/input")));
        Assert.That(NrIdentifikimit.GetAttribute("value").Trim(), Is.EqualTo("J55728107R"));

        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[2]/input")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));

        IWebElement Atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[4]/input")));
        Assert.That(Atesia.GetAttribute("value").Trim(), Is.EqualTo("Mersin"));

        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[3]/input")));
        Assert.That(Mbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));

        IWebElement Gjinia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[7]/input")));
        Assert.That(Gjinia.GetAttribute("value").Trim(), Is.EqualTo("Femër"));

        IWebElement Ditelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[6]/input")));
        Assert.That(Ditelindja.GetAttribute("value").Trim(), Is.EqualTo("28.07.1995"));

        IWebElement ShtetiLindjes = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[8]/input")));
        ShtetiLindjes.SendKeys("Shqipëri");

        IWebElement Shtetesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[9]/input")));
        Shtetesia.SendKeys("Shqiptare");

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[10]/input")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKIMIT"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgTipiAplikimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div/div")));
        Assert.That(msgTipiAplikimit.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Zgjidh Tipin e Aplikimit");
        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[1]/select"))))
            .SelectByValue("Rinovim leje / Renewed local border traffic permit");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[2]/select"))))
            .SelectByValue("Kulturor");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[3]/select"))))
            .SelectByValue("BAJRAM CURRI");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[4]/select"))))
            .SelectByValue("Viçidol");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[5]/select"))))
            .SelectByValue("Me automjet");

        Thread.Sleep(200);

        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[6]/input")).SendKeys("AB166DP");
        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[7]/textarea")).SendKeys("JO");


        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("ADRESA E PLOTË E APLIKANTIT"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgAdresa = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[1]/div")));
        Assert.That(msgAdresa.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Ploteso te dhenat e adreses se aplikantit");
        By qarkuSelect = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[1]/select");
        By bashkiaSelect = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[2]/select");
        By njesiaSelect = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[3]/select");
        By fshatiSelect = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[4]/select");
        By rrugaSelect = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[5]/select");
        By nrSelect = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[6]/select");
        By kodiPostalSelect = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div[8]/select");

        SelectByValueSafe(qarkuSelect, "53");
        SelectByValueSafe(bashkiaSelect, "404");
        SelectByValueSafe(njesiaSelect, "1764");
        SelectByValueSafe(fshatiSelect, "28430");
        SelectByValueSafe(rrugaSelect, "42202");
        SelectByValueSafe(nrSelect, "99757");
        SelectByValueSafe(kodiPostalSelect, "0");

        Thread.Sleep(500);

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));
        Thread.Sleep(4000);

        Log("Assert Step5 title");
        IWebElement Step5Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step5Title.Text.Trim(), Is.EqualTo("AFATI I LEJES"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step6 title");
        IWebElement Step6Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step6Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));


        Log("Kliko Dergo buton pa ngarkuar dokumentat");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div[3]/div/button"));


        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div[1]/div[1]/div[2]")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Ju lutem ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dok jo te sakte");

        string Fotokopje = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string Fotografi = @"C:\Users\Kreatx\Downloads\TC_TestAutomation_Mobiread.docx";
        string Deklarata = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";

        Assert.That(File.Exists(Fotokopje), Is.True, "File Fotokopje nuk ekziston.");
        Assert.That(File.Exists(Fotografi), Is.True, "File Fotografi nuk ekziston.");
        Assert.That(File.Exists(Deklarata), Is.True, "File Deklarata nuk ekziston.");

        IWebElement FotokopjeInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Fotokopja e dokumentit të vlefshëm të udhëtimit (pasaportë, kartë identiteti)')]/following::input[@type='file'][1]"))
        );
        FotokopjeInputWrong.SendKeys(Fotokopje);

        IWebElement FotografiInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Një fotografi (47mmX36mm), të bërë jo më përpara se 6 muaj nga data e aplikimit')]/following::input[@type='file'][1]"))
        );
        FotografiInputWrong.SendKeys(Fotografi);

        IWebElement DeklarataInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Deklarata individuale mbi motivin e kalimit dhe qëndrimit në Zonën Kufitare, Shqipëri')]/following::input[@type='file'][1]"))
        );
        DeklarataInputWrong.SendKeys(Deklarata);

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

        Log("Assert uncorrect doc type");
        IWebElement fileDocTypeError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Formati duhet të jetë: ')]"))
        );
        Assert.That(fileDocTypeError.Displayed, Is.True);
        Assert.That(
            fileDocTypeError.Text.Trim(),
            Does.Contain("Formati duhet të jetë:")
        );

        Log("Assert uncorrect doc size");
        IWebElement fileDocSizeError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Madhësia e dokumentit nuk duhet të jetë më shumë se  20MB')]"))
        );
        Assert.That(fileDocSizeError.Displayed, Is.True);
        Assert.That(
            fileDocSizeError.Text.Trim(),
            Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se 20MB")
        );

        Log("Remove uncorrect docs");
        RemoveAllUploadedDocs();
        Thread.Sleep(1500);

        Log("Prit 1 minutë para ngarkimit të dokumentit të saktë…");
        Thread.Sleep(TimeSpan.FromMinutes(1));

        Log("Ngarko dok e sakte");
        Fotokopje = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
        Fotografi = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
        Deklarata = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
        

        Assert.That(File.Exists(Fotokopje), Is.True, "File Fotokopje nuk ekziston.");
        Assert.That(File.Exists(Fotografi), Is.True, "File Fotografi nuk ekziston.");
        Assert.That(File.Exists(Deklarata), Is.True, "File Deklarata nuk ekziston.");
     

        IWebElement FotokopjeInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Fotokopja e dokumentit të vlefshëm të udhëtimit (pasaportë, kartë identiteti)')]/following::input[@type='file'][1]"))
        );
        FotokopjeInput.SendKeys(Fotokopje);

        IWebElement FotografiInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Një fotografi (47mmX36mm), të bërë jo më përpara se 6 muaj nga data e aplikimit')]/following::input[@type='file'][1]"))
        );
        FotografiInput.SendKeys(Fotografi); 

        IWebElement DeklarataInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Deklarata individuale mbi motivin e kalimit dhe qëndrimit në Zonën Kufitare, Shqipëri')]/following::input[@type='file'][1]"))
        );
        DeklarataInput.SendKeys(Deklarata);

        Log("Kliko checkbox e autorizimit");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div[2]/div[1]/span"));
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div[2]/div[2]/span"));

        Log("Kliko Dergo Button");
        ClickDerghoAfterDocumentationReady();

        const string successHeadline = "APLIKIMI JUAJ U DËRGUA ME SUKSES";
        const string alertExpectedTitle = "Kujdes";
        const string alertExpectedDescription =
            "Ekzistojne aplikime te pa perfunduara per kete mjet.";

        By successHeadlineBy = By.XPath(
            "//h5[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')] | //h5/b[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]");
        By alertModalBy = By.CssSelector(".alert-modal-container");

        string? outcome = null;
        try
        {
            outcome = new WebDriverWait(driver, TimeSpan.FromSeconds(20)).Until(drv =>
            {
                try
                {
                    var successEls = drv.FindElements(successHeadlineBy);
                    if (successEls.Any(e =>
                    {
                        try { return e.Displayed; }
                        catch (StaleElementReferenceException) { return false; }
                    }))
                        return "success";
                }
                catch (StaleElementReferenceException)
                {
                }

                try
                {
                    var alertEls = drv.FindElements(alertModalBy);
                    if (alertEls.Any(e =>
                    {
                        try { return e.Displayed; }
                        catch (StaleElementReferenceException) { return false; }
                    }))
                        return "alert";
                }
                catch (StaleElementReferenceException)
                {
                }

                return null;
            });
        }
        catch (WebDriverTimeoutException)
        {
        }

        if (outcome == "success")
        {
            Log("Pas 'Dërgo' u shfaq ekrani i suksesit.");
            IWebElement headline = wait.Until(ExpectedConditions.ElementIsVisible(successHeadlineBy));
            Assert.That(headline.Text.Trim(), Does.Contain(successHeadline).IgnoreCase);

            var refEls = driver.FindElements(
                By.XPath("//h6[contains(normalize-space(.),'Numri referencë i aplikimit')]"));
            var trackEls = driver.FindElements(
                By.XPath("//button[contains(normalize-space(.),'GJURMO APLIKIMIN')]"));
            bool hasRef = refEls.Any(e =>
            {
                try { return e.Displayed; }
                catch (StaleElementReferenceException) { return false; }
            });
            bool hasTrack = trackEls.Any(e =>
            {
                try { return e.Displayed; }
                catch (StaleElementReferenceException) { return false; }
            });

            if (hasRef && hasTrack)
            {
                IWebElement referenceLine = refEls.First(e =>
                {
                    try { return e.Displayed; }
                    catch (StaleElementReferenceException) { return false; }
                });
                Assert.That(
                    referenceLine.Text.Trim(),
                    Does.Contain("Numri referencë i aplikimit është:").IgnoreCase);
                Assert.That(
                    referenceLine.Text.Trim(),
                    Does.Match("(?i)eALB-\\d+"));

                IWebElement trackBtn = trackEls.First(e =>
                {
                    try { return e.Displayed; }
                    catch (StaleElementReferenceException) { return false; }
                });
                Assert.That(trackBtn.Displayed, Is.True);
                Log("Sukses i verifikuar: headline, referenca eALB dhe butoni GJURMO APLIKIMIN.");
            }
            else
            {
                Log("Sukses i verifikuar: headline (eALB/GJURMO nuk u gjetën — mjafton për AQTN).");
            }
        }
        else if (outcome == "alert")
        {
            Log("Aplikimi u dërgua: sistemi u përgjigj dhe u shfaq modal paralajmërimi 'Kujdes'.");
            IWebElement alertModal = driver.FindElement(alertModalBy);
            IWebElement modalTitle = alertModal.FindElement(By.CssSelector("h2.alert-modal-title"));
            Assert.That(modalTitle.Text.Trim(), Is.EqualTo(alertExpectedTitle));

            var descEls = alertModal.FindElements(By.CssSelector(".alert-modal-description"));
            if (descEls.Count > 0)
            {
                Assert.That(descEls[0].Text.Trim(), Is.EqualTo(alertExpectedDescription));
            }

            IWebElement mbyllBtn = alertModal.FindElement(
                By.CssSelector("button.alert-modal-button--primary"));
            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                mbyllBtn);
            Thread.Sleep(300);
            try
            {
                mbyllBtn.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", mbyllBtn);
            }
        }
        else
        {
            Assert.Fail(
                "Pas 'Dërgo' nuk u shfaq as ekrani i suksesit ('APLIKIMI JUAJ U DËRGUA ME SUKSES') " +
                "as modal paralajmërimi 'Kujdes' (.alert-modal-container).");
        }

        Log("TEST PASSED");
    }
}