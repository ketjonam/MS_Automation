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
/// Stimulon rastin e FAIL të 14111: të njëjtat të dhëna, por pa ngarkuar dokumente,
/// që pas Dërgo të mos shfaqet as sukses as "Kujdes". Testi dështon me mesazhin e UI.
/// </summary>
[TestFixture]
public class _14111_FailCase_
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

        // Prefer the known Step2 validation message path used by 14111
        try
        {
            var known = driver.FindElements(
                By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div[2]/div/div[2]/div[2]"));
            foreach (var el in known)
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

        object jsResult = ((IJavaScriptExecutor)driver).ExecuteScript(@"
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
    public void KonvertimLejesDrejtimit_FailCase_ReturnsUiMessage()
    {
        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log("Fill form (të njëjtat të dhëna si 14111)");
        driver.FindElement(By.Id("Nid")).SendKeys("J25730113W");
        driver.FindElement(By.Id("ServiceCode")).SendKeys("14111");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("dpshtrr-merge-not-ams_refactor");
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
        IWebElement step1Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(step1Title.Text.Trim(), Is.EqualTo("INFORMACION MBI APLIKANTIN"));
        Thread.Sleep(4000);

        Log("Assert Te dhenat individuale");
        Assert.That(wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nid"))).GetAttribute("value").Trim(), Is.EqualTo("J25730113W"));
        Assert.That(wait.Until(ExpectedConditions.ElementIsVisible(By.Id("emri"))).GetAttribute("value").Trim(), Is.EqualTo("Daniela"));
        Assert.That(wait.Until(ExpectedConditions.ElementIsVisible(By.Id("mbiemri"))).GetAttribute("value").Trim(), Is.EqualTo("Mema"));
        Assert.That(wait.Until(ExpectedConditions.ElementIsVisible(By.Id("atesia"))).GetAttribute("value").Trim(), Is.EqualTo("Mersin"));
        Assert.That(wait.Until(ExpectedConditions.ElementIsVisible(By.Id("datelindja"))).GetAttribute("value").Trim(), Is.EqualTo("30/07/1992"));

        Log("Kliko Vazhdo buton");
        SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));
        Thread.Sleep(3000);

        Log("Assert Step2 title");
        IWebElement step2Title = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4")));
        Assert.That(step2Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

        Log("STIMULIM FAIL: nuk ngarkohen dokumente (qëllimisht).");
        Thread.Sleep(1000);

        Log("Kliko butonin dergo pa ngarkuar dokumentat e detyrueshme");
        try
        {
            ClickDerghoAfterDocumentationReady();
        }
        catch (Exception ex)
        {
            Log("FindDergho dështoi, fallback te xpath i njohur: " + ex.Message);
            SafeClick(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/button[2]"));
        }

        DpshtrrFailCaseSupport.AssertInformationalFailAfterDergo(driver, Log);
    }

    [Test]
    public void KonvertimLejesDrejtimit_FailCase_GjendjaCivile_ReturnsGabimPopup()
    {
        const string expectedNid = "J55728107H";
        const string expectedDescription =
            "Nuk u arrit të merren të dhënat nga Gjendja Civile. Ju lutemi provoni përsëri më vonë.";

        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log($"Fill form (14111, NID {expectedNid} për stimulim Gabim nga Gjendja Civile)");
        driver.FindElement(By.Id("Nid")).SendKeys(expectedNid);
        driver.FindElement(By.Id("ServiceCode")).SendKeys("14111");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("dpshtrr-merge-not-ams_refactor");
        driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
        driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
        driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

        new SelectElement(driver.FindElement(By.Id("ProfileType"))).SelectByValue("Individual");
        new SelectElement(driver.FindElement(By.Id("Platform"))).SelectByValue("WEB");

        Log("Click LOAD SERVICE");
        driver.FindElement(By.ClassName("load-button")).Click();
        Thread.Sleep(3000);

        Log("Click Aplikimi i Ri (hapi i të dhënave të aplikantit)");
        SafeClick(By.XPath(aplikimiRiXpath));

        DpshtrrFailCaseSupport.AssertExpectedGabimPopup(
            driver, wait, Log, "Gabim", expectedDescription, "Gjendja Civile");
    }

    [Test]
    public void KonvertimLejesDrejtimit_FailCase_Qkb_ReturnsGabimPopup()
    {
        const string expectedNid = "M55555555E";
        const string expectedDescription =
            "Nuk u arrit të merren të dhënat nga QKB. Ju lutemi provoni përsëri më vonë.";

        string serviceButtonXpath = "/html/body/div/main/div/div[1]/div/a";
        string aplikimiRiXpath = "/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div";

        Log("Open website");
        driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

        Log("Click service button");
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(serviceButtonXpath))).Click();

        Log($"Fill form (14111, NID {expectedNid}, ProfileType Organisation për stimulim Gabim nga QKB)");
        driver.FindElement(By.Id("Nid")).SendKeys(expectedNid);
        driver.FindElement(By.Id("ServiceCode")).SendKeys("14111");
        driver.FindElement(By.Id("MicroserviceName")).SendKeys("dpshtrr-merge-not-ams_refactor");
        driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
        driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
        driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

        new SelectElement(driver.FindElement(By.Id("ProfileType"))).SelectByValue("Organisation");
        new SelectElement(driver.FindElement(By.Id("Platform"))).SelectByValue("WEB");

        Log("Click LOAD SERVICE");
        driver.FindElement(By.ClassName("load-button")).Click();
        Thread.Sleep(3000);

        Log("Click Aplikimi i Ri (hapi i të dhënave të aplikantit)");
        SafeClick(By.XPath(aplikimiRiXpath));

        DpshtrrFailCaseSupport.AssertExpectedGabimPopup(
            driver, wait, Log, "Gabim", expectedDescription, "Qendra Kombëtare e Biznesit");
    }
}
