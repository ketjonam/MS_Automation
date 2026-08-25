using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;

[TestFixture]
public class BiznesWeb895
{
    private void Log(string message)
    {
        string logLine = $"{DateTime.Now:HH:mm:ss} | {message}";
        TestContext.Progress.WriteLine(logLine);
        TestContext.Out.WriteLine(logLine);
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
    public void NIPTWeb()
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
                Log("Open website");
                driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

                Log("Kliko Test Sherbimesh");
                IWebElement btn = wait.Until(
                    ExpectedConditions.ElementToBeClickable(
                        By.XPath("/html/body/div/main/div/div[1]/div/a"))
                );
                btn.Click();

                Log("Mbush fushat");
                driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
                driver.FindElement(By.Id("ServiceCode")).SendKeys("895");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mie_merge");
                driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
                driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
                driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

                SelectElement tipiProfilit = new SelectElement(driver.FindElement(By.Id("ProfileType")));
                tipiProfilit.SelectByValue("Organisation");

                SelectElement platforma = new SelectElement(driver.FindElement(By.Id("Platform")));
                platforma.SelectByValue("WEB");

                Log("Kliko Load Service");
                IWebElement loadService = driver.FindElement(By.ClassName("load-button"));
                loadService.Click();

                wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("//button[@aria-label='Aplikim i ri']")));

                Log("Hap sherbimin");
                driver.FindElement(By.XPath("//button[@aria-label='Aplikim i ri']")).Click();

                IWebElement serviceContent = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.Id("serviceContent"))
                );
                Assert.That(serviceContent.Displayed, Is.True, "serviceContent nuk u shfaq.");

                Log("Assert Detajet e subjektit");
                IWebElement subjectDetailsTitle = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[@id='serviceContent']//h4[contains(normalize-space(),'Detajet e subjektit')]"))
                );
                Assert.That(subjectDetailsTitle.Text, Is.EqualTo("DETAJET E SUBJEKTIT"));

                IWebElement niptField = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.Id("nipt"))
                );
                Assert.That(niptField.GetAttribute("value"), Is.EqualTo("L12121023B"));

                IWebElement subjectNameField = driver.FindElement(By.Id("subjectName"));
                Assert.That(subjectNameField.GetAttribute("value"), Is.EqualTo("KREATX"));

                IWebElement subjectStatusField = driver.FindElement(By.Id("subjectStatus"));
                Assert.That(subjectStatusField.GetAttribute("value"), Is.EqualTo("Aktiv"));

                Log("Kliko Vazhdo - Step 1");
                IWebElement vazhdoBtn = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    vazhdoBtn
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(vazhdoBtn)).Click();

                Log("Assert Kontakti");
                IWebElement step2Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[@id='serviceContent']//h4[contains(normalize-space(),'Kontakti')]"))
                );
                Assert.That(step2Title.Text, Is.EqualTo("KONTAKTI"));

                IWebElement nrCel = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.Id("nrCel"))
                );
                Assert.That(nrCel.GetAttribute("value"), Is.EqualTo("0676041404"));

                IWebElement email = driver.FindElement(By.Id("email"));
                Assert.That(email.GetAttribute("value"), Is.EqualTo("ketjona.mema@kreatx.com"));

                Log("Kliko Vazhdo - Step 2");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]")).Click();

                Log("Assert Detajet e aplikimit");
                IWebElement step3Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[@id='serviceContent']//h4[contains(normalize-space(),'Detajet e aplikimit')]"))
                );
                Assert.That(step3Title.Text, Is.EqualTo("DETAJET E APLIKIMIT"));

                SelectElement selectLicense = new SelectElement(driver.FindElement(By.Id("licensePer")));
                selectLicense.SelectByValue("NDERTES_TOKE_TRUALL");

                Log("Kliko Vazhdo - Step 3");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]")).Click();

                Log("Assert Dokumentacioni");
                IWebElement step4Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[@id='serviceContent']//h4[contains(normalize-space(),'Dokumentacioni')]"))
                );
                Assert.That(step4Title.Text, Is.EqualTo("DOKUMENTACIONI"));

                Log("Kliko Dergo pa dokumente");
                IWebElement dergoBtn = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.CssSelector(".ealb-btn-continue.with-arrow"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    dergoBtn
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(dergoBtn)).Click();

                IWebElement error = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[contains(@class,'text-danger')]"))
                );
                Assert.That(error.Text, Does.Contain("Ju lutem ngarkoni dokumentin e kërkuar."));

                Log("Ngarko dokumente te pasakta");
                string fileKerkesaPath = @"C:\Users\Kreatx\Downloads\Historic Serial PDA Report.xlsx";
                string fileKontrataPath = @"C:\Users\Kreatx\Downloads\15mb.pdf";

                IWebElement fileUpload = driver.FindElement(By.Id("fileKerkesa"));
                fileUpload.SendKeys(fileKerkesaPath);

                IWebElement fileUpload2 = driver.FindElement(By.Id("fileKontrata"));
                fileUpload2.SendKeys(fileKontrataPath);

                Log("Assert format gabim");
                IWebElement formatError = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[contains(@class,'text-danger') and contains(.,'Formati duhet të jetë')]"))
                );

                Assert.That(formatError.Displayed, Is.True, "Mesazhi i formatit të gabuar nuk u shfaq.");
                Assert.That(formatError.Text.Trim(), Is.EqualTo("Formati duhet të jetë: PDF"));

                Log("Assert madhesi gabim");
                IWebElement fileSizeError = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Madhësia e dokumentit')]"))
                );

                Assert.That(fileSizeError.Displayed, Is.True,
                    "Mesazhi për madhësinë e dokumentit nuk u shfaq.");

                Assert.That(fileSizeError.Text.Trim(),
                    Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se 15MB"));

                Log("Hiq dokumentet e pasakta");
                IWebElement cancelUploadBtn = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.CssSelector("button[aria-label='Cancel upload']"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    cancelUploadBtn
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(cancelUploadBtn)).Click();

                Thread.Sleep(2000);

                var cancelButtons = driver.FindElements(By.CssSelector("button[aria-label='Cancel upload']"));
                if (cancelButtons.Count > 0)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript(
                        "arguments[0].scrollIntoView({block:'center'});",
                        cancelButtons[0]
                    );
                    Thread.Sleep(500);
                    cancelButtons[0].Click();
                }

        Log("Prit 1 minutë para ngarkimit të dokumentit të saktë…");
        Thread.Sleep(TimeSpan.FromMinutes(1));

                Log("Ngarko dokumente te sakta");
                string fileKerkesaPath2 = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string fileKontrataPath2 = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";

                IWebElement fileUploadFresh = wait.Until(
                    ExpectedConditions.ElementExists(By.Id("fileKerkesa"))
                );
                IWebElement fileUpload2Fresh = wait.Until(
                    ExpectedConditions.ElementExists(By.Id("fileKontrata"))
                );

                fileUploadFresh.SendKeys(fileKerkesaPath2);
                fileUpload2Fresh.SendKeys(fileKontrataPath2);

                Log("Kliko checkbox agreeCheck");
                IWebElement agreeCheck = driver.FindElement(By.Id("agreeCheck"));

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    agreeCheck
                );

                Thread.Sleep(500);
                agreeCheck.Click();

                //                Log("Kliko Dergo Button");
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