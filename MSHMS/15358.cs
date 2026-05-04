using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;

[TestFixture]
public class _15358_
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
    public void RilidhjeKontratemeInstitutetShendetesoreJoPublike()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div[1]/div/button/div/div[2]";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("15358");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("msh-merge-v2");
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
        SafeClick(By.XPath(aplikimiRiXpath));
        Thread.Sleep(3000);

        Log("Assert Step 1 Title");
        IWebElement step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(step1Title.Text.Trim(), Is.EqualTo("TË DHËNAT E SUBJEKTIT"));
        Thread.Sleep(1000);

        Log("Assert te dhenat e subjektit");
        IWebElement NID = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nipt")));
        Assert.That(NID.GetAttribute("value").Trim(), Is.EqualTo("L12121023B"));
        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("companyName")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("KREATX"));

        IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("administratorName")));
        Assert.That(Administratori.GetAttribute("value").Trim(), Is.EqualTo("Enor Nakuçi"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));

        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("phone")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequired = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[3]/div[1]/div")));
        Assert.That(msgErrorRequired.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.Name("bankName")).SendKeys("test");
        driver.FindElement(By.Name("accountNumber")).SendKeys("test123");

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div/button[2]"));
        Thread.Sleep(3000);

        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKIMIT"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/button[2]"));

        IWebElement msgErrorApl = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/div/div[6]/div")));
        Assert.That(msgErrorApl.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Name("selectedDirectorate"))).SelectByValue("DRF Tirane");

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step3 Title ");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

        Thread.Sleep(3000);

        Log("Ngarko dok jo te sakte");

        string VertetimLlogarieBankare = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string MarrveshjaLaboratorike = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";
        string LicenceDT = @"C:\Users\Kreatx\Downloads\TC_TestAutomation_Mobiread.docx";

        Assert.That(File.Exists(VertetimLlogarieBankare), Is.True, "File VertetimLlogarieBankare nuk ekziston.");
        Assert.That(File.Exists(MarrveshjaLaboratorike), Is.True, "File MarrveshjaLaboratorike nuk ekziston.");
        Assert.That(File.Exists(LicenceDT), Is.True, "File LicenceDT nuk ekziston.");

        IWebElement VertetimiInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Vërtetim të lëshuar nga banka e nivelit të dytë për llogarinë bankare 1')]/following::input[@type='file'][1]"))
        );
        VertetimiInputWrong.SendKeys(VertetimLlogarieBankare);

        IWebElement MarrveshjaLaboratorikeInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të licensës për laboratorin kliniko-biokimik e mikrobiologjik ose marrëveshje me një laborator për kryerjen e analizave 1')]/following::input[@type='file'][1]"))
        );
        MarrveshjaLaboratorikeInputWrong.SendKeys(MarrveshjaLaboratorike);

        IWebElement LicenceDTInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje së licensës së drejtuesit teknik të laboratorit 1')]/following::input[@type='file'][1]"))
        );
        LicenceDTInputWrong.SendKeys(LicenceDT);

        Log("Assert uncorrect doc size");
        IWebElement fileDocSizeError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Madhësia e dokumentit nuk duhet të jetë më shumë se  5MB')]"))
        );
        Assert.That(fileDocSizeError.Displayed, Is.True);
        Assert.That(
            fileDocSizeError.Text.Trim(),
            Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se 5MB")
        );

        Log("Assert uncorrect doc format");
        IWebElement fileDocFormatError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Formati duhet të jetë: JPG, JPEG, PNG, PDF, TXT')]"))
        );
        Assert.That(fileDocFormatError.Displayed, Is.True);
        Assert.That(
            fileDocFormatError.Text.Trim(),
            Does.Contain("Formati duhet të jetë: JPG, JPEG, PNG, PDF, TXT")
        );

        Log("Remove uncorrect docs");
        RemoveAllUploadedDocs();
        Thread.Sleep(1500);

        Log("Ngarko dok e sakte");
        VertetimLlogarieBankare = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        MarrveshjaLaboratorike = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        LicenceDT = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(VertetimLlogarieBankare), Is.True, "File Vërtetim të lëshuar nga banka e nivelit të dytë për llogarinë bankare nuk ekziston.");
        Assert.That(File.Exists(MarrveshjaLaboratorike), Is.True, "File Marrveshja laboratorike nuk ekziston.");
        Assert.That(File.Exists(LicenceDT), Is.True, "File Licenca e drejtuesit teknik të laboratorit nuk ekziston.");

        IWebElement VertetimLlogarieBankareInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Vërtetim të lëshuar nga banka e nivelit të dytë për llogarinë bankare 1')]/following::input[@type='file'][1]"))
        );
        VertetimLlogarieBankareInput.SendKeys(VertetimLlogarieBankare);

        IWebElement MarrveshjaLaboratorikeInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje të licensës për laboratorin kliniko-biokimik e mikrobiologjik ose marrëveshje me një laborator për kryerjen e analizave 1')]/following::input[@type='file'][1]"))
        );
        MarrveshjaLaboratorikeInput.SendKeys(MarrveshjaLaboratorike);

        IWebElement LicenceDTInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje së licensës së drejtuesit teknik të laboratorit 1')]/following::input[@type='file'][1]"))
        );
        LicenceDTInput.SendKeys(LicenceDT);

        Log("Kliko checkbox e autorizimit");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div[3]/span"));


        Log("TEST PASSED");
    }
}
