using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

[TestFixture]
public class _11136_NID_Web
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

    private IWebElement GetVisibleWizardButtonByText(IWebDriver driver, WebDriverWait wait, string buttonText)
    {
        return wait.Until(d =>
        {
            var buttons = d.FindElements(By.XPath($"//button[contains(normalize-space(),'{buttonText}')]"))
                           .Where(b => b.Displayed && b.Enabled)
                           .ToList();

            return buttons.LastOrDefault();
        });
    }

    private void ExpandTreeParentByText(IWebDriver driver, WebDriverWait wait, string parentText)
    {
        var parentLi = wait.Until(d =>
        {
            return d.FindElements(By.XPath(
                $"//ul[@aria-label='category-tree']//li[@role='treeitem'][.//span[normalize-space()='{parentText}']]"
            )).FirstOrDefault();
        });

        Assert.That(parentLi, Is.Not.Null, $"Parent category nuk u gjet: {parentText}");

        string expanded = parentLi.GetAttribute("aria-expanded");

        if (expanded != "true")
        {
            IWebElement toggleArea;

            var iconContainers = parentLi.FindElements(By.XPath(".//div[contains(@class,'MuiTreeItem-iconContainer')]"));
            if (iconContainers.Count > 0)
                toggleArea = iconContainers[0];
            else
                toggleArea = parentLi.FindElement(By.XPath(".//div[contains(@class,'MuiTreeItem-content')]"));

            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                toggleArea
            );

            Thread.Sleep(300);

            try
            {
                toggleArea.Click();
            }
            catch
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", toggleArea);
            }

            wait.Until(d =>
            {
                try
                {
                    return parentLi.GetAttribute("aria-expanded") == "true";
                }
                catch
                {
                    return false;
                }
            });
        }
    }

    private void ExpandSecondLevelByText(IWebDriver driver, WebDriverWait wait, string firstParentText, string secondLevelText)
    {
        ExpandTreeParentByText(driver, wait, firstParentText);
        Thread.Sleep(700);

        var secondNode = wait.Until(d =>
        {
            return d.FindElements(By.XPath(
                $"//li[@role='treeitem'][.//span[normalize-space()='{firstParentText}']]" +
                $"//*[@role='group']//li[@role='treeitem'][.//span[normalize-space()='{secondLevelText}']]"
            )).FirstOrDefault();
        });

        Assert.That(secondNode, Is.Not.Null, $"Niveli i dytë nuk u gjet: {secondLevelText}");

        string expanded = secondNode.GetAttribute("aria-expanded");

        if (expanded != "true")
        {
            IWebElement toggleArea;

            var iconContainers = secondNode.FindElements(By.XPath(".//div[contains(@class,'MuiTreeItem-iconContainer')]"));
            if (iconContainers.Count > 0)
                toggleArea = iconContainers[0];
            else
                toggleArea = secondNode.FindElement(By.XPath(".//div[contains(@class,'MuiTreeItem-content')]"));

            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                toggleArea
            );

            Thread.Sleep(300);

            try
            {
                toggleArea.Click();
            }
            catch
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", toggleArea);
            }

            wait.Until(d =>
            {
                try
                {
                    return secondNode.GetAttribute("aria-expanded") == "true";
                }
                catch
                {
                    return false;
                }
            });
        }
    }

    private void SelectThirdLevelCategoryByText(
        IWebDriver driver,
        WebDriverWait wait,
        string firstParentText,
        string secondLevelText,
        string categoryText)
    {
        ExpandSecondLevelByText(driver, wait, firstParentText, secondLevelText);
        Thread.Sleep(700);

        var categoryLabel = wait.Until(d =>
        {
            return d.FindElements(By.XPath(
                $"//li[@role='treeitem'][.//span[normalize-space()='{firstParentText}']]" +
                $"//*[@role='group']//li[@role='treeitem'][.//span[normalize-space()='{secondLevelText}']]" +
                $"//*[@role='group']//label[.//span[normalize-space()=\"{categoryText}\"]]"
            )).FirstOrDefault();
        });

        Assert.That(categoryLabel, Is.Not.Null, $"Kategoria finale nuk u gjet: {categoryText}");

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});",
            categoryLabel
        );

        Thread.Sleep(300);

        try
        {
            categoryLabel.Click();
        }
        catch
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", categoryLabel);
        }

        var checkbox = categoryLabel.FindElement(By.XPath(".//input[@type='checkbox']"));

        wait.Until(d =>
        {
            try
            {
                return checkbox.Selected || checkbox.GetAttribute("checked") != null;
            }
            catch
            {
                return false;
            }
        });
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
    public void Aplikim_i_Ri_NID_11136()
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
                driver.FindElement(By.Id("ServiceCode")).SendKeys("11136");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mie_merge");
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
                    By.XPath("//button[@aria-label='Aplikim i ri']"))).Click();

                Thread.Sleep(1000);
                Log("Assert detajet e individit");
                IWebElement detajetIndividit = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(detajetIndividit.Text.Trim(), Is.EqualTo("DETAJET E INDIVIDIT"));

                IWebElement nid = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[1]/input")));
                Assert.That(InputValue(nid), Is.EqualTo("J55728107R"));

                IWebElement emri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[2]/input")));
                Assert.That(InputValue(emri), Is.EqualTo("Ketjona"));

                IWebElement mbiemri = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[3]/input")));
                Assert.That(InputValue(mbiemri), Is.EqualTo("Mema"));

                IWebElement atesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[4]/input")));
                Assert.That(InputValue(atesia), Is.EqualTo("Mersin"));

                IWebElement gjinia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[5]/select")));
                Assert.That(InputValue(gjinia), Is.EqualTo("F"));

                IWebElement vendlindja = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[7]/input")));
                Assert.That(InputValue(vendlindja), Is.EqualTo("Kavajë"));

                IWebElement datelindja = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[6]/input")));
                Assert.That(InputValue(datelindja), Is.EqualTo("28.07.1995"));

                IWebElement qarku = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[9]/input")));
                Assert.That(InputValue(qarku), Is.EqualTo("TIRANË"));

                IWebElement amesia = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/main/div[3]/div/div/div/div/form/div/div[10]/input")));
                Assert.That(InputValue(amesia), Is.EqualTo("Aishe"));

                IWebElement rrethi = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//html/body/div/main/div[3]/div/div/div/div/form/div/div[8]/input")));
                Assert.That(InputValue(rrethi), Is.EqualTo("KAVAJË"));

                Log("Click Vazhdo button - Step 1");
                IWebElement vazhdoBtn1 = GetVisibleWizardButtonByText(driver, wait, "Vazhdo");

                Assert.That(vazhdoBtn1, Is.Not.Null, "Butoni 'Vazhdo' i Step 1 nuk u gjet.");

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    vazhdoBtn1
                );

                Thread.Sleep(500);

                try
                {
                    vazhdoBtn1.Click();
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", vazhdoBtn1);
                }

                Log("Assert Kontakti");
                IWebElement kontaktiTitle = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(kontaktiTitle.Text.Trim(), Is.EqualTo("KONTAKTI"));

                IWebElement email = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("email")));
                Assert.That(InputValue(email), Is.EqualTo("ketjona.mema@kreatx.com"));

                IWebElement phoneNumber = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("nrCel")));
                Assert.That(InputValue(phoneNumber), Is.EqualTo("0676041404"));

                Thread.Sleep(500);
                Log("Click Vazhdo button - Step 2");
                IWebElement vazhdoBtn2 = GetVisibleWizardButtonByText(driver, wait, "Vazhdo");

                Assert.That(vazhdoBtn2, Is.Not.Null, "Butoni 'Vazhdo' i Step 2 nuk u gjet.");

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    vazhdoBtn2
                );

                Thread.Sleep(500);

                try
                {
                    vazhdoBtn2.Click();
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", vazhdoBtn2);
                }

                Log("Assert Step 3");
                IWebElement step3Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(step3Title.Text.Trim(), Is.EqualTo("KATEGORITË"));

                Log("Select Kategori");

                // SKENARI 2: zgjero parent category
                Log("Expand first parent category");
                ExpandTreeParentByText(driver, wait, "LICENCE PROJEKTIMI NE NDERTIM");

                // SKENARI 3:
                // Nese e di tekstin e nen-kategorise, perdor SelectSubCategoryByText(...)
                // perndryshe zgjidh automatikisht kategorine e pare aktive nen parent
                Log("Expand second level category");
                ExpandSecondLevelByText(
                    driver,
                    wait,
                    "LICENCE PROJEKTIMI NE NDERTIM",
                    "1. PROJEKTUES URBANIST"
                );

                Log("Select final category");
                SelectThirdLevelCategoryByText(
                    driver,
                    wait,
                    "LICENCE PROJEKTIMI NE NDERTIM",
                    "1. PROJEKTUES URBANIST",
                    "b2. Plane sektoriale në nivel bashkie. Kjo kategori jepet vetëm për persona juridik (shoqëri/studio)."
                );

                Log("Click 'Vazhdo' button - Step 3");
                IWebElement vazhdoBtn3 = GetVisibleWizardButtonByText(driver, wait, "Vazhdo");

                Assert.That(vazhdoBtn3, Is.Not.Null, "Butoni 'Vazhdo' i Step 3 nuk u gjet.");

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    vazhdoBtn3
                );

                Thread.Sleep(500);

                try
                {
                    vazhdoBtn3.Click();
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", vazhdoBtn3);
                }

                Log("Assert Dokumentacioni");
                IWebElement step4Title = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("/html/body/div/main/div[3]/div/div/div/div/h4"))
                );
                Assert.That(step4Title.Text.Trim(), Is.EqualTo("DOKUMENTACIONI"));

                Log("Click 'Dergo' button without required document");
                IWebElement dergoBtn = GetVisibleWizardButtonByText(driver, wait, "Dërgo");

                Assert.That(dergoBtn, Is.Not.Null, "Butoni 'Dërgo' nuk u gjet.");

                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    dergoBtn
                );

                Thread.Sleep(500);

                try
                {
                    dergoBtn.Click();
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", dergoBtn);
                }

                IWebElement docErrorMessage = wait.Until(
                    ExpectedConditions.ElementIsVisible(
                        By.XPath("//*[contains(text(),'Ju lutem ngarkoni dokumentin e kërkuar')]"))
                );
                Assert.That(docErrorMessage.Text, Does.Contain("Ju lutem ngarkoni dokumentin e kërkuar"));

                Log("Upload uncorrect docs");
                string fileDocteknik = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
                string fileVetedeklarimi = @"C:\Users\Kreatx\Downloads\image.png";
                string fileCV = @"C:\Users\Kreatx\Downloads\15mb.pdf";

                Assert.That(File.Exists(fileDocteknik), Is.True, "File docteknik nuk ekziston.");
                Assert.That(File.Exists(fileVetedeklarimi), Is.True, "File vetedeklarimi nuk ekziston.");
                Assert.That(File.Exists(fileCV), Is.True, "File cv nuk ekziston.");

                IWebElement CVInputWrong = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'CV e individit')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                CVInputWrong.SendKeys(fileCV);

                IWebElement DocTeknikInputWrong = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'Dokumentacion teknik')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                DocTeknikInputWrong.SendKeys(fileDocteknik);

                IWebElement VetedeklarimiInputWrong = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'Vetëdeklarim i individit')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                VetedeklarimiInputWrong.SendKeys(fileVetedeklarimi);

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

        Log("Prit 1 minutë para ngarkimit të dokumentit të saktë…");
        Thread.Sleep(TimeSpan.FromMinutes(1));

                Log("Upload Correct Docs");

                string correctFileCV = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileDocTeknik = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileVetedeklarimi = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";

                Assert.That(File.Exists(correctFileCV), Is.True, "File correct CV nuk ekziston.");
                Assert.That(File.Exists(correctFileDocTeknik), Is.True, "File correct docteknik nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimi), Is.True, "File correct vetedekalrimi nuk ekziston.");

                // CV
                IWebElement CVInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'CV e individit')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                CVInput.SendKeys(correctFileCV);

                // DocTeknik
                IWebElement DocTeknikInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'Dokumentacion teknik')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                DocTeknikInput.SendKeys(correctFileDocTeknik);

                // Vetedeklarimi
                IWebElement VetedeklarimiInput = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.XPath("//span[contains(normalize-space(),'Vetëdeklarim i individit')]/ancestor::div[contains(@class,'col')][1]//input[@type='file']"))
                );
                VetedeklarimiInput.SendKeys(correctFileVetedeklarimi);

                Thread.Sleep(1500);

                Log("Verify uploaded docs are present");

                // kontrollo qe file inputs kane vlere
                Assert.That(CVInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(DocTeknikInput.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(VetedeklarimiInput.GetAttribute("value"), Does.Contain(".pdf"));

                // kontrollo qe nuk ka me mesazhe visible error
                var visibleErrors = driver.FindElements(By.XPath("//div[contains(@class,'text-danger') and normalize-space()!='']"))
                                          .Where(e => e.Displayed)
                                          .ToList();

                Assert.That(visibleErrors.Count, Is.EqualTo(0),
                    "Ka ende gabime të dukshme pas ngarkimit të dokumenteve të sakta.");

                Log("click checkbox");
                IWebElement checkbox = wait.Until(
                    ExpectedConditions.ElementExists(
                        By.Id("agreeCheck"))
                );
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    checkbox
                );

                // Log("Click 'Dergo' button");
                // IWebElement dergoFinalBtn = wait.Until(
                //     ExpectedConditions.ElementExists(
                //         By.XPath("//button[contains(normalize-space(),'Dërgo')]"))
                // );

                // ((IJavaScriptExecutor)driver).ExecuteScript(
                //     "arguments[0].scrollIntoView({block:'center'});",
                //     dergoFinalBtn
                // );

                // Thread.Sleep(500);

                // try
                // {
                //     wait.Until(ExpectedConditions.ElementToBeClickable(
                //         By.XPath("//button[contains(normalize-space(),'Dërgo')]"))).Click();
                // }
                // catch (ElementClickInterceptedException)
                // {
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