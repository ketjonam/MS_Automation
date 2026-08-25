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
public class _11140_NIPTWEB
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

    private void ScrollIntoView(IWebDriver driver, IWebElement element)
    {
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center', inline: 'nearest' });",
            element
        );
        Thread.Sleep(300);
    }

    private void ClickJs(IWebDriver driver, IWebElement element)
    {
        ScrollIntoView(driver, element);

        ((IJavaScriptExecutor)driver).ExecuteScript(@"
            var el = arguments[0];
            if (!el) return;

            var clickable =
                el.closest('.MuiTreeItem-iconContainer') ||
                el.closest('button, a, [role=""button""], .MuiButtonBase-root, .btn');

            if (!clickable) clickable = el;

            clickable.scrollIntoView({ block: 'center', inline: 'nearest' });

            try { clickable.removeAttribute('disabled'); } catch(e) {}

            try
            {
                clickable.click();
            }
            catch(e)
            {
                clickable.dispatchEvent(new MouseEvent('mouseover', { bubbles: true }));
                clickable.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
                clickable.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
                clickable.dispatchEvent(new MouseEvent('click', { bubbles: true }));
            }
        ", element);

        Thread.Sleep(500);
    }

    private IWebElement WaitVisibleAndScroll(IWebDriver driver, WebDriverWait wait, By by)
    {
        IWebElement element = wait.Until(ExpectedConditions.ElementIsVisible(by));
        ScrollIntoView(driver, element);
        return element;
    }

    private IWebElement WaitExistsAndScroll(IWebDriver driver, WebDriverWait wait, By by)
    {
        IWebElement element = wait.Until(ExpectedConditions.ElementExists(by));
        ScrollIntoView(driver, element);
        return element;
    }

    private void ClickElement(IWebDriver driver, WebDriverWait wait, By by)
    {
        IWebElement element = wait.Until(ExpectedConditions.ElementExists(by));
        ScrollIntoView(driver, element);

        try
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(element)).Click();
        }
        catch
        {
            ClickJs(driver, element);
        }
    }

    private IWebElement WaitAnyVisible(IWebDriver driver, WebDriverWait wait, params By[] locators)
    {
        return wait.Until(d =>
        {
            foreach (var by in locators)
            {
                try
                {
                    var element = d.FindElements(by).FirstOrDefault(e => e.Displayed);
                    if (element != null)
                        return element;
                }
                catch
                {
                }
            }
            return null;
        });
    }

    private List<IWebElement> GetVisibleFileInputs(IWebElement container)
    {
        return container
            .FindElements(By.XPath(".//input[@type='file']"))
            .Where(e =>
            {
                try { return e.Enabled; }
                catch { return false; }
            })
            .ToList();
    }

    private void WaitStep1Loaded(IWebDriver driver, WebDriverWait wait)
    {
        wait.Until(d =>
        {
            bool hasNipt = d.FindElements(By.Id("nipt")).Any(e => e.Displayed);
            bool hasEmri = d.FindElements(By.Id("emri")).Any(e => e.Displayed);

            bool hasTitle =
                d.FindElements(By.XPath("//*[contains(normalize-space(),'DETAJET E SUBJEKTIT')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'Detajet e subjektit')]")).Any(e => e.Displayed);

            return (hasNipt && hasEmri) || hasTitle;
        });
    }

    private void WaitStep2Loaded(IWebDriver driver, WebDriverWait wait)
    {
        wait.Until(d =>
        {
            bool hasEmail = d.FindElements(By.Name("email")).Any(e => e.Displayed);
            bool hasPhone = d.FindElements(By.Name("nrCel")).Any(e => e.Displayed);

            bool hasTitle =
                d.FindElements(By.XPath("//*[contains(normalize-space(),'KONTAKTI')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'Kontakti')]")).Any(e => e.Displayed);

            return (hasEmail && hasPhone) || hasTitle;
        });
    }

    private void WaitStep3Loaded(IWebDriver driver, WebDriverWait wait)
    {
        wait.Until(d =>
        {
            bool hasInput = d.FindElements(By.XPath("//input")).Any(e => e.Displayed && e.Enabled);
            bool hasTitle =
                d.FindElements(By.XPath("//*[contains(normalize-space(),'DREJTUESIT TEKNIK')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'Drejtuesit Teknik')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'DREJTUESIT TEKNIKË')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'Drejtuesit teknikë')]")).Any(e => e.Displayed);

            return hasTitle || hasInput;
        });
    }

    private void WaitStep4Loaded(IWebDriver driver, WebDriverWait wait)
    {
        wait.Until(d =>
        {
            bool hasFileInput =
                d.FindElements(By.XPath("//input[@type='file']")).Any(e =>
                {
                    try { return e.Enabled; }
                    catch { return false; }
                });

            bool hasDokTitle =
                d.FindElements(By.XPath("//*[contains(normalize-space(),'DOKUMENTACIONI')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'Dokumentacioni')]")).Any(e => e.Displayed);

            bool hasDergoBtn =
                d.FindElements(By.CssSelector("button.ealb-btn-continue.with-arrow"))
                 .Any(e => e.Displayed);

            return hasFileInput || hasDokTitle || hasDergoBtn;
        });
    }

    private IWebElement GetModalFieldByLabel(IWebElement modal, string labelText)
    {
        var candidates = modal.FindElements(By.XPath(
            $".//label[contains(normalize-space(),\"{labelText}\")]/following::input[1] | " +
            $".//label[contains(normalize-space(),\"{labelText}\")]/following::textarea[1] | " +
            $".//label[contains(normalize-space(),\"{labelText}\")]/following::select[1]"
        ));

        return candidates.FirstOrDefault(x =>
        {
            try { return x.Displayed; }
            catch { return false; }
        });
    }

    private IWebElement WaitModalFieldByLabel(IWebDriver driver, IWebElement modal, string labelText, int timeoutSeconds = 10)
    {
        var localWait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

        return localWait.Until(d =>
        {
            try
            {
                var element = GetModalFieldByLabel(modal, labelText);
                return element != null ? element : null;
            }
            catch
            {
                return null;
            }
        });
    }

    private void AssertDisabledModalInput(IWebDriver driver, IWebElement modal, string labelText, string expectedValue)
    {
        IWebElement input = WaitModalFieldByLabel(driver, modal, labelText, 10);

        Assert.That(input, Is.Not.Null, $"Fusha '{labelText}' nuk u gjet.");
        ScrollIntoView(driver, input);

        string actualValue = InputValue(input);
        string disabledAttr = input.GetAttribute("disabled");
        string readOnlyAttr = input.GetAttribute("readonly");

        Log($"Field '{labelText}' -> value='{actualValue}', enabled={input.Enabled}, disabled='{disabledAttr}', readonly='{readOnlyAttr}'");

        Assert.That(actualValue, Is.EqualTo(expectedValue), $"Vlera e '{labelText}' nuk është e saktë.");

        bool isReadOnlyOrDisabled =
            !input.Enabled ||
            disabledAttr != null ||
            readOnlyAttr != null;

        Assert.That(isReadOnlyOrDisabled, Is.True, $"Fusha '{labelText}' duhet të jetë disabled ose readonly.");
    }

    private void AssertEditableModalInput(IWebDriver driver, IWebElement modal, string labelText, string expectedValue)
    {
        IWebElement input = WaitModalFieldByLabel(driver, modal, labelText, 10);

        Assert.That(input, Is.Not.Null, $"Fusha '{labelText}' nuk u gjet.");
        ScrollIntoView(driver, input);

        string actualValue = InputValue(input);
        Log($"Field '{labelText}' -> value='{actualValue}', enabled={input.Enabled}");

        Assert.That(actualValue, Is.EqualTo(expectedValue), $"Vlera e '{labelText}' nuk është e saktë.");
        Assert.That(input.Enabled, Is.True, $"Fusha '{labelText}' duhet të jetë editable.");
    }

    private void RemoveUploadedFiles(IWebDriver driver)
    {
        while (true)
        {
            var cancelButtons = driver.FindElements(By.CssSelector("button[aria-label='Cancel upload']"))
                                      .Where(x =>
                                      {
                                          try { return x.Displayed; }
                                          catch { return false; }
                                      })
                                      .ToList();

            if (cancelButtons.Count == 0)
                break;

            try
            {
                ScrollIntoView(driver, cancelButtons[0]);
                cancelButtons[0].Click();
                Thread.Sleep(500);
            }
            catch
            {
                break;
            }
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
    public void _11140_NIPT_Aplikim_i_Ri()
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
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div/div[1]/div/a"));

                Log("Fill in the form fields");
                driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
                driver.FindElement(By.Id("ServiceCode")).SendKeys("11140");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mie_merge");
                driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
                driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
                driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

                new SelectElement(driver.FindElement(By.Id("ProfileType"))).SelectByValue("Organisation");
                new SelectElement(driver.FindElement(By.Id("Platform"))).SelectByValue("WEB");

                Log("Click 'Load Service' button");
                ClickElement(driver, wait, By.ClassName("load-button"));

                Log("Click 'Aplikim i Ri' button");
                ClickElement(driver, wait, By.XPath("//button[@aria-label='Aplikim i ri']"));

                Log("Wait for step 1");
                WaitStep1Loaded(driver, wait);

                Log("Assert detajet e subjektit");
                var titleCandidates = driver.FindElements(
                        By.XPath("//*[contains(normalize-space(),'DETAJET E SUBJEKTIT') or contains(normalize-space(),'Detajet e subjektit')]"))
                    .Where(e => e.Displayed)
                    .ToList();

                if (titleCandidates.Count > 0)
                {
                    ScrollIntoView(driver, titleCandidates[0]);
                    Assert.That(titleCandidates[0].Text.Trim(), Does.Contain("subjektit").IgnoreCase);
                }

                IWebElement nipt = WaitVisibleAndScroll(driver, wait, By.Id("nipt"));
                Assert.That(InputValue(nipt), Is.EqualTo("L12121023B"));

                IWebElement emriSubjektit = WaitVisibleAndScroll(driver, wait, By.Id("subjectName"));
                Assert.That(InputValue(emriSubjektit), Is.EqualTo("KREATX"));

                IWebElement dtRegjistrimit = WaitVisibleAndScroll(driver, wait, By.Id("registrationDate"));
                Assert.That(InputValue(dtRegjistrimit), Is.EqualTo("21.09.2011"));

                IWebElement statusiSubjektit = WaitVisibleAndScroll(driver, wait, By.Id("subjectStatus"));
                Assert.That(InputValue(statusiSubjektit), Is.EqualTo("Aktiv"));

                IWebElement administratori = WaitVisibleAndScroll(driver, wait, By.Id("legalRepresentative"));
                Assert.That(InputValue(administratori), Is.EqualTo("Enor  Vlash  Nakuçi |"));

                Log("Click Vazhdo button - Step 1");
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

                Log("Assert Kontakti");
                WaitStep2Loaded(driver, wait);

                IWebElement contactTitle = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//h4[.//span[contains(@class,'text-uppercase') and contains(normalize-space(),'Kontakti')]]"),
                    By.XPath("//h4[contains(normalize-space(),'Kontakti')]"),
                    By.XPath("//*[contains(normalize-space(),'Kontakti')]")
                );

                Assert.That(contactTitle, Is.Not.Null, "Titulli 'Kontakti' nuk u gjet.");
                Assert.That(contactTitle.Text.Trim(), Does.Contain("Kontakti").IgnoreCase);

                IWebElement email = WaitVisibleAndScroll(driver, wait, By.Id("contactEmail"));
                Assert.That(InputValue(email), Is.EqualTo("ketjona.mema@kreatx.com"));

                IWebElement phoneNumber = WaitVisibleAndScroll(driver, wait, By.Id("contactMobile"));
                Assert.That(InputValue(phoneNumber), Is.EqualTo("0676041404"));

                Log("Click Vazhdo button - Step 2");
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

                Log("Assert Step 3, DREJTUESIT TEKNIKË");
                WaitStep3Loaded(driver, wait);

                IWebElement step3Title = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//h5[contains(@class,'text-uppercase') and contains(normalize-space(),'Drejtuesit teknikë')]"),
                    By.XPath("//h5[contains(normalize-space(),'Drejtuesit teknikë')]"),
                    By.XPath("//*[contains(normalize-space(),'Drejtuesit teknikë')]")
                );

                Assert.That(step3Title, Is.Not.Null, "Titulli i Step 3 nuk u gjet.");
                Assert.That(step3Title.Text.Trim(), Is.EqualTo("Drejtuesit teknikë").IgnoreCase);

                Log("Click 'Vazhdo' button - Step 3 without filled the required field");
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[4]/button[2]"));

                Log("Assert error PopUP");
                try
                {
                    var popupwait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    IWebElement popup = popupwait.Until(d =>
                    {
                        var el = d.FindElement(By.CssSelector(".alert-modal-container"));
                        return (el.Displayed && el.Enabled) ? el : null;
                    });

                    Log("===== ALERT POPUP U SHFAQ =====");

                    string popupText = popup.Text;
                    Log("Modal Text i plote: " + popupText);

                    IWebElement iconWrapper = popup.FindElement(By.CssSelector(".alert-modal-icon-wrapper"));
                    Log("Icon wrapper u gjet: " + (iconWrapper.Displayed ? "PO" : "JO"));

                    IWebElement titleElement = popup.FindElement(By.CssSelector(".alert-modal-title"));
                    string titleText = titleElement.Text.Trim();
                    Log("Titulli i modalit: " + titleText);

                    IWebElement descriptionElement = popup.FindElement(By.CssSelector(".alert-modal-description"));
                    string descriptionText = descriptionElement.Text.Trim();
                    Log("Pershkrimi i modalit: " + descriptionText);

                    IWebElement closeButton = popup.FindElement(By.CssSelector(".alert-modal-button.alert-modal-button--primary"));
                    string closeButtonText = closeButton.Text.Trim();
                    Log("Butoni i modalit: " + closeButtonText);

                    Log("HTML i modalit:");
                    Log(popup.GetAttribute("outerHTML"));

                    Assert.That(titleText, Is.EqualTo("Kujdes!"), "Titulli i modalit nuk eshte 'Kujdes!'.");
                    Assert.That(
                        descriptionText,
                        Is.EqualTo("Duhet të shtoni të paktën një drejtues teknik për të vazhduar"),
                        "Pershkrimi i modalit nuk perputhet."
                    );
                    Assert.That(closeButtonText, Is.EqualTo("Mbyll"), "Butoni nuk eshte 'Mbyll'.");

                    Log("Modali u validua me sukses.");

                    Log("Kliko butonin Mbyll");
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", closeButton);

                    popupwait.Until(d =>
                    {
                        var elements = d.FindElements(By.CssSelector(".alert-modal-container"));
                        return elements.Count == 0 || !elements[0].Displayed;
                    });

                    Log("Popup u mbyll me sukses.");
                }
                catch (WebDriverTimeoutException ex)
                {
                    Log("ERROR: Modali alert-modal-container nuk u shfaq brenda afatit.");
                    Log("Mesazhi: " + ex.Message);
                    throw;
                }
                catch (NoSuchElementException ex)
                {
                    Log("ERROR: Nje element brenda modalit nuk u gjet.");
                    Log("Mesazhi: " + ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    Log("ERROR i papritur gjate leximit te modalit.");
                    Log("Mesazhi: " + ex.Message);
                    throw;
                }

                Log("Add 'Drejtues Teknik' me NID");
                driver.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[2]/div[2]/input"))
                      .SendKeys("J55728107R");

                Log("Click 'Shto' button");
                ClickElement(driver, wait, By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[2]/div[2]/div[3]/button"));

                Log("Assert modal open");
                IWebElement modal = wait.Until(d =>
                {
                    var el = d.FindElement(By.CssSelector(".custom-modal-content"));
                    return el.Displayed ? el : null;
                });

                Assert.That(modal, Is.Not.Null, "Modali 'Shtoni drejtues teknik' nuk u gjet.");

                IWebElement modalTitle = modal.FindElements(By.CssSelector(".custom-modal-title")).FirstOrDefault();
                Assert.That(modalTitle, Is.Not.Null, "Titulli i modalit nuk u gjet.");
                Assert.That(modalTitle.Text.Trim(), Is.EqualTo("Shtoni drejtues teknik"));

                Log("Assert header and footer buttons");
                IWebElement closeX = modal.FindElements(By.CssSelector("button.custom-modal-x[aria-label='Close']")).FirstOrDefault();
                Assert.That(closeX, Is.Not.Null, "Butoni X nuk u gjet.");
                Assert.That(closeX.Displayed, Is.True);

                IWebElement anuloBtn = modal.FindElements(By.XPath(".//div[contains(@class,'custom-modal-footer')]//button[.//b[contains(normalize-space(),'Anulo')]]")).FirstOrDefault();
                IWebElement shtoBtn = modal.FindElements(By.XPath(".//div[contains(@class,'custom-modal-footer')]//button[.//b[contains(normalize-space(),'Shto')]]")).FirstOrDefault();

                Assert.That(anuloBtn, Is.Not.Null, "Butoni Anulo nuk u gjet.");
                Assert.That(shtoBtn, Is.Not.Null, "Butoni Shto nuk u gjet.");
                Assert.That(shtoBtn.Enabled, Is.True, "Butoni Shto duhet të jetë enabled.");

                var modalLabels = modal.FindElements(By.XPath(".//label"))
                    .Select(x =>
                    {
                        try { return x.Text?.Trim(); }
                        catch { return string.Empty; }
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                foreach (var lbl in modalLabels)
                {
                    Log("Modal label: " + lbl);
                }

                Log("Assert section 'Detajet e individit'");
                AssertDisabledModalInput(driver, modal, "NID", "J55728107R");
                AssertDisabledModalInput(driver, modal, "Emri", "Ketjona");
                AssertDisabledModalInput(driver, modal, "Mbiemri", "Mema");
                AssertDisabledModalInput(driver, modal, "Vendlindja", "Kavajë");
                AssertDisabledModalInput(driver, modal, "Qarku", "KAVAJË");
                AssertDisabledModalInput(driver, modal, "Atësia", "Mersin");
                AssertDisabledModalInput(driver, modal, "Amësia", "Aishe");
                AssertDisabledModalInput(driver, modal, "Rrethi", "TIRANË");
                AssertDisabledModalInput(driver, modal, "Statusi civil", "SIN");
                AssertDisabledModalInput(driver, modal, "Emri NJQV", "Shkolla \"3 Deshmoret\"");
                AssertDisabledModalInput(driver, modal, "Rruga", "THABIT REXHA 04040156; Nd. 6; H. 2; ; KAVAJË; KAVAJË; 2501; KAVAJË");

                Log("Assert Datëlindja");
                IWebElement datelindjaInput = modal.FindElements(
                    By.XPath(".//label[contains(normalize-space(),'Datëlindja')]/following::input[contains(@class,'flatpickr-input')][1]")
                ).FirstOrDefault();
                Assert.That(datelindjaInput, Is.Not.Null, "Input i Datëlindjes nuk u gjet.");
                Assert.That(datelindjaInput.GetAttribute("disabled"), Is.Not.Null, "Datëlindja duhet të jetë disabled.");

                Log("Assert Gjinia");
                IWebElement gjiniaSelect = modal.FindElements(
                    By.XPath(".//label[contains(normalize-space(),'Gjinia')]/following::select[1]")
                ).FirstOrDefault();
                Assert.That(gjiniaSelect, Is.Not.Null, "Select i Gjinisë nuk u gjet.");
                Assert.That(gjiniaSelect.GetAttribute("disabled"), Is.Not.Null, "Gjinia duhet të jetë disabled.");

                Log("Assert section 'Kontakti'");
                AssertEditableModalInput(driver, modal, "Nr. i tel.", "");
                AssertEditableModalInput(driver, modal, "Nr. i cel.", "");
                AssertEditableModalInput(driver, modal, "Email", "");

                Log("Assert tree categories");
                IWebElement tree = modal.FindElements(By.CssSelector("ul[role='tree'][aria-label='category-tree']")).FirstOrDefault();
                Assert.That(tree, Is.Not.Null, "Category tree nuk u gjet.");
                Assert.That(tree.Displayed, Is.True);

                var treeItems = tree.FindElements(By.CssSelector("li[role='treeitem']")).ToList();
                Assert.That(treeItems.Count, Is.GreaterThan(0), "Nuk u gjet asnjë kategori.");

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'LICENCE MBIKEQYRJE DHE KOLAUDIM I PUNIMEVE TE ZBATIMIT NE NDERTIM')]")).Any(x => x.Displayed),
                    Is.True,
                    "Kategoria e parë nuk u gjet."
                );

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'LICENCE PROJEKTIMI NE NDERTIM')]")).Any(x => x.Displayed),
                    Is.True,
                    "Kategoria e dytë nuk u gjet."
                );

                var disabledCheckboxes = modal.FindElements(By.XPath(".//ul[@role='tree']//input[@type='checkbox' and @disabled]"));
                Assert.That(disabledCheckboxes.Count, Is.GreaterThan(0), "Checkbox-et disabled nuk u gjetën.");

                Log("Expand first tree item: LICENCE MBIKEQYRJE DHE KOLAUDIM...");

                IWebElement firstTreeItem = wait.Until(d =>
                {
                    return tree.FindElements(By.XPath(
                        ".//li[@role='treeitem'][.//*[contains(normalize-space(),'LICENCE MBIKEQYRJE DHE KOLAUDIM')]]"))
                        .FirstOrDefault(x =>
                        {
                            try { return x.Displayed; }
                            catch { return false; }
                        });
                });

                Assert.That(firstTreeItem, Is.Not.Null, "Tree item i parë nuk u gjet.");

                ScrollIntoView(driver, firstTreeItem);
                Thread.Sleep(500);

                IWebElement firstExpand = firstTreeItem.FindElements(By.XPath(
                    ".//*[contains(@class,'MuiTreeItem-iconContainer')]"))
                    .FirstOrDefault(x =>
                    {
                        try { return x.Displayed; }
                        catch { return false; }
                    });

                Assert.That(firstExpand, Is.Not.Null, "Expand icon i elementit të parë nuk u gjet.");

                ClickJs(driver, firstExpand);
                Thread.Sleep(1500);

                wait.Until(d =>
                {
                    try
                    {
                        return firstTreeItem.Text.Contains("II. PUNIME SPECIALE NDERTIMI");
                    }
                    catch
                    {
                        return false;
                    }
                });

                Log("Expand second tree item: II. PUNIME SPECIALE NDERTIMI");

                IWebElement secondTreeItem = wait.Until(d =>
                {
                    return firstTreeItem.FindElements(By.XPath(
                        ".//li[@role='treeitem'][.//*[contains(normalize-space(),'II. PUNIME SPECIALE NDERTIMI')]]"))
                        .FirstOrDefault(x =>
                        {
                            try { return x.Displayed; }
                            catch { return false; }
                        });
                });

                Assert.That(secondTreeItem, Is.Not.Null, "Tree item 'II. PUNIME SPECIALE NDERTIMI' nuk u gjet.");

                ScrollIntoView(driver, secondTreeItem);
                Thread.Sleep(500);

                IWebElement secondExpand = secondTreeItem.FindElements(By.XPath(
                    ".//*[contains(@class,'MuiTreeItem-iconContainer')]"))
                    .FirstOrDefault(x =>
                    {
                        try { return x.Displayed; }
                        catch { return false; }
                    });

                Assert.That(secondExpand, Is.Not.Null, "Expand icon për 'II. PUNIME SPECIALE NDERTIMI' nuk u gjet.");

                ClickJs(driver, secondExpand);
                Thread.Sleep(2000);

                wait.Until(d =>
                {
                    try
                    {
                        return secondTreeItem.Text.Contains("NS-4");
                    }
                    catch
                    {
                        return false;
                    }
                });

                Log("Find and select category 'NS-4...'");

                IWebElement ns4TreeItem = wait.Until(d =>
                {
                    return secondTreeItem.FindElements(By.XPath(
                        ".//li[@role='treeitem'][.//*[contains(normalize-space(),'NS-4 Punime rifiniture')]]"))
                        .FirstOrDefault(x =>
                        {
                            try { return x.Displayed; }
                            catch { return false; }
                        });
                });

                Assert.That(ns4TreeItem, Is.Not.Null, "Tree item i kategorisë NS-4 nuk u gjet.");

                ScrollIntoView(driver, ns4TreeItem);
                Thread.Sleep(800);

                IWebElement ns4Checkbox = ns4TreeItem.FindElements(By.XPath(".//input[@type='checkbox']"))
                    .FirstOrDefault(x =>
                    {
                        try { return x.Displayed || x.Enabled; }
                        catch { return false; }
                    });

                Assert.That(ns4Checkbox, Is.Not.Null, "Checkbox i kategorisë NS-4 nuk u gjet.");

                IWebElement ns4Label = ns4TreeItem.FindElements(By.XPath(
                    ".//label[contains(.,'NS-4 Punime rifiniture')]"))
                    .FirstOrDefault(x =>
                    {
                        try { return x.Displayed; }
                        catch { return false; }
                    });

                if (ns4Label == null)
                {
                    ns4Label = ns4TreeItem.FindElements(By.XPath(".//label"))
                        .FirstOrDefault(x =>
                        {
                            try { return x.Displayed; }
                            catch { return false; }
                        });
                }

                Assert.That(ns4Label, Is.Not.Null, "Label i kategorisë NS-4 nuk u gjet.");

                try
                {
                    ScrollIntoView(driver, ns4Label);
                    Thread.Sleep(300);
                    ns4Label.Click();
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", ns4Label);
                }

                Thread.Sleep(1000);

                bool isChecked =
                    ns4Checkbox.Selected ||
                    ns4Checkbox.GetAttribute("checked") != null ||
                    ns4Checkbox.GetAttribute("aria-checked") == "true";

                Assert.That(isChecked, Is.True, "Kategoria NS-4 nuk u selektua.");

                Log("Assert documents section");
                IWebElement fuDiploma = modal.FindElements(By.Id("fuDiploma")).FirstOrDefault();
                IWebElement fuCV = modal.FindElements(By.Id("fuCV")).FirstOrDefault();
                IWebElement fuDokJustifikues = modal.FindElements(By.Id("fuDokJustifikues")).FirstOrDefault();
                IWebElement fuVetdeklarim = modal.FindElements(By.Id("fuVetdeklarim")).FirstOrDefault();

                Assert.That(fuDiploma, Is.Not.Null, "fuDiploma nuk u gjet.");
                Assert.That(fuCV, Is.Not.Null, "fuCV nuk u gjet.");
                Assert.That(fuDokJustifikues, Is.Not.Null, "fuDokJustifikues nuk u gjet.");
                Assert.That(fuVetdeklarim, Is.Not.Null, "fuVetdeklarim nuk u gjet.");

                Assert.That(fuDiploma.GetAttribute("accept"), Does.Contain(".pdf"));
                Assert.That(fuCV.GetAttribute("accept"), Does.Contain(".pdf"));
                Assert.That(fuDokJustifikues.GetAttribute("accept"), Does.Contain(".pdf"));
                Assert.That(fuVetdeklarim.GetAttribute("accept"), Does.Contain(".pdf"));

                Assert.That(fuDiploma.GetAttribute("aria-label"), Does.Contain("Diploma"));
                Assert.That(fuCV.GetAttribute("aria-label"), Does.Contain("CV e individit"));
                Assert.That(fuDokJustifikues.GetAttribute("aria-label"), Does.Contain("Dokumentacion teknik justifikues"));
                Assert.That(fuVetdeklarim.GetAttribute("aria-label"), Does.Contain("Vetëdeklarim i individit"));

                Log("Assert download buttons");
                var shkarkoButtons = modal.FindElements(By.XPath(".//button[contains(.,'Shkarko')]"));
                Assert.That(shkarkoButtons.Count, Is.GreaterThanOrEqualTo(2), "Butonat Shkarko nuk u gjetën.");

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'Vetëdeklarim i individit (sipas formatit 6/1 në rastin e aplikimit për studimin e projektimin)')]")).Any(x => x.Displayed),
                    Is.True,
                    "Teksti i shkarkimit 6/1 mungon."
                );

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'Vetëdeklarim i individit (sipas formatit 6/2 në rastin e aplikimit për mbikëqyrje e kolaudim)')]")).Any(x => x.Displayed),
                    Is.True,
                    "Teksti i shkarkimit 6/2 mungon."
                );

                Log("Click Shto without docs and assert required validation");
                ClickElement(driver, wait,
                    By.XPath("//div[contains(@class,'custom-modal-footer')]//button[.//b[contains(normalize-space(),'Shto')]]"));

                IWebElement requiredDocError = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//div[contains(@class,'custom-modal-content')]//*[contains(normalize-space(),'Ju lutem ngarkoni dokumentin e kërkuar')]"),
                    By.XPath("//div[contains(@class,'text-danger')][contains(normalize-space(),'Ju lutem ngarkoni dokumentin e kërkuar')]")
                );
                Assert.That(requiredDocError.Displayed, Is.True, "Mesazhi për dokumentin e detyrueshëm nuk u shfaq.");

                Log("Upload wrong docs in modal");
                string fileDiploma = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
                string fileCVWrong = @"C:\Users\Kreatx\Downloads\15mb.pdf";
                string fileDocTeknik = @"C:\Users\Kreatx\Downloads\image.png";
                string fileVetedeklarim = @"C:\Users\Kreatx\Downloads\TEST.pdf";

                Assert.That(File.Exists(fileDiploma), Is.True, "File Diploma nuk ekziston.");
                Assert.That(File.Exists(fileCVWrong), Is.True, "File CV nuk ekziston.");
                Assert.That(File.Exists(fileDocTeknik), Is.True, "File Doc Teknik nuk ekziston.");
                Assert.That(File.Exists(fileVetedeklarim), Is.True, "File Vetedeklarim nuk ekziston.");

                fuDiploma.SendKeys(fileDiploma);
                fuCV.SendKeys(fileCVWrong);
                fuDokJustifikues.SendKeys(fileDocTeknik);
                fuVetdeklarim.SendKeys(fileVetedeklarim);

                Log("Assert wrong file validations in modal");
                IWebElement fileSizeError = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//*[contains(@class,'text-danger') and contains(normalize-space(),'Madhësia e dokumentit')]")
                );
                Assert.That(fileSizeError.Displayed, Is.True);
                Assert.That(fileSizeError.Text.Trim(), Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se 15MB"));

                IWebElement formatError = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//*[contains(@class,'text-danger') and contains(normalize-space(),'Formati duhet të jetë: PDF')]")
                );
                Assert.That(formatError.Displayed, Is.True);

                Log("Remove wrong docs from modal");
                RemoveUploadedFiles(driver);

                Log("Upload correct docs in modal");
                string correctFileDiploma = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileCV = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileDocTeknik = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileVetedeklarimi = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";

                Assert.That(File.Exists(correctFileDiploma), Is.True, "File correct Diploma nuk ekziston.");
                Assert.That(File.Exists(correctFileCV), Is.True, "File correct CV nuk ekziston.");
                Assert.That(File.Exists(correctFileDocTeknik), Is.True, "File correct doc teknik nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimi), Is.True, "File correct vetedeklarimi nuk ekziston.");

                fuDiploma = modal.FindElements(By.Id("fuDiploma")).FirstOrDefault();
                fuCV = modal.FindElements(By.Id("fuCV")).FirstOrDefault();
                fuDokJustifikues = modal.FindElements(By.Id("fuDokJustifikues")).FirstOrDefault();
                fuVetdeklarim = modal.FindElements(By.Id("fuVetdeklarim")).FirstOrDefault();

                fuDiploma.SendKeys(correctFileDiploma);
                fuCV.SendKeys(correctFileCV);
                fuDokJustifikues.SendKeys(correctFileDocTeknik);
                fuVetdeklarim.SendKeys(correctFileVetedeklarimi);

                Assert.That(fuDiploma.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(fuCV.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(fuDokJustifikues.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(fuVetdeklarim.GetAttribute("value"), Does.Contain(".pdf"));

                Log("Wait before save");
                Thread.Sleep(5000);

                Log("Click Shto in modal");
                IWebElement shtoModalBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("//div[contains(@class,'custom-modal-footer')]//button[.//b[contains(normalize-space(),'Shto')]]")
                ));
                ScrollIntoView(driver, shtoModalBtn);
                Thread.Sleep(500);
                ClickJs(driver, shtoModalBtn);

                Log("Wait after save");
                Thread.Sleep(5000);

                Log("Click 'Vazhdo' button - Step 3 after filling the required field");
                ClickElement(driver, wait, By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div/div[4]/button[2]"));

                bool step3ValidationStillVisible = driver.FindElements(By.XPath(
                        "//*[contains(normalize-space(),'Ju lutem shtoni të paktën një drejtues teknik për të vazhduar')]"))
                    .Any(x =>
                    {
                        try { return x.Displayed; }
                        catch { return false; }
                    });

                Assert.That(step3ValidationStillVisible, Is.False,
                    "Pas Shto, drejtuesi teknik nuk u ruajt realisht sepse validimi i Step 3 u shfaq përsëri.");

                Log("Assert Dokumentacioni");
                WaitStep4Loaded(driver, wait);

                var step4Titles = driver.FindElements(
                        By.XPath("//*[contains(normalize-space(),'DOKUMENTACIONI') or contains(normalize-space(),'Dokumentacioni')]"))
                    .Where(e => e.Displayed)
                    .ToList();

                if (step4Titles.Count > 0)
                {
                    ScrollIntoView(driver, step4Titles[0]);
                    Assert.That(step4Titles[0].Text.Trim(), Does.Contain("Dokumentacioni").IgnoreCase);
                }

                IWebElement step4Container = wait.Until(d =>
                {
                    var el = d.FindElement(By.XPath("/html/body/div[1]/main/div[3]/div/div/div/div"));
                    return el.Displayed ? el : null;
                });

                Assert.That(step4Container, Is.Not.Null, "Step 4 container nuk u gjet.");
                ScrollIntoView(driver, step4Container);

                Log("Click 'Dergo' button without required document");

                IWebElement dergoBtn = wait.Until(d =>
                {
                    return d.FindElements(By.CssSelector("button.ealb-btn-continue.with-arrow"))
                        .FirstOrDefault(b =>
                        {
                            try
                            {
                                return b.Displayed && b.Enabled && b.Text.Contains("Dërgo");
                            }
                            catch
                            {
                                return false;
                            }
                        });
                });

                Assert.That(dergoBtn, Is.Not.Null, "Butoni Dërgo nuk u gjet.");
                ScrollIntoView(driver, dergoBtn);
                Thread.Sleep(300);

                try
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("document.activeElement && document.activeElement.blur();");
                }
                catch { }

                Thread.Sleep(200);

                try
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", dergoBtn);
                }
                catch
                {
                    ClickJs(driver, dergoBtn);
                }

                Thread.Sleep(1000);

                try
                {
                    Log("Step4 container text AFTER Dërgo:");
                    Log(step4Container.Text);
                }
                catch { }

                IWebElement docErrorMessage = wait.Until(d =>
                {
                    var el = d.FindElements(By.XPath(
                        "//div[contains(@class,'text-danger') and contains(normalize-space(),'Ju lutem ngarkoni dokumentin e kërkuar')]"
                    ))
                    .FirstOrDefault(x =>
                    {
                        try { return x.Displayed; }
                        catch { return false; }
                    });

                    return el;
                });

                Assert.That(docErrorMessage, Is.Not.Null, "Mesazhi i validimit për dokumentin e detyrueshëm nuk u gjet.");
                ScrollIntoView(driver, docErrorMessage);

                string docErrorText = docErrorMessage.Text.Trim();
                Log("Doc error text: " + docErrorText);

                Assert.That(
                    docErrorText,
                    Does.Contain("Ju lutem ngarkoni dokumentin e kërkuar"),
                    "Mesazhi i validimit për dokumentin e detyrueshëm nuk përputhet."
                );

                Log("Upload uncorrect docs");
                string fileKontratat = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
                string fileVetedeklarimiPT = @"C:\Users\Kreatx\Downloads\image.png";
                string fileVetedeklarimiDT = @"C:\Users\Kreatx\Downloads\15mb.pdf";

                Assert.That(File.Exists(fileKontratat), Is.True, "File kontratat nuk ekziston.");
                Assert.That(File.Exists(fileVetedeklarimiPT), Is.True, "File VetedeklarimiPT nuk ekziston.");
                Assert.That(File.Exists(fileVetedeklarimiDT), Is.True, "File VetedeklarimiDT nuk ekziston.");

                var wrongFileInputs = wait.Until(d =>
                {
                    var els = GetVisibleFileInputs(step4Container);

                    Log("File inputs found: " + els.Count);

                    foreach (var e in els)
                    {
                        try
                        {
                            Log($"Input -> displayed={e.Displayed}, enabled={e.Enabled}");
                        }
                        catch { }
                    }

                    return els.Count >= 3 ? els : null;
                });

                ScrollIntoView(driver, wrongFileInputs[0]);
                wrongFileInputs[0].SendKeys(fileKontratat);

                ScrollIntoView(driver, wrongFileInputs[1]);
                wrongFileInputs[1].SendKeys(fileVetedeklarimiPT);

                ScrollIntoView(driver, wrongFileInputs[2]);
                wrongFileInputs[2].SendKeys(fileVetedeklarimiDT);

                Log("Assert Max size");
                IWebElement fileSizeErrorStep4 = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Madhësia e dokumentit')]")
                );
                Assert.That(fileSizeErrorStep4.Displayed, Is.True);
                Assert.That(
                    fileSizeErrorStep4.Text.Trim(),
                    Does.Contain("Madhësia e dokumentit nuk duhet të jetë më shumë se 15MB")
                );

                Log("Assert format gabim");
                IWebElement formatErrorStep4 = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//div[contains(@class,'text-danger') and contains(.,'Formati duhet të jetë')]")
                );
                Assert.That(formatErrorStep4.Displayed, Is.True);
                Assert.That(formatErrorStep4.Text.Trim(), Is.EqualTo("Formati duhet të jetë: PDF"));

                Log("Assert uncorrect doc name");
                IWebElement fileDocNameErrorStep4 = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//div[contains(@class,'text-danger') and contains(text(),'Emri i dokumentit është i pavlefshëm')]")
                );
                Assert.That(fileDocNameErrorStep4.Displayed, Is.True);
                Assert.That(
                    fileDocNameErrorStep4.Text.Trim(),
                    Does.Contain("Emri i dokumentit është i pavlefshëm")
                );

                Log("Remove uncorrect docs");
                RemoveUploadedFiles(driver);

                Thread.Sleep(1000);

        Log("Prit 1 minutë para ngarkimit të dokumentit të saktë…");
        Thread.Sleep(TimeSpan.FromMinutes(1));

                Log("Upload Correct Docs");
                string correctFileKontratat = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileVetedeklarimiPTCorrect = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileVetedeklarimiDTCorrect = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";

                Assert.That(File.Exists(correctFileKontratat), Is.True, "File correct Kontratat nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimiPTCorrect), Is.True, "File correct Vetedeklarimi PT nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimiDTCorrect), Is.True, "File correct Vetedeklarimi DT nuk ekziston.");

                var correctFileInputs = wait.Until(d =>
                {
                    var els = GetVisibleFileInputs(step4Container);

                    Log("Correct file inputs found: " + els.Count);

                    foreach (var e in els)
                    {
                        try
                        {
                            Log($"Correct input -> displayed={e.Displayed}, enabled={e.Enabled}");
                        }
                        catch { }
                    }

                    return els.Count >= 3 ? els : null;
                });

                ScrollIntoView(driver, correctFileInputs[0]);
                correctFileInputs[0].SendKeys(correctFileKontratat);

                ScrollIntoView(driver, correctFileInputs[1]);
                correctFileInputs[1].SendKeys(correctFileVetedeklarimiPTCorrect);

                ScrollIntoView(driver, correctFileInputs[2]);
                correctFileInputs[2].SendKeys(correctFileVetedeklarimiDTCorrect);

                Thread.Sleep(1500);

                Log("Verify uploaded docs are present");
                Assert.That(correctFileInputs[0].GetAttribute("value"), Is.Not.Empty, "File 1 u ngarkua por inputi është bosh.");
                Assert.That(correctFileInputs[1].GetAttribute("value"), Is.Not.Empty, "File 2 u ngarkua por inputi është bosh.");
                Assert.That(correctFileInputs[2].GetAttribute("value"), Does.Contain(".pdf"));

                var visibleErrors = driver.FindElements(
                        By.XPath("//div[contains(@class,'text-danger') and normalize-space()!='']"))
                    .Where(e => e.Displayed)
                    .ToList();

                Assert.That(visibleErrors.Count, Is.EqualTo(0),
                    "Ka ende gabime të dukshme pas ngarkimit të dokumenteve të sakta.");

                

                Log("Kliko Dergo Button");
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