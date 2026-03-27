using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;

[TestFixture]
public class NIDWeb_9287
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

    [Test]
    public void Aplikim_i_Ri_9287()
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
                driver.FindElement(By.Id("Nid")).SendKeys("J55728107R");
                driver.FindElement(By.Id("ServiceCode")).SendKeys("9287");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mieinstitution-mie-institution-1");
                driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
                driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
                driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

                new SelectElement(driver.FindElement(By.Id("ProfileType")))
                    .SelectByValue("Individual");

                new SelectElement(driver.FindElement(By.Id("Platform")))
                    .SelectByValue("WEB");

                Log("Click 'Load Service' button");
                driver.FindElement(By.ClassName("load-button")).Click();

                Log("Click 'Aplikim i Ri' button");
                wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div/div/div/div[1]/div/button"))).Click();

                Thread.Sleep(1000);
                Log("Assert detajet e individit");
                IWebElement detajetIndividit = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h5"))
                );
                Assert.That(detajetIndividit.Text.Trim(), Is.EqualTo("DETAJET E INDIVIDIT"));

                IWebElement nid = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nid")));
                Assert.That(InputValue(nid), Is.EqualTo("J55728107R"));

                IWebElement emri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("emri")));
                Assert.That(InputValue(emri), Is.EqualTo("Ketjona"));

                IWebElement mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("mbiemri")));
                Assert.That(InputValue(mbiemri), Is.EqualTo("Mema"));

                IWebElement atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("atesia")));
                Assert.That(InputValue(atesia), Is.EqualTo("Mersin"));

                IWebElement amesia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("memesia")));
                Assert.That(InputValue(amesia), Is.EqualTo("Aishe"));

                IWebElement gjinia = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("gjinia")));
                Assert.That(InputValue(gjinia), Is.EqualTo("Femër"));

                IWebElement statusiCivil = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("gjCiv")));
                Assert.That(InputValue(statusiCivil), Is.EqualTo("Beqare"));

                IWebElement vendlindja = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("vendlindja")));
                Assert.That(InputValue(vendlindja), Is.EqualTo("Kavajë"));

                IWebElement datelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("datelindja")));
                Assert.That(InputValue(datelindja), Is.EqualTo("28.07.1995"));

                IWebElement qarku = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("emQarku")));
                Assert.That(InputValue(qarku), Is.EqualTo("TIRANË"));

                IWebElement rrethi = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("emRrethi")));
                Assert.That(InputValue(rrethi), Is.EqualTo("KAVAJË"));

                Log("Click Vazhdo button - Step 1");
                IWebElement vazhdoBtn1 = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    vazhdoBtn1
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(vazhdoBtn1)).Click();

                Log("Assert Kontakti");
                IWebElement kontaktiTitle = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h5"))
                );
                Assert.That(kontaktiTitle.Text.Trim(), Is.EqualTo("KONTAKTI"));

                IWebElement email = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
                Assert.That(InputValue(email), Is.EqualTo("ketjona.mema@kreatx.com"));

                IWebElement phoneNumber = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nrCel")));
                Assert.That(InputValue(phoneNumber), Is.EqualTo("0676041404"));

                Thread.Sleep(500);
                Log("Click Vazhdo button - Step 2");
                IWebElement vazhdoBtn2 = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    vazhdoBtn2
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(vazhdoBtn2)).Click();

                Log("Assert Step 3");
                IWebElement step3Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h5"))
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
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[2]/div/button[2]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    dergoBtn
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(dergoBtn)).Click();

                IWebElement docErrorMessage = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div[1]/div[2]"))
                );
                Assert.That(docErrorMessage.Text, Does.Contain("Ju lutem ngarkoni dokumentin e kërkuar"));

                Log("Upload uncorrect docs");
                string fileDiploma = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
                string fileCertifikata = @"C:\Users\Kreatx\Downloads\image.png";
                string fileVetdeklarimi = @"C:\Users\Kreatx\Downloads\15mb.pdf";

                Assert.That(File.Exists(fileDiploma), Is.True, "File diploma nuk ekziston.");
                Assert.That(File.Exists(fileCertifikata), Is.True, "File certifikata nuk ekziston.");
                Assert.That(File.Exists(fileVetdeklarimi), Is.True, "File vetedeklarimi nuk ekziston.");

                IWebElement diplomaInputWrong = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'Diplomë universitare')]/following::input[@type='file'][1]"))
                );
                diplomaInputWrong.SendKeys(fileDiploma);

                IWebElement certifikataInputWrong = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'Certifikatë për kryerjen e programeve të studimit')]/following::input[@type='file'][1]"))
                );
                certifikataInputWrong.SendKeys(fileCertifikata);

                IWebElement vetedeklarimiInputWrong = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'Vetëdeklarim')]/following::input[@type='file'][1]"))
                );
                vetedeklarimiInputWrong.SendKeys(fileVetdeklarimi);

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

                // hiqi te gjitha dokumentet e gabuara derisa te mos ngelet asnje
                while (true)
                {
                    var cancelButtons = driver.FindElements(By.CssSelector("button[aria-label='Cancel upload']"));
                    if (cancelButtons.Count == 0)
                        break;

                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript(
                            "arguments[0].scrollIntoView({block:'center'});",
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

                Log("Upload Correct Docs");
                string correctFileDiploma = @"C:\Users\Kreatx\Downloads\TEST.pdf";
                string correctFileCertifikata = @"C:\Users\Kreatx\Downloads\TEST.pdf";
                string correctFileVetedeklarimi = @"C:\Users\Kreatx\Downloads\TEST.pdf";

                Assert.That(File.Exists(correctFileDiploma), Is.True, "File correct diploma nuk ekziston.");
                Assert.That(File.Exists(correctFileCertifikata), Is.True, "File correct certifikata nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimi), Is.True, "File correct vetedeklarimi nuk ekziston.");

                // Diploma
                IWebElement diplomaInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'Diplomë universitare')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                diplomaInput.SendKeys(correctFileDiploma);

                // Certifikata
                IWebElement certifikataInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'Certifikatë për kryerjen e programeve të studimit')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                certifikataInput.SendKeys(correctFileCertifikata);

                // Vetedeklarimi
                IWebElement vetedeklarimiInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'Vetëdeklarim')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                vetedeklarimiInput.SendKeys(correctFileVetedeklarimi);

                Thread.Sleep(1500);

                Log("Verify uploaded docs are present");

                // kontrollo qe file inputs kane vlere
                Assert.That(diplomaInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(certifikataInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(vetedeklarimiInput.GetAttribute("value"), Does.Contain(".pdf"));

                // kontrollo qe nuk ka me mesazhe visible error
                var visibleErrors = driver.FindElements(By.XPath("//div[contains(@class,'text-danger') and normalize-space()!='']"))
                                          .Where(e => e.Displayed)
                                          .ToList();

                Assert.That(visibleErrors.Count, Is.EqualTo(0),
                    "Ka ende gabime të dukshme pas ngarkimit të dokumenteve të sakta.");

                Log("Click 'Dergo' button");
                IWebElement dergoFinalBtn = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//button[contains(normalize-space(),'Dërgo')]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    dergoFinalBtn
                );

                Thread.Sleep(500);

                try
                {
                    wait.Until(ExpectedConditions.ElementToBeClickable(
                        By.XPath("//button[contains(normalize-space(),'Dërgo')]"))).Click();
                }
                catch (ElementClickInterceptedException)
                {
                    dergoFinalBtn = driver.FindElement(By.XPath("//button[contains(normalize-space(),'Dërgo')]"));
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", dergoFinalBtn);
                }

                Thread.Sleep(1500);

                // Kontrollo nese del popup "Kujdes!"
                var kujdesPopups = driver.FindElements(By.CssSelector(".alert-modal-container"));

                if (kujdesPopups.Count > 0 && kujdesPopups[0].Displayed)
                {
                    Log("Popup 'Kujdes' u shfaq");

                    IWebElement kujdesTitle = wait.Until(
                        ExpectedConditions.ElementIsVisible(By.CssSelector(".alert-modal-title"))
                    );
                    Assert.That(kujdesTitle.Text.Trim(), Is.EqualTo("Kujdes!"));

                    IWebElement kujdesDescription = wait.Until(
                        ExpectedConditions.ElementIsVisible(By.CssSelector(".alert-modal-description"))
                    );
                    Assert.That(
                        kujdesDescription.Text.Trim(),
                        Is.EqualTo("Ju keni nje aplikim ne proces per kete lloj license!")
                    );

                    IWebElement okBtn = wait.Until(
                        ExpectedConditions.ElementToBeClickable(By.CssSelector(".alert-modal-button--primary"))
                    );
                    okBtn.Click();

                    Log("Testi perfundon me klikimin e popup 'Kujdes!'");
                    return;
                }

                Log("Assert success page");
                IWebElement successTitle = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//h5/b[contains(normalize-space(),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]"))
                );
                Assert.That(successTitle.Displayed, Is.True);
                Assert.That(successTitle.Text.Trim(), Is.EqualTo("APLIKIMI JUAJ U DËRGUA ME SUKSES."));

                IWebElement referenceNumber = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//h6[contains(.,'Numri referencë i aplikimit është')]//b"))
                );
                Assert.That(referenceNumber.Displayed, Is.True);
                Assert.That(referenceNumber.Text.Trim(), Is.Not.Empty);

                IWebElement gjurmoBtn = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//button[contains(.,'Gjurmo Aplikimin')]"))
                );
                Assert.That(gjurmoBtn.Displayed, Is.True);

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