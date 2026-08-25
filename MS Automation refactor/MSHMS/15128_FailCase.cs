using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Input;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;

/// <summary>
/// Stimulon rastin e FAIL të 15128: të njëjtat të dhëna, por pa ngarkuar dokumente,
/// që pas Dërgo të mos shfaqet as sukses as "Kujdes". Testi dështon me mesazhin e UI.
/// </summary>
[TestFixture]
public class _15128_FailCase_
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

        Log("===== TEST START (FAIL CASE) =====");
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

    private string CaptureVisibleUiMessageAfterDergo()
    {
        Thread.Sleep(1500);

        string[] preferredSelectors =
        {
            ".alert-modal-container",
            ".alert-modal-title",
            ".alert-modal-description",
            ".swal2-title",
            ".swal2-html-container",
            "[role='alert']",
            ".text-danger",
            ".invalid-feedback",
            ".toast-body",
            ".Toastify__toast-body"
        };

        foreach (string css in preferredSelectors)
        {
            try
            {
                foreach (var el in driver.FindElements(By.CssSelector(css)))
                {
                    try
                    {
                        if (!el.Displayed)
                            continue;
                        string t = (el.Text ?? string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(t))
                            return t;
                    }
                    catch (StaleElementReferenceException)
                    {
                    }
                }
            }
            catch (WebDriverException)
            {
            }
        }

        object? jsResult = ((IJavaScriptExecutor)driver).ExecuteScript(@"
            const parts = [];
            const root = document.querySelector('#root') || document.querySelector('main') || document.body;
            if (!root) return '';

            const danger = Array.from(root.querySelectorAll('.text-danger, .invalid-feedback, [role=""alert""], .alert'))
                .map(e => (e.innerText || '').trim())
                .filter(Boolean);
            if (danger.length) return danger.join(' | ');

            const headings = Array.from(root.querySelectorAll('h1,h2,h3,h4,h5,h6,p,span'))
                .map(e => (e.innerText || '').trim())
                .filter(t => t.length > 5 && t.length < 300);
            if (headings.length) return headings.slice(0, 8).join(' | ');

            return (root.innerText || '').trim().substring(0, 500);
        ");

        string fromJs = (jsResult?.ToString() ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(fromJs))
            return fromJs;

        return "(Nuk u gjet asnjë mesazh i dukshëm në UI pas Dërgo.)";
    }

    [Test]
    public void RegjistrimPajisjeMjekesore_FailCase_ReturnsUiMessage()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "//button[@aria-label='Aplikim i ri']";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form (të njëjtat të dhëna si 15128)");
        driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("15128");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("mshms_merge");
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
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[2]/div/button[2]"));

        IWebElement msgErrorApl = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[2]/div/div")));
        Assert.That(msgErrorApl.Text.Trim(), Is.EqualTo("Ju nuk keni shtuar asnjë pajisje mjekësore."));

        Log("Ploteso fushat e detyrueshme");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div[1]/div/button[1]"));

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

        Log("STIMULIM FAIL: nuk ngarkohen dokumente (qëllimisht).");
        Thread.Sleep(1000);

        Log("Kliko butonin dergo pa ngarkuar dokumentat e detyrueshme");
        ClickDerghoAfterDocumentationReady();

        By successHeadlineBy = By.XPath(
            "//h5[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]");
        By alertModalBy = By.CssSelector(".alert-modal-container");

        Thread.Sleep(2500);

        bool sawSuccess = false;
        try
        {
            sawSuccess = driver.FindElements(successHeadlineBy).Any(e =>
            {
                try { return e.Displayed; }
                catch (StaleElementReferenceException) { return false; }
            });
        }
        catch (WebDriverException)
        {
        }

        if (sawSuccess)
        {
            Assert.Fail(
                "Stimulimi i FAIL dështoi: u shfaq ekrani i suksesit (APLIKIMI JUAJ U DËRGUA ME SUKSES.) " +
                "ndërsa ky test pret që të mos shfaqet as sukses as Kujdes.");
        }

        try
        {
            var visibleAlert = driver.FindElements(alertModalBy).FirstOrDefault(e =>
            {
                try { return e.Displayed; }
                catch (StaleElementReferenceException) { return false; }
            });

            if (visibleAlert is not null)
            {
                string title = visibleAlert.FindElement(By.CssSelector("h2.alert-modal-title")).Text.Trim();
                string desc = visibleAlert.FindElement(By.CssSelector(".alert-modal-description")).Text.Trim();
                string modalMessage = $"[{title}] {desc}";

                if (string.Equals(title, "Kujdes", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail(
                        "Stimulimi i FAIL dështoi: u shfaq modal 'Kujdes' (aplikime ekzistuese). " +
                        $"Mesazhi: {modalMessage}");
                }

                Log("Rasti FAIL — u shfaq modal (jo Kujdes): " + modalMessage);
                Assert.Fail(
                    "Rasti FAIL (as sukses, as Kujdes). Mesazhi që u shfaq në UI: " + modalMessage);
            }
        }
        catch (NoSuchElementException)
        {
        }
        catch (WebDriverException)
        {
        }

        string uiMessage = CaptureVisibleUiMessageAfterDergo();
        Log("Mesazhi i kapur nga UI (rasti FAIL): " + uiMessage);

        Assert.Fail(
            "Rasti FAIL (as sukses, as Kujdes). Mesazhi që u shfaq në UI: " + uiMessage);
    }
}
