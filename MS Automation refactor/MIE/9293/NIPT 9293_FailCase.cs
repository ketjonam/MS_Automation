using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;
using System.Linq;

/// <summary>
/// Stimulon rastin e FAIL të 9293: të njëjtat të dhëna, por pa ngarkuar dokumente,
/// që pas Dërgo të mos shfaqet as sukses as "Kujdes". Testi dështon me mesazhin e UI.
/// </summary>
[TestFixture]
public class NIPT_9293_FailCase
{
    private void Log(string message)
    {
        string logLine = $"{DateTime.Now:HH:mm:ss} | {message}";
        TestContext.Progress.WriteLine(logLine);
        Console.WriteLine(logLine);
    }

    private void SaveScreenshot(IWebDriver driver, string artifactsFolder, string namePrefix)
    {
        try
        {
            if (driver is ITakesScreenshot screenshotDriver)
            {
                string filePath = Path.Combine(
                    artifactsFolder,
                    $"{namePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                );

                screenshotDriver.GetScreenshot().SaveAsFile(filePath);
                TestContext.AddTestAttachment(filePath, "Failure Screenshot");
                Log("Screenshot saved: " + filePath);
            }
        }
        catch (Exception ex)
        {
            Log("Screenshot error: " + ex.Message);
        }
    }

    private static string InputValue(IWebElement element) =>
        element.GetAttribute("value")?.Trim() ?? string.Empty;

    private IWebElement FindDerghoButtonInMain(IWebDriver driver)
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

    private void ClickDerghoAfterDocumentationReady(IWebDriver driver)
    {
        var sendWait = new WebDriverWait(driver, TimeSpan.FromSeconds(45));
        sendWait.Until(drv =>
        {
            try
            {
                var b = FindDerghoButtonInMain(driver);
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

        IWebElement dergo = FindDerghoButtonInMain(driver);
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center', inline:'nearest'});",
            dergo);
        Thread.Sleep(400);
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", dergo);
        Log("Klikuar butoni 'Dërgo' (JavaScript click pasi u aktivizua).");
    }

    private string CaptureVisibleUiMessageAfterDergo(IWebDriver driver)
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
    public void Aplikim_i_Ri_Biznes_9293_FailCase_ReturnsUiMessage()
    {
        var options = new EdgeOptions();
        options.AddArgument("start-maximized");

        string runTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string testName = TestContext.CurrentContext.Test.Name;
        string artifactsFolder = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "TestArtifacts",
            $"{testName}_{runTime}"
        );

        Directory.CreateDirectory(artifactsFolder);

        Log("===== TEST START (FAIL CASE) =====");
        Log("Artifacts folder: " + artifactsFolder);

        using (IWebDriver driver = new EdgeDriver(options))
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            try
            {
                Log("Open Website");
                driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

                Log("Click 'Test Sherbimesh' button");
                wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("/html/body/div/main/div/div[1]/div/a"))).Click();

                Log("Fill in the form fields");
                driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
                driver.FindElement(By.Id("ServiceCode")).SendKeys("9293");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mie_merge");
                driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
                driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
                driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

                new SelectElement(driver.FindElement(By.Id("ProfileType")))
                    .SelectByValue("Organisation");

                new SelectElement(driver.FindElement(By.Id("Platform")))
                    .SelectByValue("WEB");

                Log("Click 'Load Service' button");
                driver.FindElement(By.ClassName("load-button")).Click();

                Log("Click 'Aplikim i Ri' button");
                wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("//button[@aria-label='Aplikim i ri']"))).Click();

                Thread.Sleep(8000);
                Log("Assert detajet e subjektit");
                IWebElement DetajeteSubjektit = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(DetajeteSubjektit.Text.Trim(), Is.EqualTo("DETAJET E SUBJEKTIT"));

                IWebElement nipt = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nipt")));
                Assert.That(InputValue(nipt), Is.EqualTo("L12121023B"));

                IWebElement EmriSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("emri")));
                Assert.That(InputValue(EmriSubjektit), Is.EqualTo("KREATX"));

                IWebElement DtRegjistrimit = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("registrationDate")));
                Assert.That(InputValue(DtRegjistrimit), Is.EqualTo("21.09.2011"));

                IWebElement StatusiSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("status")));
                Assert.That(InputValue(StatusiSubjektit), Is.EqualTo("Aktiv"));

                IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("administrator")));
                Assert.That(InputValue(Administratori), Is.EqualTo("Enor  Vlash  Nakuçi |"));

                Log("Click Vazhdo button - Step 1");
                IWebElement vazhdoBtn1 = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({ block: 'center' });",
                    vazhdoBtn1
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(vazhdoBtn1)).Click();

                Log("Assert Kontakti");
                IWebElement kontaktiTitle = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(kontaktiTitle.Text.Trim(), Is.EqualTo("KONTAKTI"));

                IWebElement email = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//*[@id='email' or @name='email']"))
                );
                Assert.That(InputValue(email), Is.EqualTo("ketjona.mema@kreatx.com"));

                IWebElement phoneNumber = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.Name("nrCel"))
                );
                Assert.That(InputValue(phoneNumber), Is.EqualTo("0676041404"));

                Thread.Sleep(500);
                Log("Click Vazhdo button - Step 2");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]")).Click();

                Log("Assert Step 3");
                IWebElement step3Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(step3Title.Text.Trim(), Is.EqualTo("DETAJET E APLIKIMIT"));

                Log("Select Licence");
                new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.Id("licensePer"))))
                    .SelectByValue("LINJA_TEKNOLOGJIKE");

                Log("Click 'Vazhdo' button - Step 3");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]")).Click();

                Log("Assert Dokumentacioni");
                IWebElement step4Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

                Log("STIMULIM FAIL: nuk ngarkohen dokumente (qëllimisht).");
                Thread.Sleep(1000);

                Log("Kliko butonin dergo pa ngarkuar dokumentat e detyrueshme");
                ClickDerghoAfterDocumentationReady(driver);

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

                string uiMessage = CaptureVisibleUiMessageAfterDergo(driver);
                Log("Mesazhi i kapur nga UI (rasti FAIL): " + uiMessage);

                Assert.Fail(
                    "Rasti FAIL (as sukses, as Kujdes). Mesazhi që u shfaq në UI: " + uiMessage);}
            catch (Exception ex)
            {
                Log("TEST FAILED: " + ex.Message);
                SaveScreenshot(driver, artifactsFolder, "FAILED");
                throw;
            }
            finally
            {
                Log("===== TEST END =====");
            }
        }
    }
}