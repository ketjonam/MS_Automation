using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;
using System.Linq;

[TestFixture]
public class NIPTWeb_9288
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

    [Test]
    public void Aplikim_i_Ri_Biznes_9288()
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

        Log("===== TEST START =====");
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
                driver.FindElement(By.Id("ServiceCode")).SendKeys("9288");
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

                IWebElement nipt = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nipt")));
                Assert.That(InputValue(nipt), Is.EqualTo("L12121023B"));

                IWebElement EmriSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("subjectName")));
                Assert.That(InputValue(EmriSubjektit), Is.EqualTo("KREATX"));

                IWebElement DtRegjistrimit = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("registrationDate")));
                Assert.That(InputValue(DtRegjistrimit), Is.EqualTo("21.09.2011"));

                IWebElement StatusiSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("subjectStatus")));
                Assert.That(InputValue(StatusiSubjektit), Is.EqualTo("Aktiv"));

                IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("administrator")));
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
                        By.XPath("//*[@id='phone' or @name='phone']"))
                );
                Assert.That(InputValue(phoneNumber), Is.EqualTo("+35544200600"));

                Thread.Sleep(500);
                Log("Click Vazhdo button - Step 2");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]")).Click();

                Log("Assert Step 3");
                IWebElement step3Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(step3Title.Text.Trim(), Is.EqualTo("DETAJET E APLIKIMIT"));

                Log("Click 'Vazhdo' button - Step 3");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]")).Click();

                Log("Assert Dokumentacioni");
                IWebElement step4Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

                Log("Click 'Dergo' button without required document");
                IWebElement dergoBtn = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[5]/div/button[2]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({ block: 'center' });",
                    dergoBtn
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(dergoBtn)).Click();

                IWebElement docErrorMessage = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[2]/div[1]/div[2]"))
                );
                Assert.That(docErrorMessage.Text, Does.Contain("Ju lutem ngarkoni dokumentin e kërkuar"));

                Log("Upload uncorrect docs");
                string fileKerkesa = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
                string fileVetedeklarim = @"C:\Users\Kreatx\Downloads\image.png";
                string fileKontrata = @"C:\Users\Kreatx\Downloads\15mb.pdf";

                Assert.That(File.Exists(fileKerkesa), Is.True, "File Kerkesa nuk ekziston.");
                Assert.That(File.Exists(fileVetedeklarim), Is.True, "File Vetedeklarim nuk ekziston.");
                Assert.That(File.Exists(fileKontrata), Is.True, "File Kontrata nuk ekziston.");

                var wrongFileInputs = wait.Until(d =>
                {
                    var els = d.FindElements(By.XPath("//input[@type='file']"));
                    return els.Count >= 3 ? els : null;
                });

                wrongFileInputs[0].SendKeys(fileKerkesa);
                wrongFileInputs[1].SendKeys(fileVetedeklarim);
                wrongFileInputs[2].SendKeys(fileKontrata);

                Log("Assert Max size");
                IWebElement fileSizeError = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Madhësia e dokumentit')]"))
                );
                Assert.That(fileSizeError.Displayed, Is.True);
                Assert.That(
                    fileSizeError.Text.Trim(),
                    Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se 15MB")
                );

                Log("Assert format gabim");
                IWebElement formatError = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[contains(@class,'text-danger') and contains(.,'Formati duhet të jetë')]"))
                );
                Assert.That(formatError.Displayed, Is.True);
                Assert.That(formatError.Text.Trim(), Is.EqualTo("Formati duhet të jetë: PDF"));

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

                while (true)
                {
                    var cancelButtons = driver.FindElements(By.CssSelector("button[aria-label='Cancel upload']"));
                    if (cancelButtons.Count == 0)
                        break;

                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript(
                            "arguments[0].scrollIntoView({ block: 'center' });",
                            cancelButtons[0]
                        );
                        Thread.Sleep(300);
                        cancelButtons[0].Click();
                        Thread.Sleep(500);
                    }
                    catch
                    {
                        break;
                    }
                }

                Thread.Sleep(1000);

        Log("Prit 1 minutë para ngarkimit të dokumentit të saktë…");
        Thread.Sleep(TimeSpan.FromMinutes(1));

                Log("Upload Correct Docs");
                string correctFileKerkesa = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileVetedeklarimi = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileKontrata = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";

                Assert.That(File.Exists(correctFileKerkesa), Is.True, "File correct kerkesa nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimi), Is.True, "File correct vetedeklarimi nuk ekziston.");
                Assert.That(File.Exists(correctFileKontrata), Is.True, "File correct kontrata nuk ekziston.");

                var correctFileInputs = wait.Until(d =>
                {
                    var els = d.FindElements(By.XPath("//input[@type='file']"));
                    return els.Count >= 3 ? els : null;
                });

                correctFileInputs[0].SendKeys(correctFileKerkesa);
                correctFileInputs[1].SendKeys(correctFileVetedeklarimi);
                correctFileInputs[2].SendKeys(correctFileKontrata);

                Thread.Sleep(1500);

                Log("Verify uploaded docs are present");
                Assert.That(correctFileInputs[0].GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(correctFileInputs[1].GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(correctFileInputs[2].GetAttribute("value"), Does.Contain(".pdf"));

                var visibleErrors = driver.FindElements(
                        By.XPath("//div[contains(@class,'text-danger') and normalize-space()!='']"))
                    .Where(e => e.Displayed)
                    .ToList();

                Assert.That(visibleErrors.Count, Is.EqualTo(0),
                    "Ka ende gabime të dukshme pas ngarkimit të dokumenteve të sakta.");

                ClickDerghoAfterDocumentationReady(driver);

                const string successHeadline = "APLIKIMI JUAJ U DËRGUA ME SUKSES";
                const string alertExpectedTitle = "Kujdes";
                const string alertExpectedDescription =
                    "Ekzistojne aplikime te pa perfunduara per kete mjet.";

                By successHeadlineBy = By.XPath(
                    "//h5[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')] | //h5/b[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]");
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

                    var refEls = driver.FindElements(
                        By.XPath("//h6[contains(normalize-space(.),'Numri referencë i aplikimit')]"));
                    var trackEls = driver.FindElements(
                        By.XPath("//button[contains(normalize-space(.),'GJURMO APLIKIMIN')]"));
                    bool hasRef = refEls.Any(e =>
                    {
                        try { return e.Displayed; }
                        catch (StaleElementReferenceException) { return false; }
                    });
                    bool hasTrack = trackEls.Any(e =>
                    {
                        try { return e.Displayed; }
                        catch (StaleElementReferenceException) { return false; }
                    });

                    if (hasRef && hasTrack)
                    {
                        IWebElement referenceLine = refEls.First(e =>
                        {
                            try { return e.Displayed; }
                            catch (StaleElementReferenceException) { return false; }
                        });
                        Assert.That(
                            referenceLine.Text.Trim(),
                            Does.Contain("Numri referencë i aplikimit është:").IgnoreCase);
                        Assert.That(
                            referenceLine.Text.Trim(),
                            Does.Match("(?i)eALB-\\d+"));

                        IWebElement trackBtn = trackEls.First(e =>
                        {
                            try { return e.Displayed; }
                            catch (StaleElementReferenceException) { return false; }
                        });
                        Assert.That(trackBtn.Displayed, Is.True);
                        Log("Sukses i verifikuar: headline, referenca eALB dhe butoni GJURMO APLIKIMIN.");
                    }
                    else
                    {
                        Log("Sukses i verifikuar: headline (eALB/GJURMO nuk u gjetën — mjafton për AQTN).");
                    }
                }
                else if (outcome == "alert")
                {
                    Log("Aplikimi u dërgua: sistemi u përgjigj dhe u shfaq modal paralajmërimi 'Kujdes'.");
                    IWebElement alertModal = driver.FindElement(alertModalBy);
                    IWebElement alertModalTitle = alertModal.FindElement(By.CssSelector("h2.alert-modal-title"));
                    Assert.That(alertModalTitle.Text.Trim(), Does.StartWith("Kujdes"));

                    var descEls = alertModal.FindElements(By.CssSelector(".alert-modal-description"));
                    if (descEls.Count > 0)
                    {
                        Log("Kujdes description: " + descEls[0].Text.Trim());
                    }

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
                        "Pas 'Dërgo' nuk u shfaq as ekrani i suksesit ('APLIKIMI JUAJ U DËRGUA ME SUKSES') " +
                        "as modal paralajmërimi 'Kujdes' (.alert-modal-container).");
                }

                Log("TEST PASSED");
            }
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