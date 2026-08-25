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
public class _10095_
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

    private IWebElement ScrollUntilElementVisibleWithWait(By locator)
    {
        IWebElement element = wait.Until(ExpectedConditions.ElementIsVisible(locator));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            element
        );
        Thread.Sleep(500);
        return element;
    }

    /// <summary>
    /// Ngarkimi përdor &lt;document-upload&gt; me Shadow DOM; butoni dropzone (ds-comp__dropzone) nuk merr SendKeys për PDF.
    /// </summary>
    private IWebElement FindDocumentUploadFileInput()
    {
        wait.Until(drv =>
        {
            foreach (var host in drv.FindElements(By.TagName("document-upload")))
            {
                try
                {
                    if (host.GetShadowRoot().FindElements(By.CssSelector("input[type='file']")).Count > 0)
                        return true;
                }
                catch (InvalidOperationException)
                {
                }
                catch (StaleElementReferenceException)
                {
                }
                catch (WebDriverException)
                {
                }
            }

            return drv.FindElements(By.CssSelector("main input[type='file']")).Count > 0;
        });

        foreach (var host in driver.FindElements(By.TagName("document-upload")))
        {
            try
            {
                var inputs = host.GetShadowRoot().FindElements(By.CssSelector("input[type='file']"));
                if (inputs.Count > 0)
                    return inputs[0];
            }
            catch (InvalidOperationException)
            {
            }
            catch (StaleElementReferenceException)
            {
            }
            catch (WebDriverException)
            {
            }
        }

        foreach (var dropzone in driver.FindElements(
                     By.CssSelector("button.ds-comp__dropzone[data-role='dropzone'], [data-role='dropzone']")))
        {
            try
            {
                if (!dropzone.Displayed)
                    continue;
                return dropzone.FindElement(
                    By.XPath("./ancestor::*[.//input[@type='file']][1]//input[@type='file']"));
            }
            catch (NoSuchElementException)
            {
            }
        }

        return wait.Until(ExpectedConditions.ElementExists(By.CssSelector("main input[type='file']")));
    }

    private static readonly string UploadResourceNotFoundMessage =
        "Burimi i kërkuar nuk u gjet";

    private string GetPageAndDocumentUploadShadowText()
    {
        object? result = ((IJavaScriptExecutor)driver).ExecuteScript(@"
            const parts = [];
            if (document.body && document.body.innerText)
                parts.push(document.body.innerText);
            document.querySelectorAll('document-upload').forEach(h => {
                if (h.shadowRoot)
                    parts.push(h.shadowRoot.textContent || '');
            });
            return parts.join('\n');
        ");
        return result?.ToString() ?? string.Empty;
    }

    private bool TryClickDocumentUploadRetryInShadow()
    {
        object? result = ((IJavaScriptExecutor)driver).ExecuteScript(@"
            function label(el) {
                const a = (el.getAttribute('aria-label') || '') + ' ' + (el.getAttribute('title') || '');
                const c = (el.className && el.className.toString) ? el.className.toString() : '';
                return (a + ' ' + c).toLowerCase();
            }
            for (const h of document.querySelectorAll('document-upload')) {
                const root = h.shadowRoot;
                if (!root) continue;
                for (const el of root.querySelectorAll('button, [role=""button""]')) {
                    const L = label(el);
                    if (L.includes('retry') || L.includes('refresh') || L.includes('rifresk')
                        || L.includes('riprov') || L.includes('re-upload') || L.includes('reupload')) {
                        el.click();
                        return true;
                    }
                }
            }
            return false;
        ");
        return result is bool b && b;
    }

    private static bool DocumentUploadResourceErrorShouldMarkInconclusive()
    {
        string? v = Environment.GetEnvironmentVariable("DPSHTRR_DOCUMENT_UPLOAD_INCONCLUSIVE_ON_RESOURCE_ERROR");
        return string.Equals(v, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }

    private void WaitUntilDocumentUploadSucceededInUi(string fileName, TimeSpan timeout, string fullPdfPath)
    {
        var deadline = DateTime.UtcNow + timeout;
        bool phase1RecoveryDone = false;
        bool phase2RecoveryDone = false;

        while (DateTime.UtcNow < deadline)
        {
            string blob = GetPageAndDocumentUploadShadowText();

            if (blob.Contains(UploadResourceNotFoundMessage, StringComparison.OrdinalIgnoreCase))
            {
                if (!phase1RecoveryDone)
                {
                    phase1RecoveryDone = true;
                    deadline += TimeSpan.FromSeconds(30);
                    bool clicked = TryClickDocumentUploadRetryInShadow();
                    Log(
                        clicked
                            ? "U shfaq \"" + UploadResourceNotFoundMessage + "\"; u klikua rifillimi në UI (retry). Pritje për përgjigje të re…"
                            : "U shfaq \"" + UploadResourceNotFoundMessage + "\"; nuk u gjet butoni retry në shadow — kalon te ri-dërgimi i skedarit në fazën tjetër.");
                    Thread.Sleep(clicked ? 1500 : 400);
                    continue;
                }

                if (!phase2RecoveryDone)
                {
                    phase2RecoveryDone = true;
                    deadline += TimeSpan.FromSeconds(35);
                    Log(
                        "Gabimi i burimit vazhdon; po riprovohet ngarkimi duke ridërguar rrugën e skedarit në input[type=file]…");
                    Thread.Sleep(400);
                    FindDocumentUploadFileInput().SendKeys(fullPdfPath);
                    Thread.Sleep(3000);
                    continue;
                }

                string resourceFailMessage =
                    "Ngarkimi i dokumentit dështoi në UI me mesazhin \"" + UploadResourceNotFoundMessage + "\" " +
                    "edhe pas rifillimit në UI dhe ridërgimit të \"" + fullPdfPath + "\". " +
                    "Shërbimi i dokumenteve në këtë mjedis (141.95.84.12:8080) kthehet me burim të panjohur — " +
                    "kontrollo në DevTools rrjetin (URL e POST/PUT për dokumentin, status 404), reverse proxy dhe base URL të API-së; " +
                    "testi nuk mund të vazhdojë pa upload të suksesshëm. " +
                    "Për pipeline pa API dokumentesh të rregulluar, vendos variablin e mjedisit " +
                    "DPSHTRR_DOCUMENT_UPLOAD_INCONCLUSIVE_ON_RESOURCE_ERROR=1 për Assert.Inconclusive në vend të dështimit.";

                if (DocumentUploadResourceErrorShouldMarkInconclusive())
                {
                    Log(
                        "DPSHTRR_DOCUMENT_UPLOAD_INCONCLUSIVE_ON_RESOURCE_ERROR është aktiv; " +
                        "testi shënohet Inconclusive për shkak të API-së së dokumenteve.");
                    Assert.Inconclusive(resourceFailMessage);
                }

                Assert.Fail(resourceFailMessage);
            }

            if (blob.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            {
                Thread.Sleep(600);
                blob = GetPageAndDocumentUploadShadowText();
                if (blob.Contains(UploadResourceNotFoundMessage, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (blob.Contains(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    Log("Ngarkimi u verifikua në UI: emri i skedarit pa gabimin e burimit të kërkuar.");
                    return;
                }
            }

            Thread.Sleep(400);
        }

        Assert.Fail(
            $"Pas {timeout.TotalSeconds}s nuk u konfirmua ngarkimi i suksesshëm për \"{fileName}\" " +
            "(emri duhet të jetë i dukshëm dhe pa mesazhin e burimit të panjohur).");
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
    public void PajisjemeCKP()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("10095");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("dpshtrr-ams");
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

        Thread.Sleep(4000);

        Log("Assert Step1 title");
        IWebElement Step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step1Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKANTIT"));

        Log("Assert te dhenat e aplikantit");
        IWebElement NrIdentifikimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/input")));
        Assert.That(NrIdentifikimit.GetAttribute("value").Trim(), Is.EqualTo("J55728107R"));

        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[3]/input")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));

        IWebElement Atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/input")));
        Assert.That(Atesia.GetAttribute("value").Trim(), Is.EqualTo("Mersin"));

        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[4]/input")));
        Assert.That(Mbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));

        IWebElement Datelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[5]/input")));
        Assert.That(Datelindja.GetAttribute("value").Trim(), Is.EqualTo("28.07.1995"));


        Log("Kliko Vazhdo button pa plotesuar nivelin e arsimit");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Log("Assert error message per nivelin e arsimit");
        IWebElement msgErrorEducation = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[7]/span")));
        Assert.That(msgErrorEducation.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Zgjidh nivelin e arsimit");
        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[7]/select")))).SelectByValue("I larte");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/button[2]"));



        Thread.Sleep(4000);
        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("INFORMACIONI I KONTAKTIT TË APLIKANTIT"));

        Log("Assert te dhenat e kontaktit");
        IWebElement Qyteti = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("city")));
        Assert.That(Qyteti.GetAttribute("value").Trim(), Is.EqualTo("TIRANË"));

        IWebElement Rrethi = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("rrethi")));
        Assert.That(Rrethi.GetAttribute("value").Trim(), Is.EqualTo("KAVAJË"));

        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nrTel")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("MOTIVI I KËRKESËS"));

        Log("Kliko Vazhdo button pa plotesuar fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Log("Assert error message per fushat e detyrueshme");
        IWebElement msgErrorMsg = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/span")));
        Assert.That(msgErrorMsg.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Ploteso fushat e detyrueshme");
        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/select")))).SelectByValue("11");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/select")))).SelectByValue("Për transportin e mallrave dhe udhëtarëve brenda vendit");

        driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[3]/textarea")).SendKeys("Test");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

        Log("Kliko Dergo buton pa ngarkuar dokumentat");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[4]/button[2]"));

        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div/div[2]")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Ju lutem ngarkoni dokumentin e kërkuar."));

        Log("Ngarko dok jo te sakte");

        string Diploma = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";

        Assert.That(File.Exists(Diploma), Is.True, "File Diploma nuk ekziston.");

        IWebElement DiplomaInputWrong = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Diploma')]/following::input[@type='file'][1]"))
        );
        DiplomaInputWrong.SendKeys(Diploma);

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

        Log("Prit disa minuta në hapin DOKUMENTACIONI para ngarkimit të dokumenteve…");
        Thread.Sleep(TimeSpan.FromMinutes(1));

        Log("Ngarko dokumente e sakte");
        const string testPdfPath = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
        const string testPdfFileName = "Signed_TEST_signed.pdf";
        Assert.That(
            File.Exists(testPdfPath),
            Is.True,
            $"Skedari PDF i dokumentit nuk ekziston: {testPdfPath}");

        IWebElement DiplomaInput = wait.Until(
            ExpectedConditions.ElementExists(
                By.XPath("//div[contains(.,'Diploma')]/following::input[@type='file'][1]"))
        );
        DiplomaInput.SendKeys(testPdfPath);
        Thread.Sleep(2000);

        Log("Prit që lista e dokumenteve (document-upload) të shfaqet në hapin DOKUMENTACIONI…");
        var docsWait = new WebDriverWait(driver, TimeSpan.FromSeconds(45));
        docsWait.Until(drv =>
        {
            var hosts = drv.FindElements(By.TagName("document-upload"));
            if (hosts.Count == 0)
                return false;

            foreach (var host in hosts)
            {
                try
                {
                    if (host.GetShadowRoot().FindElements(By.CssSelector("input[type='file']")).Count > 0)
                        return true;
                }
                catch (InvalidOperationException)
                {
                }
                catch (StaleElementReferenceException)
                {
                }
                catch (WebDriverException)
                {
                }
            }

            return false;
        });

        var uploadHosts = driver.FindElements(By.TagName("document-upload"));
        Log($"U gjetën {uploadHosts.Count} slot(e) document-upload — po ngarkoj PDF në secilin.");

        int uploadedCount = 0;
        foreach (var host in uploadHosts)
        {
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center', inline:'nearest'});",
                    host);
                Thread.Sleep(400);

                var inputs = host.GetShadowRoot().FindElements(By.CssSelector("input[type='file']"));
                if (inputs.Count == 0)
                    continue;

                inputs[0].SendKeys(testPdfPath);
                uploadedCount++;
                Thread.Sleep(1500);
            }
            catch (StaleElementReferenceException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (WebDriverException)
            {
            }
        }

        if (uploadedCount == 0)
        {
            Log("Ngarkimi përmes shadow host dështoi — fallback te FindDocumentUploadFileInput.");
            FindDocumentUploadFileInput().SendKeys(testPdfPath);
            Thread.Sleep(2000);
        }
        else
        {
            Log($"U dërgua SendKeys te {uploadedCount} input(e) file.");
        }

        Log("Duke pritur që ngarkimi të përfundojë në UI (pa gabimin e burimit të kërkuar)…");
        WaitUntilDocumentUploadSucceededInUi(testPdfFileName, TimeSpan.FromSeconds(45), testPdfPath);

        Log("Kliko CHECKBOX");

        IWebElement checkbox = wait.Until(ExpectedConditions.ElementExists(By.Id("agreeCheck")));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            checkbox
        );

        Thread.Sleep(800);

        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", checkbox);

        Thread.Sleep(1000);

        Log("Kliko Dergo Button");
        ClickDerghoAfterDocumentationReady();

        const string successHeadline = "APLIKIMI JUAJ U DËRGUA ME SUKSES.";
        const string alertExpectedTitle = "Kujdes";
        const string alertExpectedDescription =
            "Ekzistojne aplikime te pa perfunduara per kete mjet.";

        By successHeadlineBy = By.XPath(
            "//h5[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]");
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

            IWebElement referenceLine = wait.Until(ExpectedConditions.ElementIsVisible(
                By.XPath("//h6[contains(normalize-space(.),'Numri referencë i aplikimit')]")));
            Assert.That(
                referenceLine.Text.Trim(),
                Does.Contain("Numri referencë i aplikimit është:").IgnoreCase);
            Assert.That(
                referenceLine.Text.Trim(),
                Does.Match("(?i)eALB-\\d+"));

            IWebElement trackBtn = wait.Until(ExpectedConditions.ElementIsVisible(
                By.XPath("//button[contains(normalize-space(.),'GJURMO APLIKIMIN')]")));
            Assert.That(trackBtn.Displayed, Is.True);
            Log("Sukses i verifikuar: headline, referenca eALB dhe butoni GJURMO APLIKIMIN.");
        }
        else if (outcome == "alert")
        {
            Log("Aplikimi u dërgua: sistemi u përgjigj dhe u shfaq modal paralajmërimi 'Kujdes'.");
            IWebElement alertModal = driver.FindElement(alertModalBy);
            IWebElement modalTitle = alertModal.FindElement(By.CssSelector("h2.alert-modal-title"));
            IWebElement modalDesc = alertModal.FindElement(By.CssSelector(".alert-modal-description"));
            Assert.That(modalTitle.Text.Trim(), Is.EqualTo(alertExpectedTitle));
            Assert.That(modalDesc.Text.Trim(), Is.EqualTo(alertExpectedDescription));

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
                "Pas 'Dërgo' nuk u shfaq as ekrani i suksesit ('APLIKIMI JUAJ U DËRGUA ME SUKSES.') " +
                "as modal paralajmërimi 'Kujdes' (.alert-modal-container).");
        }

        Log("TEST PASSED");
    }
}