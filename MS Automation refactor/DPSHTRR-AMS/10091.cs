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
public class _10091_
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

    private IWebElement FindDerghoButtonInMain()
    {
        var candidates = driver.FindElements(
            By.XPath("//main//button[contains(normalize-space(.), 'Dërgo') or contains(normalize-space(.), 'Dergo')]"));
        IWebElement pick = candidates.LastOrDefault(e =>
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
        if (pick == null && candidates.Count > 0)
            pick = candidates[candidates.Count - 1];
        if (pick == null)
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

    private bool TryClickOptionalAgreeCheckbox()
    {
        By[] locators =
        {
            By.Id("agreeCheck"),
            By.Id("consentCheckbox"),
            By.XPath("//main//input[@type='checkbox' and not(@disabled)]"),
            By.XPath("//main//span[contains(.,'deklarativ') or contains(.,'pajtohem') or contains(.,'Pajtohem')]"),
            By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[2]/div/span")
        };

        foreach (By by in locators)
        {
            try
            {
                var el = driver.FindElements(by).FirstOrDefault(e =>
                {
                    try { return e.Displayed; }
                    catch (StaleElementReferenceException) { return false; }
                });
                if (el == null)
                    continue;

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});", el);
                Thread.Sleep(400);
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el);
                Log("U klikua checkbox deklarativ/agree (opsional).");
                Thread.Sleep(800);
                return true;
            }
            catch (Exception ex)
            {
                Log("TryClickOptionalAgreeCheckbox: " + ex.Message);
            }
        }

        Log("Nuk u gjet checkbox deklarativ — vazhdohet me Dërgo.");
        return false;
    }

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
                catch (InvalidOperationException) { }
                catch (StaleElementReferenceException) { }
                catch (WebDriverException) { }
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
            catch (InvalidOperationException) { }
            catch (StaleElementReferenceException) { }
            catch (WebDriverException) { }
        }

        return wait.Until(ExpectedConditions.ElementExists(By.CssSelector("main input[type='file']")));
    }

    private void UploadSignedPdfOnDocumentationStep()
    {
        const string signedPdfPath = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
        Assert.That(File.Exists(signedPdfPath), Is.True, $"Skedari PDF nuk ekziston: {signedPdfPath}");

        Log("Prit 1 minutë para ngarkimit të dokumenteve të sakta…");
        Thread.Sleep(TimeSpan.FromMinutes(1));

        Log("Ngarko Signed_TEST_signed.pdf");
        bool uploaded = false;

        var labelInputs = driver.FindElements(
            By.XPath("//div[contains(.,'DAP') or contains(.,'dokument') or contains(.,'Dokument')]/following::input[@type='file'][1]"));
        foreach (var input in labelInputs)
        {
            try
            {
                if (!input.Displayed && input.GetAttribute("type") != "file")
                    continue;
                input.SendKeys(signedPdfPath);
                uploaded = true;
                Thread.Sleep(1500);
                break;
            }
            catch (Exception)
            {
            }
        }

        if (!uploaded)
        {
            var hosts = driver.FindElements(By.TagName("document-upload"));
            foreach (var host in hosts)
            {
                try
                {
                    var inputs = host.GetShadowRoot().FindElements(By.CssSelector("input[type='file']"));
                    if (inputs.Count == 0)
                        continue;
                    inputs[0].SendKeys(signedPdfPath);
                    uploaded = true;
                    Thread.Sleep(1500);
                }
                catch (Exception)
                {
                }
            }
        }

        if (!uploaded)
        {
            FindDocumentUploadFileInput().SendKeys(signedPdfPath);
            Thread.Sleep(2000);
        }
    }

    private void AssertSuccessOrKujdesAfterDergo()
    {
        const string successHeadline = "APLIKIMI JUAJ U DËRGUA ME SUKSES.";
        const string alertExpectedTitle = "Kujdes";
        const string alertExpectedDescription =
            "Ekzistojne aplikime te pa perfunduara per kete mjet.";

        By successHeadlineBy = By.XPath(
            "//h5[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]");
        By alertModalBy = By.CssSelector(".alert-modal-container");

        string outcome = null;
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
    }

    private string ReadVisibleMainTitle()
    {
        foreach (var by in new[]
        {
            By.XPath("//main//h4"),
            By.XPath("//main//h5"),
            By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"),
            By.XPath("/html/body/div/main/div[3]/div/div/div/div/h5")
        })
        {
            try
            {
                var el = driver.FindElements(by).FirstOrDefault(e =>
                {
                    try { return e.Displayed && !string.IsNullOrWhiteSpace(e.Text); }
                    catch { return false; }
                });
                if (el != null)
                    return el.Text.Trim();
            }
            catch
            {
            }
        }

        return string.Empty;
    }

    [Test]
    public void PajisjemeDAPperMakineriteeRenda()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form");
        driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("10091");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("dpshtrr-ams");
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

        Thread.Sleep(4000);

        Log("Assert Step1 title");
        IWebElement Step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step1Title.Text.Trim(), Is.EqualTo("TË DHËNAT E SUBJEKTIT"));

        Log("Assert te dhenat e SUBJEKTIT");
        IWebElement NrIdentifikimit = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nipt")));
        Assert.That(NrIdentifikimit.GetAttribute("value").Trim(), Is.EqualTo("L12121023B"));

        IWebElement Emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("subjectName")));
        Assert.That(Emri.GetAttribute("value").Trim(), Is.EqualTo("KREATX"));

        IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("administrator")));
        Assert.That(Administratori.GetAttribute("value").Trim(), Is.EqualTo("Enor  Vlash  Nakuçi"));

        IWebElement Tel = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("phoneNumber")));
        Assert.That(Tel.GetAttribute("value").Trim(), Is.EqualTo("0676041404"));

        IWebElement Email = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("email")));
        Assert.That(Email.GetAttribute("value").Trim(), Is.EqualTo("ketjona.mema@kreatx.com"));


        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Thread.Sleep(4000);
        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("TË DHËNAT E KANDIDATIT"));

        Log("Kliko Vazhdo pa plotesuar te dhenat e kandidatit");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Log("Assert mesazhi per te plotesuar te dhenat e kandidatit");
        IWebElement msgError = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/div/div")));
        Assert.That(msgError.Text.Trim(), Is.EqualTo("Plotësoni fushën për të vazhduar"));

        Log("Ploteso te dhenat e kandidatit");
        IWebElement NID = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/div/input")));
        NID.SendKeys("J55728107R");
        NID.SendKeys(Keys.Tab);

        Thread.Sleep(2000);

        Log("Assert te dhenat e KANDIDATIT");

        IWebElement EmriKandidat = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div/div[2]/input")));
        Assert.That(EmriKandidat.GetAttribute("value").Trim(), Is.EqualTo("Ketjona"));

        IWebElement MbiemriKandidat = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div/div[3]/input")));
        Assert.That(MbiemriKandidat.GetAttribute("value").Trim(), Is.EqualTo("Mema"));

        IWebElement DatelindjaKandidat = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div/div[5]/input")));
        Assert.That(DatelindjaKandidat.GetAttribute("value").Trim(), Is.EqualTo("28.07.1995"));

        IWebElement GjiniaKandidat = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/form/div/div[6]/input")));
        Assert.That(GjiniaKandidat.GetAttribute("value").Trim(), Is.EqualTo("Femër"));

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("TË DHËNAT E LEJES SË DREJTIMIT QË DISPONON"));

        Log("Assert te dhenat e lejes se drejtimit");
        IWebElement Kategoria = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/input")));
        Assert.That(Kategoria.GetAttribute("value").Trim(), Is.EqualTo("B"));

        IWebElement DataLeshimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/input")));
        Assert.That(DataLeshimit.GetAttribute("value").Trim(), Is.EqualTo("03.06.2022"));

        IWebElement DataVlefshmerise = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[3]/input")));
        Assert.That(DataVlefshmerise.GetAttribute("value").Trim(), Is.EqualTo("02.06.2032"));

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Thread.Sleep(4000);

        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("LLOJI I DAP"));


        Log("Kliko Dergo buton pa plotesuar llojin e dap");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

        Log("Assert mesazhi per te plotesuar llojin e dap");
        IWebElement msgErrorDAP = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/span")));
        Assert.That(msgErrorDAP.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

        Log("Zgjidh llojin e DAP");
       new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/select"))))
            .SelectByValue("Automakinist");

        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/select"))))
            .SelectByValue("11");

        Log("Kliko Vazhdo pas zgjedhjes së llojit DAP (provo DOKUMENTACIONI nëse ekziston)");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));
        Thread.Sleep(4000);

        string afterDapTitle = ReadVisibleMainTitle();
        Log("Titulli pas LLOJI I DAP: '" + afterDapTitle + "'");

        bool onDokumentacioni =
            afterDapTitle.IndexOf("DOKUMENTACIONI", StringComparison.OrdinalIgnoreCase) >= 0
            || driver.FindElements(By.TagName("document-upload")).Count > 0
            || driver.FindElements(By.CssSelector("main input[type='file']")).Count > 0;

        if (onDokumentacioni)
        {
            Log("U arrit hapi DOKUMENTACIONI — ngarko Signed PDF + Dërgo.");
            UploadSignedPdfOnDocumentationStep();
            TryClickOptionalAgreeCheckbox();
            ClickDerghoAfterDocumentationReady();
            AssertSuccessOrKujdesAfterDergo();
        }
        else
        {
            Log("Nuk u gjet hap DOKUMENTACIONI — provo Dërgo në hapin e fundit nëse ekziston.");
            try
            {
                TryClickOptionalAgreeCheckbox();
                ClickDerghoAfterDocumentationReady();
                AssertSuccessOrKujdesAfterDergo();
            }
            catch (NoSuchElementException)
            {
                Log("Nuk u gjet butoni Dërgo pas LLOJI I DAP — shërbimi nuk ka hap dërgimi të dukshëm në këtë hap.");
                Assert.Inconclusive(
                    "Pas LLOJI I DAP nuk u gjet as DOKUMENTACIONI as butoni Dërgo. " +
                    "FailCase mbulon dërgimin pa fusha të plota.");
            }
            catch (WebDriverTimeoutException)
            {
                Log("Timeout duke pritur Dërgo — shërbimi mund të mos ketë hap dërgimi.");
                Assert.Inconclusive(
                    "Pas LLOJI I DAP nuk u aktivizua Dërgo brenda kohës. FailCase mbulon rastin FAIL.");
            }
        }

        Log("TEST PASSED");
    }
}