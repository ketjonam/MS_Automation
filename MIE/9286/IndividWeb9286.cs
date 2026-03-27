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
public class Individ_Web_9286
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

    private static string InputValue(IWebElement element) =>
        element.GetAttribute("value")?.Trim() ?? string.Empty;

    [Test]
    public void Aplikim_i_Ri_9286()
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
                driver.FindElement(By.Id("ServiceCode")).SendKeys("9286");
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
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
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
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(kontaktiTitle.Text.Trim(), Is.EqualTo("KONTAKTI"));

                IWebElement email = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
                Assert.That(InputValue(email), Is.EqualTo("ketjona.mema@kreatx.com"));

                IWebElement phoneNumber = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nrCel")));
                Assert.That(InputValue(phoneNumber), Is.EqualTo("0676041404"));

                Thread.Sleep(500);
                Log("Click Vazhdo button - Step 1");
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
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(
                    step3Title.Text.Trim(),
                    Is.EqualTo("DETAJET E APLIKIMIT PËR LICENCË INDIVIDUALE TË SHKALLËS SË PARË (RRITJE SHKALLE NGA II NË TË I)")
                ); 

                Log("Click 'Vazhdo' without selected required field");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/button[2]")).Click();

                IWebElement errorMessage = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/span"))
                );
                Assert.That(errorMessage.Text.Trim(), Is.EqualTo("Përzgjidhni një vlerë për të vazhduar"));

                IWebElement licenseTypeDropdown = wait.Until(
                    ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/select"))
                );
                new SelectElement(licenseTypeDropdown)
                    .SelectByValue("TOKE_BUJQESORE_PYJE_LIVADH");

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
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/div/button[2]"))
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
                Assert.That(docErrorMessage.Text, Does.Contain("Ju lutemi ngarkoni dokumentin e kërkuar"));

                Log("Upload uncorrect docs");
                string fileCV = @"C:\Users\Kreatx\Downloads\Historic Serial PDA Report.xlsx";
                string fileObjekteteVleresuara = @"C:\Users\Kreatx\Downloads\image.png";
                string fileReferenca = @"C:\Users\Kreatx\Downloads\15mb.pdf";
                string fileRaportet = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
                string fileVetedeklarimi = @"C:\Users\Kreatx\Downloads\TEST.pdf";

                var fileInputs = wait.Until(d =>
                {
                    var els = d.FindElements(By.XPath("//input[@type='file']"));
                    return els.Count >= 5 ? els : null;
                });

                fileInputs[0].SendKeys(fileCV);
                fileInputs[1].SendKeys(fileObjekteteVleresuara);
                fileInputs[2].SendKeys(fileReferenca);
                fileInputs[3].SendKeys(fileRaportet);
                fileInputs[4].SendKeys(fileVetedeklarimi);

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
                string correctFileCV = @"C:\Users\Kreatx\Downloads\TEST.pdf";
                string correctFileObjekteteVleresuara = @"C:\Users\Kreatx\Downloads\TEST.pdf";
                string correctFileReferenca = @"C:\Users\Kreatx\Downloads\TEST.pdf";
                string correctFileRaportet = @"C:\Users\Kreatx\Downloads\TEST.pdf";
                string correctFileVetedeklarimi = @"C:\Users\Kreatx\Downloads\TEST.pdf";

                Assert.That(File.Exists(correctFileCV), Is.True, "File correct CV nuk ekziston.");
                Assert.That(File.Exists(correctFileObjekteteVleresuara), Is.True, "File correct Objektet e Vleresuara nuk ekziston.");
                Assert.That(File.Exists(correctFileReferenca), Is.True, "File correct Referenca nuk ekziston.");
                Assert.That(File.Exists(correctFileRaportet), Is.True, "File correct Raportet nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimi), Is.True, "File correct Vetedeklarimi nuk ekziston.");

                // CV
                IWebElement cvInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'Curriculum Vitae') or contains(.,'CV')]/following::input[@type='file'][1]"))
                );
                cvInput.SendKeys(correctFileCV);

                // Objektet e vleresuara
                IWebElement objekteInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'objektete') or contains(.,'Objektete') or contains(.,'objekteve')]/following::input[@type='file'][1]"))
                );
                objekteInput.SendKeys(correctFileObjekteteVleresuara);

                // Referenca
                IWebElement referencaInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'Referenca') or contains(.,'referenca')]/following::input[@type='file'][1]"))
                );
                referencaInput.SendKeys(correctFileReferenca);

                // Raportet
                IWebElement raporteInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'Raportet') or contains(.,'raporteve')]/following::input[@type='file'][1]"))
                );
                raporteInput.SendKeys(correctFileRaportet);

                // Vetedeklarimi
                IWebElement vetedeklarimiInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//div[contains(.,'Vetëdeklarimi') or contains(.,'Vetedeklarimi')]/following::input[@type='file'][1]"))
                );
                vetedeklarimiInput.SendKeys(correctFileVetedeklarimi);

                Thread.Sleep(1500);

                Log("Verify uploaded docs are present");

                // kontrollo qe file inputs kane vlere
                Assert.That(cvInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(objekteInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(referencaInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(raporteInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(vetedeklarimiInput.GetAttribute("value"), Does.Contain(".pdf"));

                // kontrollo qe nuk ka me gabime visible
                var visibleErrors = driver.FindElements(
                        By.XPath("//div[contains(@class,'text-danger') and normalize-space()!='']"))
                    .Where(e => e.Displayed)
                    .ToList();

                Assert.That(visibleErrors.Count, Is.EqualTo(0),
                    "Ka ende gabime të dukshme pas ngarkimit të dokumenteve të sakta.");

                Log("Click checkbox of adm docs");

                IWebElement agreeCheckLabel = wait.Until(
                    ExpectedConditions.ElementExists(By.CssSelector("label[for='agreeCheck']"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    agreeCheckLabel
                );

                Thread.Sleep(500);
                agreeCheckLabel.Click();

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

                // kontrollo nese del popup "Kujdes!"
                // kontrollo nese del popup "Kujdes!"
                var kujdesPopups = driver.FindElements(By.CssSelector(".alert-modal-container"));

                if (kujdesPopups.Count > 0 && kujdesPopups[0].Displayed)
                {
                    Log("Popup 'Kujdes' u shfaq");

                    IWebElement kujdesTitle = wait.Until(
                        ExpectedConditions.ElementIsVisible(
                            By.CssSelector(".alert-modal-title"))
                    );
                    Assert.That(kujdesTitle.Text.Trim(), Is.EqualTo("Kujdes!"));

                    IWebElement kujdesDescription = wait.Until(
                        ExpectedConditions.ElementIsVisible(
                            By.CssSelector(".alert-modal-description"))
                    );
                    Assert.That(
                        kujdesDescription.Text.Trim(),
                        Is.EqualTo("Ju keni nje aplikim ne proces per kete lloj license!")
                    );

                    IWebElement okBtn = wait.Until(
                        ExpectedConditions.ElementToBeClickable(
                            By.CssSelector(".alert-modal-button.alert-modal-button--primary"))
                    );
                    okBtn.Click();

                    Log("Testi perfundoj ne klikim te 'OK' ne popup 'Kujdes!'");
                    return;
                }

                Log("Assert success page");
                IWebElement successTitle = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//h5/b[contains(normalize-space(),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]"))
                );
                Assert.That(successTitle.Displayed, Is.True);
                Assert.That(successTitle.Text.Trim(), Does.StartWith("APLIKIMI JUAJ U DËRGUA ME SUKSES"));

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