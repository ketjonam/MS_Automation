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
public class _15007_
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
    public void PeshkimClodhesArgetues()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("15007");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("ams-other");
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
        IWebElement step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div/div/div/div[1]/h4")));
        Assert.That(step1Title.Text.Trim(), Is.EqualTo("MËNYRA E IDENTIFIKIMIT"));

        Log("Zgjidh llojin e identifikimit");
        IWebElement LlojiIdentifikimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div/div/div/div[2]/div[1]/input")));
        LlojiIdentifikimit.Click(); 

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h5")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKANTIT"));  

        Log("Assert te dhenat e aplikantit");
        IWebElement NrIdentifikimit = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nid")));      
        Assert.That(NrIdentifikimit.GetAttribute("value").Trim(), Is.EqualTo("J55728107R"));

        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("name")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));

        IWebElement Atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("fatherName")));
        Assert.That(Atesia.GetAttribute("value").Trim(), Is.EqualTo("Mersin"));

        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("surname")));
        Assert.That(Mbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));

        IWebElement Gjinia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("gender")));
        Assert.That(Gjinia.GetAttribute("value").Trim(), Is.EqualTo("F"));

        IWebElement Ditelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("birthday")));
        Assert.That(Ditelindja.GetAttribute("value").Trim(), Is.EqualTo("28.07.1995"));

        IWebElement Vendlindja = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("birthplace")));
        Assert.That(Vendlindja.GetAttribute("value").Trim(), Is.EqualTo("Kavajë"));

        IWebElement Shtetesia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nationality")));
        Assert.That(Shtetesia.GetAttribute("value").Trim(), Is.EqualTo("Shqiptar"));

        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("phone")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h5")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKIMIT"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[2]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgTipiAplikimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div[1]/div/div")));
        Assert.That(msgTipiAplikimit.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Zgjidh Tipin e Aplikimit");
        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.Name("applicationType"))))
            .SelectByValue("tourist");

        Thread.Sleep(1000);

        Log("Ploteso detajet e mjetit lundrues");
        driver.FindElement(By.Name("vesselName")).SendKeys("Test");
        driver.FindElement(By.Name("vesselLength")).SendKeys("1");
        driver.FindElement(By.Name("grossTonnage")).SendKeys("1");
        driver.FindElement(By.Name("enginePower")).SendKeys("1");
        driver.FindElement(By.Name("maxPassengers")).SendKeys("1");
        driver.FindElement(By.Name("licenseNumber")).SendKeys("Test");
        driver.FindElement(By.Name("registrationCertNumber")).SendKeys("test");
        driver.FindElement(By.Name("activityZone")).SendKeys("test");
        driver.FindElement(By.Name("fishingGear")).SendKeys("test");
        driver.FindElement(By.Name("touristFishingActivity")).SendKeys("test");
        driver.FindElement(By.Name("startDate")).SendKeys("14.04.2026");
        driver.FindElement(By.Name("endDate")).SendKeys("14.05.2026");

        Log("Kliko Vazhdo buton");  
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div/button[2]"));


        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h5")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));


        Log("Kliko Dergo buton pa ngarkuar dokumentat");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[3]/div/button"));


        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div[4]/div/div[2]")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Ju lutemi ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dok jo te sakte");

        string Kopje = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string CertifikataRegjistrimit = @"C:\Users\Kreatx\Downloads\TC_TestAutomation_Mobiread.docx";
        string DeshmiAftesie = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";

        Assert.That(File.Exists(Kopje), Is.True, "File Kopja e librit te anijes nuk ekziston.");
        Assert.That(File.Exists(CertifikataRegjistrimit), Is.True, "File Ceritifikata regjistrimit nuk ekziston.");
        Assert.That(File.Exists(DeshmiAftesie), Is.True, "File Deshmia e aftesise nuk ekziston.");

        IWebElement KopjeInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të librit të anijes ose regjistrit detar')]/following::input[@type='file'][1]"))
        );
        KopjeInputWrong.SendKeys(Kopje);

        IWebElement CertifikataInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të Certifikatës së Regjistrimit të mjetit lundrues')]/following::input[@type='file'][1]"))
        );
        CertifikataInputWrong.SendKeys(CertifikataRegjistrimit);

        IWebElement DeshmiAftesieInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të Dëshmisë së Aftësisë për drejtimin e mjetit lundruese')]/following::input[@type='file'][1]"))
        );
        DeshmiAftesieInputWrong.SendKeys(DeshmiAftesie);

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

        Log("Ngarko dok e sakte");
        Kopje = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        CertifikataRegjistrimit = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        DeshmiAftesie = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        string CertifikateLundrimi = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        string Pagesa = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(Kopje), Is.True, "File Kopja nuk ekziston.");
        Assert.That(File.Exists(CertifikataRegjistrimit), Is.True, "File certifikata regjistrimit nuk ekziston.");
        Assert.That(File.Exists(DeshmiAftesie), Is.True, "File Deshmia aftesise nuk ekziston.");
        Assert.That(File.Exists(CertifikateLundrimi), Is.True, "File Certifikata lundrimit nuk ekziston.");
        Assert.That(File.Exists(Pagesa), Is.True, "File Pagesa nuk ekziston.");

        IWebElement KopjeInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të librit të anijes ose regjistrit detar')]/following::input[@type='file'][1]"))
        );
        KopjeInput.SendKeys(Kopje);

        IWebElement CertifikataRegjistrimitInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të Certifikatës së Regjistrimit të mjetit lundrues')]/following::input[@type='file'][1]"))
        );
        CertifikataRegjistrimitInput.SendKeys(CertifikataRegjistrimit);

        IWebElement DeshmiAftesieInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të Dëshmisë së Aftësisë për drejtimin e mjetit lundrues')]/following::input[@type='file'][1]"))
        );
        DeshmiAftesieInput.SendKeys(DeshmiAftesie);

        IWebElement CertifikateLundrimiInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të certifikatës së lundrimit të sigurt, nga shoqëria klasifikuese e cila duhet të jetë e vlefshme')]/following::input[@type='file'][1]"))
        );
        CertifikateLundrimiInput.SendKeys(CertifikateLundrimi);

        IWebElement PagesaInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Paguaj')]/following::input[@type='file'][1]"))
        );
        PagesaInput.SendKeys(Pagesa);




        Log("TEST PASSED");
    }
}