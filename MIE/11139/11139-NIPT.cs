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
public class _11139_NIPT
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
    public void Aplikim_i_Ri_Biznes_11139()
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
                driver.FindElement(By.Id("ServiceCode")).SendKeys("11139");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mieinstitution-mie-institution-1");
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
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div/div/div/div[1]/div/button/div/div[1]"))).Click();

                Thread.Sleep(8000);
                Log("Assert detajet e subjektit");
                IWebElement DetajeteSubjektit = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h5"))
                );
                Assert.That(DetajeteSubjektit.Text.Trim(), Is.EqualTo("DETAJET E SUBJEKTIT"));

                IWebElement nipt = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("nipt")));
                Assert.That(InputValue(nipt), Is.EqualTo("L12121023B"));

                IWebElement EmriSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/input")));
                Assert.That(InputValue(EmriSubjektit), Is.EqualTo("KREATX"));

                IWebElement DtRegjistrimit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[3]/input")));
                Assert.That(InputValue(DtRegjistrimit), Is.EqualTo("21.09.2011"));

                IWebElement StatusiSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[5]/input")));
                Assert.That(InputValue(StatusiSubjektit), Is.EqualTo("Aktiv"));

                IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[4]/input")));
                Assert.That(InputValue(Administratori), Is.EqualTo("Enor  Vlash  Nakuçi |"));

                Log("Click Vazhdo button - Step 1");
                IWebElement vazhdoBtn1 = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"))
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
                        By.Name("email"))
                );
                Assert.That(InputValue(email), Is.EqualTo("info@kreatx.com"));

                IWebElement phoneNumber = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.Name("telCel"))
                );
                Assert.That(InputValue(phoneNumber), Is.EqualTo("+35544200600"));

                Thread.Sleep(500);
                Log("Click Vazhdo button - Step 2");
                driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]")).Click();

                Log("Assert Dokumentacioni");
                IWebElement step4Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

                Log("Click 'Dergo' button without required document");
                IWebElement dergoBtn = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/button[2]"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({ block: 'center' });",
                    dergoBtn
                );

                Thread.Sleep(500);
                wait.Until(ExpectedConditions.ElementToBeClickable(dergoBtn)).Click();

                IWebElement docErrorMessage = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[1]/div[1]/div/div[1]/div[2]"))
                );
                Assert.That(docErrorMessage.Text, Does.Contain("Ju lutem ngarkoni dokumentin e kërkuar"));

                Log("Upload uncorrect docs");
                string fileVetedeklarim = @"C:\Users\Kreatx\Downloads\image.png";
                string filePagesaTarifes = @"C:\Users\Kreatx\Downloads\15mb.pdf";

                Assert.That(File.Exists(fileVetedeklarim), Is.True, "File Vetedeklarim nuk ekziston.");
                Assert.That(File.Exists(filePagesaTarifes), Is.True, "File PagesaTarifes nuk ekziston.");

                var wrongFileInputs = wait.Until(d =>
                {
                    var els = d.FindElements(By.XPath("//input[@type='file']"));
                    return els.Count >= 2 ? els : null;
                });

                wrongFileInputs[0].SendKeys(fileVetedeklarim);
                wrongFileInputs[1].SendKeys(filePagesaTarifes);

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

                Log("Upload Correct Docs");
                string correctFileVetedeklarimi = @"C:\Users\Kreatx\Downloads\TEST.pdf";
                string correctFilePagesaTarifes = @"C:\Users\Kreatx\Downloads\TEST.pdf";

               
                Assert.That(File.Exists(correctFileVetedeklarimi), Is.True, "File correct vetedeklarimi nuk ekziston.");
                Assert.That(File.Exists(correctFilePagesaTarifes), Is.True, "File correct pagesa tarifes nuk ekziston.");

                var correctFileInputs = wait.Until(d =>
                {
                    var els = d.FindElements(By.XPath("//input[@type='file']"));
                    return els.Count >= 2 ? els : null;
                });

                correctFileInputs[0].SendKeys(correctFileVetedeklarimi);
                correctFileInputs[1].SendKeys(correctFilePagesaTarifes);

                Thread.Sleep(1500);

                Log("Verify uploaded docs are present");
                Assert.That(correctFileInputs[0].GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(correctFileInputs[1].GetAttribute("value"), Does.Contain(".pdf"));

                var visibleErrors = driver.FindElements(
                        By.XPath("//div[contains(@class,'text-danger') and normalize-space()!='']"))
                    .Where(e => e.Displayed)
                    .ToList();

                Assert.That(visibleErrors.Count, Is.EqualTo(0),
                    "Ka ende gabime të dukshme pas ngarkimit të dokumenteve të sakta.");

                Log("Click checkbox");
                IWebElement checkbox = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.Id("agreeCheck"))
                );

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({ block: 'center' });",
                    checkbox
                );

                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", checkbox);

                //Log("Click 'Dergo' button");
                //IWebElement dergoFinalBtn = wait.Until(
                //    ExpectedConditions.ElementExists(
                //        By.XPath("//button[contains(normalize-space(),'Dërgo')]"))
                //);

                //((IJavaScriptExecutor)driver).ExecuteScript(
                //    "arguments[0].scrollIntoView({ block: 'center' });",
                //    dergoFinalBtn
                //);

                //Thread.Sleep(500);

                //try
                //{
                //    wait.Until(ExpectedConditions.ElementToBeClickable(
                //        By.XPath("//button[contains(normalize-space(),'Dërgo')]"))).Click();
                //}
                //catch (ElementClickInterceptedException)
                //{
                //    dergoFinalBtn = driver.FindElement(By.XPath("//button[contains(normalize-space(),'Dërgo')]"));
                //    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", dergoFinalBtn);
                //}

                //Thread.Sleep(1500);

                //var kujdesPopups = driver.FindElements(By.CssSelector(".alert-modal-container"));

                //if (kujdesPopups.Count > 0 && kujdesPopups[0].Displayed)
                //{
                //    Log("Popup 'Kujdes' u shfaq");

                //    IWebElement kujdesTitle = wait.Until(
                //        ExpectedConditions.ElementIsVisible(By.CssSelector(".alert-modal-title"))
                //    );
                //    Assert.That(kujdesTitle.Text.Trim(), Is.EqualTo("Kujdes!"));

                //    IWebElement kujdesDescription = wait.Until(
                //        ExpectedConditions.ElementIsVisible(By.CssSelector(".alert-modal-description"))
                //    );
                //    Assert.That(
                //        kujdesDescription.Text.Trim(),
                //        Is.EqualTo("Ju keni nje aplikim ne proces per kete lloj license!")
                //    );

                //    IWebElement okBtn = wait.Until(
                //        ExpectedConditions.ElementToBeClickable(By.CssSelector(".alert-modal-button--primary"))
                //    );
                //    okBtn.Click();

                //    Log("Testi perfundon me klikimin e popup 'Kujdes!'");
                //    return;
                //}

                //Log("Assert success page");
                //IWebElement successTitle = wait.Until(
                //    ExpectedConditions.ElementIsVisible(
                //        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[1]/h5"))
                //);
                //Assert.That(successTitle.Displayed, Is.True);
                //Assert.That(successTitle.Text.Trim(), Is.EqualTo("APLIKIMI JUAJ U KRYE ME SUKSES."));

                //IWebElement referenceNumber = wait.Until(
                //    ExpectedConditions.ElementIsVisible(
                //        By.XPath("//h6[contains(.,'Numri referencë i aplikimit është')]//b"))
                //);
                //Assert.That(referenceNumber.Displayed, Is.True);
                //Assert.That(referenceNumber.Text.Trim(), Is.Not.Empty);

                //Log("Assert that in success page is GjurmoBtn, PaguajOnlline, ShkarkoMandatin");
                //IWebElement gjurmoBtn = wait.Until(
                //    ExpectedConditions.ElementIsVisible(
                //        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[1]/div/button"))
                //);
                //Assert.That(gjurmoBtn.Displayed, Is.True);

                //IWebElement PayOnline = wait.Until(
                //    ExpectedConditions.ElementIsVisible(
                //        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/div/button[1]"))
                //);
                //Assert.That(PayOnline.Displayed, Is.True);

                //IWebElement ShkarkoMandatin = wait.Until(
                //    ExpectedConditions.ElementIsVisible(
                //        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div/div/div/button[2]"))
                //);
                //Assert.That(ShkarkoMandatin.Displayed, Is.True);

                //Log("Click 'gjurmoBtn' button");
                //((IJavaScriptExecutor)driver).ExecuteScript(
                //    "arguments[0].scrollIntoView({block:'center'});",
                //    gjurmoBtn
                //);

                //Thread.Sleep(500);

                //try
                //{
                //    wait.Until(ExpectedConditions.ElementToBeClickable(
                //        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[1]/div/button"))).Click();
                //}
                //catch (ElementClickInterceptedException)
                //{
                //    gjurmoBtn = driver.FindElement(
                //        By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[1]/div/button"));
                //    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", gjurmoBtn);
                //}

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