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
public class _9636_
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
    public void KerkesePeRifitimShtetesie()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("9636");
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
        IWebElement step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(step2Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKANTIT"));
        Thread.Sleep(4000);


        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequiredStep1 = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[5]/div")));
        Assert.That(msgErrorRequiredStep1.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        IWebElement NrPasaportes = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[1]/input")));
        NrPasaportes.SendKeys("test");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[2]/select"))))
            .SelectByValue("Femer");

        IWebElement DokUdhetimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[3]/input")));
        DokUdhetimit.SendKeys("test");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[4]/select"))))
            .SelectByValue("ANDORRA (AD) - AN");
        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[5]/input")));
        Emri.SendKeys("Ketjona");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[6]/select"))))
            .SelectByValue("EMIRATET E BASHKUARA ARABE (AE) - TC"); 

        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[7]/input")));
        Mbiemri.SendKeys("Mema");   

        IWebElement Atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[9]/input")));
        Atesia.SendKeys("Mersin");

        IWebElement Amesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[11]/input")));
        Amesia.SendKeys("Aishe");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[12]/select"))))
            .SelectByValue("Beqar");

        By datelindjaLocator = By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[13]/input");

        Log("Ploteso datelindjen");
        SafeClick(datelindjaLocator);

        IWebElement datelindja = wait.Until(ExpectedConditions.ElementIsVisible(datelindjaLocator));
        datelindja.SendKeys(Keys.Control + "a");
        datelindja.SendKeys(Keys.Delete);
        datelindja.SendKeys("28.07.1995");
        datelindja.SendKeys(Keys.Tab);
        Thread.Sleep(1000);

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Thread.Sleep(3000);

        Log("Assert Step2 title");  
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("INFORMACIONI I KONTAKTIT TË APLIKANTIT"));
       
        Log("Kliko Vazhdo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorRequired = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[1]/div")));
        Assert.That(msgErrorRequired.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
       IWebElement Fshati_Qyteti = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[1]/input")));
        Fshati_Qyteti.SendKeys("test");

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[2]/input")));
        Email.SendKeys("ketjona.mema@reatx.com");   

        IWebElement Telefon = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[3]/input")));
        Telefon.SendKeys("0676041404");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[5]/select"))))
            .SelectByValue("Tiranë");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[6]/select"))))
            .SelectByValue("1");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[7]/select"))))
            .SelectByValue(", TIRANE, KOM POL NR 1");

        IWebElement Rruga = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[1]/div[8]/textarea")));
        Rruga.SendKeys("test");


        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/form/div[2]/div/button[2]"));


        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));


        Log("Kliko Dergo buton pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div[2]/div/button[2]"));

        IWebElement msgErrorTipi = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div[1]/div/div")));
        Assert.That(msgErrorTipi.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Ploteso fushat per bashkine");
        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div[1]/div/select"))))
            .SelectByValue("1");

        Thread.Sleep(3000);

        Log("Kliko Dergo buton pa ngarkuar dokumentin");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div[5]/div/button[2]"));

        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div[2]/div[2]/div/div[2]")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Ju lutemi ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dok jo te sakte");

        string ID = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
        string Kerkesa = @"C:\Users\Kreatx\Downloads\TC_TestAutomation_Mobiread.docx";
        string Deklarata = @"C:\Users\Kreatx\Downloads\E88.30_CheckPointVPN.msi";

        Assert.That(File.Exists(ID), Is.True, "File ID nuk ekziston.");
        Assert.That(File.Exists(Kerkesa), Is.True, "File Kerkesa nuk ekziston.");
        Assert.That(File.Exists(Deklarata), Is.True, "File Deklarata nuk ekziston.");

        IWebElement IDInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje e dokumentit të identifikimit')]/following::input[@type='file'][1]"))
        );
        IDInputWrong.SendKeys(ID);

        IWebElement KerkesaInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kërkesa e shtetasit të huaj drejtuar Presidentit të Republikës')]/following::input[@type='file'][1]"))
        );
        KerkesaInputWrong.SendKeys(Kerkesa);

        IWebElement DeklarataInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Deklarate noteriale')]/following::input[@type='file'][1]"))
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

        Log("Ngarko dok e sakte");
        ID = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        Kerkesa = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        Deklarata = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        string Vertetimi = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        string Certifikata = @"C:\Users\Kreatx\Downloads\TEST.pdf"; 
        string Fotografia = @"C:\Users\Kreatx\Downloads\TEST.pdf";

        Assert.That(File.Exists(ID), Is.True, "File ID nuk ekziston.");
        Assert.That(File.Exists(Kerkesa), Is.True, "File Kerkesa nuk ekziston.");
        Assert.That(File.Exists(Deklarata), Is.True, "File Deklarata nuk ekziston.");
        Assert.That(File.Exists(Vertetimi), Is.True, "File Vertetimi nuk ekziston.");
        Assert.That(File.Exists(Certifikata), Is.True, "File Certifikata nuk ekziston.");
        Assert.That(File.Exists(Fotografia), Is.True, "File Fotografia nuk ekziston.");

        IWebElement IDInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kopje e dokumentit të identifikimit')]/following::input[@type='file'][1]"))
        );
        IDInput.SendKeys(ID);

        IWebElement KerkesaInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Kërkesa e shtetasit të huaj drejtuar Presidentit të Republikës')]/following::input[@type='file'][1]"))
        );
        KerkesaInput.SendKeys(Kerkesa);

        IWebElement DeklarataInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Deklarate noteriale')]/following::input[@type='file'][1]"))
        );
        DeklarataInput.SendKeys(Deklarata);

        IWebElement VertetimiInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Vërtetimi i marrë nga njësia administrative ku është banor')]/following::input[@type='file'][1]"))
        );
        VertetimiInput.SendKeys(Vertetimi);

        IWebElement CertifikataInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Certifikatë lindje ose vdekje e prindërve të aplikantit')]/following::input[@type='file'][1]"))
        );
        CertifikataInput.SendKeys(Certifikata);

        IWebElement FotografiaInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Fotografi e aplikantit(përmasa 4*6 cm). Në rast se ka fëmijë nën 14 vjeç, fotografi për secilin prej tyre')]/following::input[@type='file'][1]"))
        );
                FotografiaInput.SendKeys(Fotografia);

        Log("Kliko checkbox e autotirimit");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div[4]/span"));

     

        Log("TEST PASSED");
    }
}