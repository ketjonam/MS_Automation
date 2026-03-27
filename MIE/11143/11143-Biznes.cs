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
public class _11143_BiznesWEB
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
    public void Mbyllje_Aktiviteti_11143()
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
                driver.FindElement(By.Id("ServiceCode")).SendKeys("11143");
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

                IWebElement nipt = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nipt")));
                Assert.That(InputValue(nipt), Is.EqualTo("L12121023B"));

                IWebElement EmriSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("emriSubjektit")));
                Assert.That(InputValue(EmriSubjektit), Is.EqualTo("KREATX"));

                IWebElement DtRegjistrimit = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("dataRregjistrimit")));
                Assert.That(InputValue(DtRegjistrimit), Is.EqualTo("21.09.2011"));

                IWebElement StatusiSubjektit = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("statusi")));
                Assert.That(InputValue(StatusiSubjektit), Is.EqualTo("Aktiv"));

                IWebElement Administratori = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("perfaqesuesi")));
                Assert.That(InputValue(Administratori), Is.EqualTo("Enor  Vlash  Nakuçi"));

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
                Assert.That(InputValue(email), Is.EqualTo("ketjona.mema@kreatx.com"));

                IWebElement phoneNumber = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.Name("mobile"))
                );
                Assert.That(InputValue(phoneNumber), Is.EqualTo("0676041404"));

                Thread.Sleep(500);
                Log("Click checkbox per mbylljen e aktivitetit - Step 2");

                IWebElement checkbox = wait.Until(
                    ExpectedConditions.ElementExists(By.Id("confirmClosure"))
                );
                ((IJavaScriptExecutor)driver).ExecuteScript(
    "arguments[0].scrollIntoView({ block: 'center' });",
    checkbox
);

                Thread.Sleep(500);

                if (!checkbox.Selected)
                {
                    try
                    {
                        wait.Until(ExpectedConditions.ElementToBeClickable(checkbox)).Click();
                    }
                    catch
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", checkbox);
                    }
                }




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
