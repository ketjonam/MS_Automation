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
public class _15128_
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
    public void RegjistrimPajisjeMjekesore()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div[1]/div/button/div/div[2]";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("15128");
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
        IWebElement step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(step1Title.Text.Trim(), Is.EqualTo("INFORMACION MBI APLIKUESIN"));
        Thread.Sleep(1000);

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequiredStep1 = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div/div/div")));
        Assert.That(msgErrorRequiredStep1.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar."));

        Log("Zgjidhni fushat e detyrueshme dhe klikoni Vazhdo");
        new SelectElement(driver.FindElement(By.Id("applicantType"))).SelectByValue("AUTHP");

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert te dhenat e subjektit");
        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[1]/div[1]/div/input")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("KREATX"));

        IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[1]/div[2]/div/input")));
        Assert.That(Administratori.GetAttribute("value").Trim(), Is.EqualTo("Enor Nakuçi"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[2]/div[2]/div/input")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("info@kreatx.com"));

        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[1]/div[3]/div/input")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("+35544200600"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequired = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[2]/div[1]/div/div")));
        Assert.That(msgErrorRequired.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[2]/div[1]/div/input")).SendKeys("test123");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[2]/div[4]/div/input")).SendKeys("test123");
        new SelectElement(driver.FindElement(By.Id("deviceCategoryRepresentative"))).SelectByValue("100");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[2]/div[6]/div/input")).SendKeys("test");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[2]/div[7]/div/input")).SendKeys("+35544200600");
        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div[2]/div[3]/div/input")).SendKeys("test");   

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"));


        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("INFORMACION MBI PAJISJET MJEKËSORE"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[4]/div/button[2]"));

        IWebElement msgErrorApl = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[2]/div/div")));
        Assert.That(msgErrorApl.Text.Trim(), Is.EqualTo("Ju nuk keni shtuar asnjë pajisje mjekësore."));

        Log("Ploteso fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[2]/div/button[2]"));

        Thread.Sleep(2000);
        Log("Zgjidhni kategorine e pajisjes");

        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div/div[2]/div/div[1]/div/input")).SendKeys("test");
        new SelectElement(driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div/div[2]/div/div[2]/div/select"))).SelectByValue("I");
        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div/div[2]/div/div[3]/div/input")).SendKeys("test");
        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div/div[2]/div/div[4]/div/input")).SendKeys("test");

        Log("Kliko RUAJ button ne modal");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div/div[3]/button[2]"));

        Log(" shto standart per pajisjen");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/div[2]/div/div/div/div[2]/div/div[8]/div/span[2]"));

        Log("kliko shto standart button");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[2]/div[1]/div/button"));

        Thread.Sleep(3000);

        Log("shto standartin per pajisjen");
        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[3]/div/div[2]/div/div[1]/div/input")).SendKeys("test");
        driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[3]/div/div[2]/div/div[2]/div/input")).SendKeys("test");

        Log("Kliko RUAJ button ne modalin e standartit");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[3]/div/div[3]/button[2]"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[4]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("INFORMACION MBI ORGANIN E MIRATUAR"));

        Log("Assert mesazhin se nuk ka organ miratuar");
        IWebElement msgOrganiMiratuar = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[1]/p")));
        Assert.That(msgOrganiMiratuar.Text.Trim(), Is.EqualTo("Kjo seksion nuk kërkohet për pajisje të klasës I."));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step4 Title ");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

        Thread.Sleep(3000);

        Log("Kliko Dergo buton pa ngarkuar dokumentin");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[3]/div[3]/div/button[2]"));

        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div[1]/div/div[2]")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Ju lutem ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dok jo te sakte");

        string LejeVeprimtarise = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string Planimetria = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";
        string VertetimiPageses = @"C:\Users\Kreatx\Downloads\TC_TestAutomation_Mobiread.docx";

        Assert.That(File.Exists(LejeVeprimtarise), Is.True, "File LejeVeprimtarise nuk ekziston.");
        Assert.That(File.Exists(Planimetria), Is.True, "File Planimetria nuk ekziston.");
        Assert.That(File.Exists(VertetimiPageses), Is.True, "File VertetimiPageses nuk ekziston.");

        IWebElement LejeVeprimtariseInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Leje e veprimtarisë të lëshuar nga institucioni që mbulon veprimtarinë')]/following::input[@type='file'][1]"))
        );
        LejeVeprimtariseInputWrong.SendKeys(LejeVeprimtarise);

        IWebElement PlanimetriaInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Planimetrinë e sipërfaqes')]/following::input[@type='file'][1]"))
        );
        PlanimetriaInputWrong.SendKeys(Planimetria);

        IWebElement VertetimiPagesesInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Vërtetimin për pagesën')]/following::input[@type='file'][1]"))
        );
        VertetimiPagesesInputWrong.SendKeys(VertetimiPageses);

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
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Madhësia e dokumentit nuk duhet të jetë më shumë se  20MB')]"))
        );
        Assert.That(fileDocSizeError.Displayed, Is.True);
        Assert.That(
            fileDocSizeError.Text.Trim(),
            Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se 20MB")
        );

        Log("Assert uncorrect doc format");
        IWebElement fileDocFormatError = wait.Until(
            ExpectedConditions.ElementIsVisible(
                By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Formati duhet të jetë:  PDF, JPG, JPEG, PNG')]"))
        );
        Assert.That(fileDocFormatError.Displayed, Is.True);
        Assert.That(
            fileDocFormatError.Text.Trim(),
            Does.Contain("Formati duhet të jetë: PDF, JPG, JPEG, PNG")
        );

        Log("Remove uncorrect docs");
        RemoveAllUploadedDocs();
        Thread.Sleep(1500);

        Log("Ngarko dok e sakte");
        LejeVeprimtarise = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        Planimetria = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        VertetimiPageses = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(LejeVeprimtarise), Is.True, "File Leje e veprimtarisë nuk ekziston.");
        Assert.That(File.Exists(Planimetria), Is.True, "File Planimetria nuk ekziston.");
        Assert.That(File.Exists(VertetimiPageses), Is.True, "File Vërtetimi për pagesën nuk ekziston.");

        IWebElement LejeVeprimtariseInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Leje e veprimtarisë të lëshuar nga institucioni që mbulon veprimtarinë')]/following::input[@type='file'][1]"))
        );
        LejeVeprimtariseInput.SendKeys(LejeVeprimtarise);

        IWebElement PlanimetriaInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Planimetrinë e sipërfaqes')]/following::input[@type='file'][1]"))
        );
        PlanimetriaInput.SendKeys(Planimetria);

        IWebElement VertetimiPagesesInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Vërtetimin për pagesën')]/following::input[@type='file'][1]"))
        );
        VertetimiPagesesInput.SendKeys(VertetimiPageses);

        Log("Kliko checkbox e autorizimit");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[4]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[5]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[6]/span"));


        Log("TEST PASSED");
    }
}