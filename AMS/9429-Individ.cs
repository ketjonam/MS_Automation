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
public class _9429_
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
        Log("Fillova heqjen e dokumenteve jo te sakta");

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

        Log("Perfundoi tentativa per heqjen e dokumenteve jo te sakta");
    }

    [Test]
    public void LicensimiAdministratoreveTeFalimentit()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("9429");
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
        IWebElement step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(step2Title.Text.Trim(), Is.EqualTo("INFORMACION I PËRGJITHSHËM"));
        Thread.Sleep(4000);

        Log("Assert Te dhenat individuale");
        IWebElement NID = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[4]/input")));
        Assert.That(NID.GetAttribute("value").Trim(), Is.EqualTo("J55728107R"));

        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[1]/input")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));

        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[2]/input")));
        Assert.That(Mbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));

        IWebElement Atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[3]/input")));
        Assert.That(Atesia.GetAttribute("value").Trim(), Is.EqualTo("Mersin"));

        IWebElement Datelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[6]/input")));
        Assert.That(Datelindja.GetAttribute("value").Trim(), Is.EqualTo("28.07.1995"));

        IWebElement Vendlindja = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[7]/input")));
        Assert.That(Vendlindja.GetAttribute("value").Trim(), Is.EqualTo("Kavajë"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[12]/input")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));

        IWebElement PhoneNumber = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[11]/input")));
        Assert.That(PhoneNumber.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        IWebElement Gjinia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[9]/input")));
        Assert.That(Gjinia.GetAttribute("value").Trim(), Is.EqualTo("KAVAJË"));

        IWebElement Rrethi = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[10]/input")));
        Assert.That(Rrethi.GetAttribute("value").Trim(), Is.EqualTo("TIRANË"));

        IWebElement Shtetesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div/div[8]/input")));
        Assert.That(Shtetesia.GetAttribute("value").Trim(), Is.EqualTo("Shqiptare"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("KOMPANIA NËSE JENI I PUNËSUAR"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[4]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("DEKLARIME"));

        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[4]/div/button[2]"));

        IWebElement msgErrorDekl = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/div[2]/div[2]")));
        Assert.That(msgErrorDekl.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Kryej deklarimet e profesionit");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div/div[2]/div[1]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div/div[2]/div[2]/span"));

        Log("Kryej deklarimet personale");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/div[2]/div/div[1]/input"));

        Log("Zgjidh statusin e licenses");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[3]/div/div[2]/div[1]/div[1]/input"));

        Log("Kryej deklarimin e pranimit te kushteve");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[3]/div/div[2]/div[3]/span"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[4]/div/button[2]"));

        Log("Assert Step4 Title ");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

        Log("Kliko Vazhdo buton pa ngarkuar dokumentin");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/div/button[2]"));

        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div[1]/div/div[2]")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Ju lutem ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dok jo te sakte");

        string Diploma = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string CV = @"C:\Users\Kreatx\Downloads\png-transparent-visual-studio-logo-vs-brand-3d-icon.png";
        string GaranciaBankare = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";

        Assert.That(File.Exists(Diploma), Is.True, "File Diploma nuk ekziston.");
        Assert.That(File.Exists(CV), Is.True, "File CV nuk ekziston.");
        Assert.That(File.Exists(GaranciaBankare), Is.True, "File Garancia Bankare nuk ekziston.");

        IWebElement DiplomaInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Diplomën')]/following::input[@type='file'][1]"))
        );
        DiplomaInputWrong.SendKeys(Diploma);

        IWebElement CVInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'CV')]/following::input[@type='file'][1]"))
        );
        CVInputWrong.SendKeys(CV);

        IWebElement GaranciaInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'garanci bankare')]/following::input[@type='file'][1]"))
        );
        GaranciaInputWrong.SendKeys(GaranciaBankare);

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
        Diploma = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        CV = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        GaranciaBankare = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(Diploma), Is.True, "File Diploma nuk ekziston.");
        Assert.That(File.Exists(CV), Is.True, "File CV nuk ekziston.");
        Assert.That(File.Exists(GaranciaBankare), Is.True, "File Garancia Bankare nuk ekziston.");

        IWebElement DiplomaInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Diplomën')]/following::input[@type='file'][1]"))
        );
        DiplomaInput.SendKeys(Diploma);

        IWebElement CVInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'CV')]/following::input[@type='file'][1]"))
        );
        CVInput.SendKeys(CV);

        IWebElement GaranciaInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'garanci bankare')]/following::input[@type='file'][1]"))
        );
        GaranciaInput.SendKeys(GaranciaBankare);

        Log("Kliko checkbox e autotirimit");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[2]/div/div[2]/div/span"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/div/button[2]"));

        Log("Assert Step5 Title ");
        IWebElement Step5Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step5Title.Text.Trim(), Is.EqualTo("DEKLARATË MBI USHTRIMIN E VEPRIMTARISË"));

        Log("Ploteso checkbox e dekarates");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div/div/div[1]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div/div/div[2]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/div/div[1]/span"));
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/div/div[2]/span"));

        Log("TEST PASSED");
    }
}