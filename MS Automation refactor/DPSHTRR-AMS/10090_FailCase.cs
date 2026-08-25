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

/// <summary>
/// Stimulon rastin e FAIL të 10090: të njëjtat hapa deri në DOKUMENTACIONI,
/// por pa ngarkuar dokumentin e saktë. Pas Dërgo pret Fail me mesazhin e UI
/// (as sukses, as "Kujdes").
/// </summary>
[TestFixture]
public class _10090_FailCase_
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
            By.XPath("//main//span[contains(.,'deklarativ') or contains(.,'pajtohem') or contains(.,'Pajtohem')]")
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

        Log("Nuk u gjet checkbox — vazhdohet me Dërgo.");
        return false;
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

        object jsResult = ((IJavaScriptExecutor)driver).ExecuteScript(@"
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

    private void AssertFailWithUiMessage()
    {
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

            if (visibleAlert != null)
            {
                string title = visibleAlert.FindElement(By.CssSelector("h2.alert-modal-title")).Text.Trim();
                string desc = visibleAlert.FindElement(By.CssSelector(".alert-modal-description")).Text.Trim();
                string modalMessage = $"[{title}] {desc}";

                if (string.Equals(title, "Kujdes", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail(
                        "Stimulimi i FAIL dështoi: u shfaq modal 'Kujdes'. Mesazhi: " + modalMessage);
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

    [Test]
    public void RipajisjeDAP_FailCase_ReturnsUiMessage()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form (të njëjtat të dhëna si 10090)");
        driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("10090");
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

        Log("Zgjidh llojin e DAP");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div/div[1]/div[2]/div[1]/input"));

        Log("Kliko Vazhdo");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));
        Thread.Sleep(4000);

        Log("Assert Step2 title");
        IWebElement Step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step2Title.Text.Trim(), Is.EqualTo("TË DHËNAT E APLIKANTIT"));

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/button[2]"));
        Thread.Sleep(4000);

        Log("Assert Step3 title");
        IWebElement Step3Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step3Title.Text.Trim(), Is.EqualTo("INFORMACIONI I KONTAKTIT TË APLIKANTIT"));

        Log("Zgjidh DPSHTRR");
        new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.Name("rajoni"))))
            .SelectByValue("346f9d39-16dd-4000-a53d-b8b49a30d210");

        Log("Kliko Vazhdo button");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));
        Thread.Sleep(4000);

        Log("Assert Step4 title");
        IWebElement Step4Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(Step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

        Log("Zgjidh arsyen kerkeses");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/div/div[2]/div[2]/input"));

        Log("STIMULIM FAIL: nuk ngarkohet dokumenti i saktë (skip correct upload).");
        Thread.Sleep(2000);

        TryClickOptionalAgreeCheckbox();

        Log("Kliko Dergo Button");
        try
        {
            ClickDerghoAfterDocumentationReady();
        }
        catch (Exception ex)
        {
            Log("FindDergho dështoi, fallback te xpath: " + ex.Message);
            SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/button[2]"));
        }

        AssertFailWithUiMessage();
    }
}
