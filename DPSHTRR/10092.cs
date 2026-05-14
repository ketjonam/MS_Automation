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
public class _10092_
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

    /// <summary>
    /// Ngarkimi përdor &lt;document-upload&gt; me Shadow DOM — butoni dropzone nuk është i dukshëm nga driver i zakonshëm.
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
                    // host pa shadow të hapur
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

        foreach (var dropzone in driver.FindElements(By.CssSelector("[data-role='dropzone']")))
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

    /// <summary>
    /// Teksti i dukshëm i faqes + shadow roots të document-upload (për mesazhe ngarkimi).
    /// </summary>
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

    /// <summary>
    /// Provo të klikosh rifillimin e ngarkimit brenda shadow DOM (ikonë retry), nëse ekziston.
    /// </summary>
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

    /// <summary>
    /// Pritet që emri i skedarit të shfaqet dhe që të mos ketë gabimin e burimit (404 në API dokumentesh).
    /// Emri i skedarit mund të shfaqet edhe kur ngarkimi dështon — prandaj kontrollohet edhe mesazhi i kuq.
    /// </summary>
    private void WaitUntilDocumentUploadSucceededInUi(string fileName, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        bool retriedAfterResourceError = false;

        while (DateTime.UtcNow < deadline)
        {
            string blob = GetPageAndDocumentUploadShadowText();

            if (blob.Contains(UploadResourceNotFoundMessage, StringComparison.OrdinalIgnoreCase))
            {
                if (!retriedAfterResourceError && TryClickDocumentUploadRetryInShadow())
                {
                    retriedAfterResourceError = true;
                    deadline += TimeSpan.FromSeconds(30);
                    Log(
                        "U shfaq \"" + UploadResourceNotFoundMessage + "\"; u provua rifillimi nga UI (retry). " +
                        "Pritje shtesë për përgjigjen e shërbimit të dokumenteve…");
                    Thread.Sleep(1500);
                    continue;
                }

                Assert.Fail(
                    "Ngarkimi i dokumentit dështoi në UI me mesazhin \"" + UploadResourceNotFoundMessage + "\" " +
                    (retriedAfterResourceError ? "(edhe pas një retry). " : "") +
                    "Kjo tregon që API-ja e dokumenteve kthen burim të panjohur (404) ose URL të gabuar — " +
                    "rregullo konfigurimin e mjedisit / proxy / base URL për shërbimin e dokumenteve; testi nuk mund të vazhdojë pa upload të suksesshëm.");
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

    /// <summary>
    /// Butoni 'Dërgo' shpesh mbetet i mbuluar ose native click nuk e aktivizon; përdor pritje për enabled + klik JS.
    /// </summary>
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
    public void Regjistrim_Ciklomotori()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";
        string titleXpath = "/html/body/div/main/div[3]/div/div/div/div/h4";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("J25730113W");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("10092");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("dpshtrr-merge-not-ams");
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

        Log("Assert Title");
        IWebElement titleElement = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(titleXpath)));
        Log("Title text: " + titleElement.Text.Trim());
        Assert.That(titleElement.Displayed, Is.True, "Titulli nuk eshte visible");

        Log("Zgjidh te dhenat mbi Drejtorine Rajonale");
        SelectByValueSafe(By.Name("rajoni"), "11");
        SelectByValueSafe(By.Name("bashkia"), "TIR");
        SelectByValueSafe(By.Name("njesiaAdm"), "NJESIADMINNR1");
        SelectByValueSafe(By.Name("nenNjesia"), "NJESIABASHKNR1-TIR");

        Log("Kliko Vazhdo");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step 2 Title");
        IWebElement step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(step2Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKANTIT"));
        Thread.Sleep(4000);
        Log("Assert Te dhenat individuale");
        IWebElement NID = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nid")));
        Assert.That(NID.GetAttribute("value").Trim(), Is.EqualTo("J25730113W"));

        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("emri")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("Daniela"));

        IWebElement Mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("mbiemri")));
        Assert.That(Mbiemri.GetAttribute("value").Trim(), Is.EqualTo("Mema"));

        IWebElement Atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("atesia")));
        Assert.That(Atesia.GetAttribute("value").Trim(), Is.EqualTo("Mersin"));

        IWebElement Datelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("datelindja")));
        Assert.That(Datelindja.GetAttribute("value").Trim(), Is.EqualTo("1992-07-30"));

        IWebElement Vendlindja = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("vendlindja")));
        Assert.That(Vendlindja.GetAttribute("value").Trim(), Is.EqualTo("Kavajë"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));

        IWebElement NrTel = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("telNo")));
        Assert.That(NrTel.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("INFORMACION SPECIFIK MBI APLIKIMIN"));

        Log("Assert 'Mesazhet e errorit per fushat required'");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        IWebElement errorMessage = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/span")));
        Assert.That(errorMessage.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Plotëso fushat e kërkuara dhe vazhdo");
        driver.FindElement(By.Name("vin")).SendKeys("test");
        driver.FindElement(By.Name("licenceNo")).SendKeys("test");
        new SelectElement(driver.FindElement(By.Name("vehicleType"))).SelectByValue("M");

        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));


        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));



        Log("Ngarko nje dokument");
        const string testPdfPath = @"C:\Users\Kreatx\Downloads\TEST.pdf";
        const string testPdfFileName = "TEST.pdf";
        Assert.That(
            File.Exists(testPdfPath),
            Is.True,
            $"Skedari PDF i dokumentit nuk ekziston: {testPdfPath}");

        foreach (var host in driver.FindElements(By.TagName("document-upload")))
        {
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center', inline:'nearest'});",
                    host);
            }
            catch (StaleElementReferenceException)
            {
            }
        }

        Thread.Sleep(400);
        FindDocumentUploadFileInput().SendKeys(testPdfPath);
        Log("Duke pritur që ngarkimi të përfundojë në UI (pa gabimin e burimit të kërkuar)…");
        WaitUntilDocumentUploadSucceededInUi(testPdfFileName, TimeSpan.FromSeconds(45));

        Log("Kliko CHECKBOX");

        IWebElement checkbox = wait.Until(ExpectedConditions.ElementExists(By.Id("consentCheckbox")));
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            checkbox
        );

        Thread.Sleep(800);


        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", checkbox);

        Thread.Sleep(1000);

        Log("Kliko Dergo Button");
        ClickDerghoAfterDocumentationReady();

        const string alertExpectedTitle = "Kujdes";
        const string alertExpectedDescription =
            "Ekzistojne aplikime te pa perfunduara per kete mjet.";

        IWebElement? alertModal = null;
        try
        {
            alertModal = new WebDriverWait(driver, TimeSpan.FromSeconds(12)).Until(
                ExpectedConditions.ElementIsVisible(By.CssSelector(".alert-modal-container")));
        }
        catch (WebDriverTimeoutException)
        {
        }

        if (alertModal is not null && alertModal.Displayed)
        {
            Log("Aplikimi u dërgua: sistemi u përgjigj dhe u shfaq modal paralajmërimi i pritur.");
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
                "Pas 'Dërgo' nuk u shfaq modal paralajmërimi (.alert-modal-container). " +
                "Kontrollo nëse dokumenti u ngarkua plotësisht dhe nëse butoni 'Dërgo' ekzekutoi dërgimin.");
        }

        Log("TEST PASSED");
    }
}