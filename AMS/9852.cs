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
public class _9852_
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
    public void KerkesePerPjesmarrjeNeAktivitete()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("9852");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("ams-other");
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
        IWebElement step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(step2Title.Text.Trim(), Is.EqualTo("INFORMACION MBI SUBJEKTIN"));
        Thread.Sleep(4000);

        Log("Assert te dhenat e subjektit");
        IWebElement NIPT = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nipt")));
        Assert.That(NIPT.GetAttribute("value").Trim(), Is.EqualTo("L12121023B"));

        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("subjectName")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("KREATX"));

        IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[1]/div[3]/div/input")));
        Assert.That(Administratori.GetAttribute("value").Trim(), Is.EqualTo("Enor  Vlash  Nakuçi |"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("info@kreatx.com"));

        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("phone")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("+35544200600"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[5]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequired = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[3]/div[1]/div/div")));
        Assert.That(msgErrorRequired.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.Id("licenseNumber")).SendKeys("test123");

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[5]/div/button[2]"));


        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("INFORMACION MBI APLIKANTIN"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[5]/div/button[2]"));

        IWebElement msgErrornid = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[1]/div/div/div")));
        Assert.That(msgErrornid.Text.Trim(), Is.EqualTo("Ju lutem kërkoni përdoruesin fillimisht për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        IWebElement NID = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nid")));
        NID.SendKeys("J55728107R");
        NID.SendKeys(Keys.Tab);


        Thread.Sleep(3000);

        Log("Assert te dhenat e aplikantit");
        IWebElement AplEmri = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("name")));
        Assert.That(AplEmri.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));

        IWebElement AplMbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("surname")));
        Assert.That(AplMbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));

        IWebElement AplAtesia = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("fatherName")));
        Assert.That(AplAtesia.GetAttribute("value").Trim(), Is.EqualTo("Mersin"));

        IWebElement AplGjinia = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("gender")));
        Assert.That(AplGjinia.GetAttribute("value").Trim(), Is.EqualTo("Femër"));

        Log("Kliko Vazhdo buton pa plotesuar Nr e tel dhe Email");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[5]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequiredApl = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[3]/div[3]/div/div")));
        Assert.That(msgErrorRequiredApl.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso Nr e tel dhe Email");
        IWebElement AplEmail = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("applicantEmail")));
        AplEmail.SendKeys("ketjona.mema@kreatx.com");

        IWebElement AplTel = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("mobilePhone")));
        AplTel.SendKeys("0676041404");

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[5]/div/button[2]"));


        Thread.Sleep(3000);

        Log("Assert Step3 Title ");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("INFORMACION MBI AKTIVITETIN"));

        Log("Kliko Vazhdo pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequiredActivity = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[1]/div[1]/div/div")));
        Assert.That(msgErrorRequiredActivity.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(driver.FindElement(By.Id("activityType")))
            .SelectByValue("Evente");
        new SelectElement(driver.FindElement(By.Id("fairType")))
            .SelectByValue("Kombëtare");
        new SelectElement(driver.FindElement(By.Id("list")))
            .SelectByValue("FITUR: Panairi Ndërkombëtar i Tregtisë së Turizmit në Madrid");
        IWebElement VetedeklarimiXhiros = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[1]/div[4]/div/input")));
        VetedeklarimiXhiros.SendKeys("1000000");

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div/div/button[2]"));

        Log("Assert Step4 Title ");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

        Thread.Sleep(3000);

        Log("Kliko Dergo buton pa ngarkuar dokumentin");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div[3]/div/button[2]"));

        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div[1]/div/div[2]/div[2]/div/div[2]")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Ju lutem ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dok jo te sakte");

        string LogoKompanise = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string MaterialePromocionale = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";
        

        Assert.That(File.Exists(LogoKompanise), Is.True, "File LogoKompanise nuk ekziston.");
        Assert.That(File.Exists(MaterialePromocionale), Is.True, "File MaterialePromocionale nuk ekziston.");

        IWebElement LogoKompaniseInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Logo e kompanisë në format vektorial')]/following::input[@type='file'][1]"))
        );
        LogoKompaniseInputWrong.SendKeys(LogoKompanise);

        IWebElement MaterialePromocionaleInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Materiale promocionale të prodhuara nga operatori turistik')]/following::input[@type='file'][1]"))
        );
        MaterialePromocionaleInputWrong.SendKeys(MaterialePromocionale);

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

        Log("Remove uncorrect docs");
        RemoveAllUploadedDocs();
        Thread.Sleep(1500);

        Log("Ngarko dok e sakte");
        LogoKompanise = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        MaterialePromocionale = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(LogoKompanise), Is.True, "File Logo e kompanise nuk ekziston.");
        Assert.That(File.Exists(MaterialePromocionale), Is.True, "File Materiale Promocionale nuk ekziston.");

        IWebElement LogoKompaniseInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Logo e kompanisë në format vektorial')]/following::input[@type='file'][1]"))
        );
        LogoKompaniseInput.SendKeys(LogoKompanise);

        IWebElement MaterialePromocionaleInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Materiale promocionale të prodhuara nga operatori turistik')]/following::input[@type='file'][1]"))
        );
        MaterialePromocionaleInput.SendKeys(MaterialePromocionale);

        Log("Kliko checkbox e autorizimit");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div[2]/div/div[1]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div[2]/div/div[2]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div[2]/div/div[3]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div[2]/div/div[4]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/div[2]/div/div[5]/span"));


        Log("TEST PASSED");
    }
}