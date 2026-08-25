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

/// <summary>
/// Stimulon rastin e FAIL të 11135: të njëjtat të dhëna, por pa ngarkuar dokumente,
/// që pas Dërgo të mos shfaqet as sukses as "Kujdes". Testi dështon me mesazhin e UI.
/// </summary>
[TestFixture]
public class _11135_NIPT_WEB_FailCase
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

            var clickable = el.closest('button, a, [role=""button""], .MuiButtonBase-root, .MuiTreeItem-iconContainer, .btn');
            if (!clickable) clickable = el;

            clickable.scrollIntoView({block:'center', inline:'nearest'});

            try { clickable.removeAttribute('disabled'); } catch(e) {}

            try {
                if (typeof clickable.click === 'function') {
                    clickable.click();
                    return;
                }
            } catch (e) {}

            clickable.dispatchEvent(new MouseEvent('mouseover', { bubbles: true }));
            clickable.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
            clickable.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
            clickable.dispatchEvent(new MouseEvent('click', { bubbles: true }));
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
            .Where(e => e.Displayed || e.Enabled)
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
            bool hasMkInput = d.FindElements(By.XPath("//input")).Any(e => e.Displayed && e.Enabled);
            bool hasTitle =
                d.FindElements(By.XPath("//*[contains(normalize-space(),'DREJTUESIT TEKNIK')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'Drejtuesit Teknik')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'DREJTUESIT TEKNIKË')]")).Any(e => e.Displayed);

            return hasTitle || hasMkInput;
        });
    }

    private void WaitStep4Loaded(IWebDriver driver, WebDriverWait wait)
    {
        wait.Until(d =>
        {
            bool hasFileInput =
                d.FindElements(By.XPath("//input[@type='file']")).Any(e => e.Displayed || e.Enabled);

            bool hasDokTitle =
                d.FindElements(By.XPath("//*[contains(normalize-space(),'DOKUMENTACIONI')]")).Any(e => e.Displayed) ||
                d.FindElements(By.XPath("//*[contains(normalize-space(),'Dokumentacioni')]")).Any(e => e.Displayed);

            bool hasDergoBtn =
                d.FindElements(By.XPath("//button[.//b[contains(normalize-space(),'Dergo')] or contains(normalize-space(),'Dergo')]"))
                 .Any(e => e.Displayed);

            return hasFileInput || hasDokTitle || hasDergoBtn;
        });
    }

    private IWebElement GetModal(IWebDriver driver, WebDriverWait wait)
    {
        return wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div.custom-modal-content")));
    }

    private IWebElement GetModalInputByLabel(IWebDriver driver, IWebElement modal, string labelText)
    {
        return modal.FindElements(By.XPath(
                $".//label[contains(normalize-space(),'{labelText}')]/following-sibling::input[1]"))
            .FirstOrDefault();
    }

    private void AssertDisabledModalInput(IWebDriver driver, IWebElement modal, string labelText, string expectedValue)
    {
        IWebElement input = GetModalInputByLabel(driver, modal, labelText);
        Assert.That(input, Is.Not.Null, $"Input '{labelText}' nuk u gjet.");
        ScrollIntoView(driver, input);
        Assert.That(InputValue(input), Is.EqualTo(expectedValue), $"Vlera e '{labelText}' nuk është e saktë.");
        Assert.That(input.Enabled, Is.False, $"Input '{labelText}' duhet të jetë disabled.");
    }

    private void AssertEditableModalInput(IWebDriver driver, IWebElement modal, string labelText, string expectedValue)
    {
        IWebElement input = GetModalInputByLabel(driver, modal, labelText);
        Assert.That(input, Is.Not.Null, $"Input '{labelText}' nuk u gjet.");
        ScrollIntoView(driver, input);
        Assert.That(InputValue(input), Is.EqualTo(expectedValue), $"Vlera e '{labelText}' nuk është e saktë.");
        Assert.That(input.Enabled, Is.True, $"Input '{labelText}' duhet të jetë editable.");
    }

    private IWebElement GetModalFooterButton(IWebElement modal, string text)
    {
        return modal.FindElements(By.XPath(
                $".//div[contains(@class,'custom-modal-footer')]//button[.//b[contains(normalize-space(),'{text}')]] | " +
                $".//div[contains(@class,'custom-modal-footer')]//button[contains(normalize-space(),'{text}')]"))
            .FirstOrDefault();
    }

    private void SelectCategoryInModal(IWebDriver driver, IWebElement modal, WebDriverWait wait)
    {
        var expanders = modal.FindElements(By.CssSelector(
            "ul[role='tree'] .MuiTreeItem-iconContainer, ul[role='tree'] button, ul[role='tree'] [role='button']"))
            .Where(x =>
            {
                try { return x.Displayed; }
                catch { return false; }
            })
            .ToList();

        foreach (var expander in expanders)
        {
            try
            {
                ClickJs(driver, expander);
                Thread.Sleep(300);
            }
            catch
            {
            }
        }

        Thread.Sleep(1000);

        var activeCheckbox = modal.FindElements(By.XPath(".//ul[@role='tree']//input[@type='checkbox' and not(@disabled)]"))
            .FirstOrDefault(x =>
            {
                try { return x.Displayed && x.Enabled; }
                catch { return false; }
            });

        if (activeCheckbox != null)
        {
            ScrollIntoView(driver, activeCheckbox);

            try
            {
                activeCheckbox.Click();
            }
            catch
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", activeCheckbox);
            }

            Thread.Sleep(500);

            Assert.That(
                activeCheckbox.Selected || activeCheckbox.GetAttribute("checked") != null,
                Is.True,
                "Checkbox i kategorisë nuk u selektua."
            );
            return;
        }

        var selectableNodes = modal.FindElements(By.XPath(
            ".//ul[@role='tree']//li[@role='treeitem']//*[contains(@class,'MuiTreeItem-label') or contains(@class,'MuiTreeItem-content')]"))
            .Where(x =>
            {
                try
                {
                    return x.Displayed && !string.IsNullOrWhiteSpace(x.Text);
                }
                catch
                {
                    return false;
                }
            })
            .ToList();

        foreach (var node in selectableNodes)
        {
            try
            {
                ScrollIntoView(driver, node);
                ClickJs(driver, node);
                Thread.Sleep(500);
                return;
            }
            catch
            {
            }
        }

        Assert.Fail("Nuk u gjet asnjë kategori e përzgjedhshme në modal.");
    }

    private void RemoveUploadedFiles(IWebDriver driver)
    {
        while (true)
        {
            var cancelButtons = driver.FindElements(By.CssSelector("button[aria-label='Cancel upload']"))
                                      .Where(x => x.Displayed)
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
    public void _11135_NIPT_Aplikim_i_Ri_FailCase_ReturnsUiMessage()
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
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div/div[1]/div/a"));

                Log("Fill in the form fields");
                driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
                driver.FindElement(By.Id("ServiceCode")).SendKeys("11135");
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mie_merge");
                driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
                driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
                driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

                new SelectElement(driver.FindElement(By.Id("ProfileType")))
                    .SelectByValue("Organisation");

                new SelectElement(driver.FindElement(By.Id("Platform")))
                    .SelectByValue("WEB");

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

                IWebElement EmriSubjektit = WaitVisibleAndScroll(driver, wait, By.Id("emri"));
                Assert.That(InputValue(EmriSubjektit), Is.EqualTo("KREATX"));

                IWebElement DtRegjistrimit = WaitVisibleAndScroll(driver, wait, By.Id("registrationDate"));
                Assert.That(InputValue(DtRegjistrimit), Is.EqualTo("21.09.2011"));

                IWebElement StatusiSubjektit = WaitVisibleAndScroll(driver, wait, By.Id("status"));
                Assert.That(InputValue(StatusiSubjektit), Is.EqualTo("Aktiv"));

                IWebElement Administratori = WaitVisibleAndScroll(driver, wait, By.Id("administrator"));
                Assert.That(InputValue(Administratori), Is.EqualTo("Enor  Vlash  Nakuçi |"));

                Log("Click Vazhdo button - Step 1");
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

                Log("Assert Kontakti");
                WaitStep2Loaded(driver, wait);

                IWebElement email = WaitVisibleAndScroll(driver, wait, By.Name("email"));
                Assert.That(InputValue(email), Is.EqualTo("ketjona.mema@kreatx.com"));

                IWebElement phoneNumber = WaitVisibleAndScroll(driver, wait, By.Name("nrCel"));
                Assert.That(InputValue(phoneNumber), Is.EqualTo("0676041404"));

                Log("Click Vazhdo button - Step 2");
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/button[2]"));

                Log("Assert Step 3");
                WaitStep3Loaded(driver, wait);

                Log("Click 'Vazhdo' button - Step 3 without filled the required field");
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/button[2]"));

                Log("Assert error message for required field in step 3");
                IWebElement errorMessageStep3 = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//*[contains(normalize-space(),'Ju lutem shtoni të paktën një drejtues teknik për të vazhduar')]"),
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/div[2]/div")
                );
                ScrollIntoView(driver, errorMessageStep3);
                Assert.That(
                    errorMessageStep3.Text,
                    Does.Contain("Ju lutem shtoni të paktën një drejtues teknik për të vazhduar")
                );

                Log("Fill the required field in step 3");
                IWebElement dtInput = WaitVisibleAndScroll(
                    driver,
                    wait,
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/div[1]/div[2]/div/input")
                );
                dtInput.SendKeys("MK.2862/1");

                Log("Click 'Shto' button");
                ClickElement(
                    driver,
                    wait,
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[2]/div[3]/div[1]/div[2]/div/button")
                );

                Log("Assert modal open");
                IWebElement modal = GetModal(driver, wait);

                IWebElement modalTitle = modal.FindElements(By.XPath(".//h2[contains(normalize-space(),'Shto Drejtues Teknik për Shtesa')]")).FirstOrDefault();
                Assert.That(modalTitle, Is.Not.Null, "Titulli i modalit nuk u gjet.");
                Assert.That(modalTitle.Text.Trim(), Is.EqualTo("Shto Drejtues Teknik për Shtesa"));

                Log("Assert header and footer buttons");
                IWebElement closeX = modal.FindElements(By.CssSelector("button.custom-modal-x[aria-label='Close']")).FirstOrDefault();
                Assert.That(closeX, Is.Not.Null, "Butoni X nuk u gjet.");
                Assert.That(closeX.Displayed, Is.True);

                IWebElement anuloBtn = GetModalFooterButton(modal, "Anulo");
                IWebElement ruajBtn = GetModalFooterButton(modal, "Ruaj");

                Assert.That(anuloBtn, Is.Not.Null, "Butoni Anulo nuk u gjet.");
                Assert.That(ruajBtn, Is.Not.Null, "Butoni Ruaj nuk u gjet.");
                Assert.That(ruajBtn.Enabled, Is.True, "Butoni Ruaj duhet të jetë enabled.");

                Log("Assert section 'Të dhënat personale'");
                AssertDisabledModalInput(driver, modal, "NID", "H70422177B");
                AssertDisabledModalInput(driver, modal, "Emri", "Gëzim");
                AssertDisabledModalInput(driver, modal, "Mbiemri", "Kuka");
                AssertDisabledModalInput(driver, modal, "Atësia", "Brahim");
                AssertDisabledModalInput(driver, modal, "Mëmësia", "Lavdie");
                AssertDisabledModalInput(driver, modal, "Datëlindja", "22.04.1977");
                AssertDisabledModalInput(driver, modal, "Gjinia", "M");
                AssertDisabledModalInput(driver, modal, "Vendi i lindjes", "Fier,Fier");
                AssertDisabledModalInput(driver, modal, "Gjendja civile", "MAR");
                AssertDisabledModalInput(driver, modal, "Bashkia", "Bashkia Tiranë - Njësia Bashkiake Nr. 7");
                AssertDisabledModalInput(driver, modal, "Qarku", "Tirane");
                AssertDisabledModalInput(driver, modal, "Rajoni", "TIRANE");
                AssertDisabledModalInput(driver, modal, "Adresa", "1023 Njesia bashkiake nr. 7 Frosina Plaku Homeplan 2 2");

                Log("Assert section 'Kontakti'");
                AssertEditableModalInput(driver, modal, "Nr. Tel", "");
                AssertEditableModalInput(driver, modal, "Nr. Cel", "0696752043");
                AssertEditableModalInput(driver, modal, "Email", "gezim.kuka@yahoo.com");

                Log("Assert tree categories");
                IWebElement tree = modal.FindElements(By.CssSelector("ul[role='tree'][aria-label='category-tree']")).FirstOrDefault();
                Assert.That(tree, Is.Not.Null, "Category tree nuk u gjet.");
                Assert.That(tree.Displayed, Is.True);

                var treeItems = tree.FindElements(By.CssSelector("li[role='treeitem']")).ToList();
                Assert.That(treeItems.Count, Is.GreaterThan(0), "Nuk u gjet asnjë kategori.");

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'LICENCE MBIKEQYRJE DHE KOLAUDIM I PUNIMEVE TE ZBATIMIT NE NDERTIM')]"))
                         .Any(x => x.Displayed),
                    Is.True,
                    "Kategoria e parë nuk u gjet."
                );

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'LICENCE PROJEKTIMI NE NDERTIM')]"))
                         .Any(x => x.Displayed),
                    Is.True,
                    "Kategoria e dytë nuk u gjet."
                );

                var disabledCheckboxes = modal.FindElements(By.XPath(".//ul[@role='tree']//input[@type='checkbox' and @disabled]"));
                Assert.That(disabledCheckboxes.Count, Is.GreaterThan(0), "Checkbox-et disabled nuk u gjetën.");

                Log("Expand tree items");
                IWebElement expandLevel1 = tree.FindElements(By.CssSelector(
                    "li[role='treeitem'] .MuiTreeItem-iconContainer, li[role='treeitem'] button, li[role='treeitem'] [role='button'], li[role='treeitem'] svg"))
                    .FirstOrDefault();

                Assert.That(expandLevel1, Is.Not.Null, "Expand icon i level 1 nuk u gjet.");
                ClickJs(driver, expandLevel1);
                Thread.Sleep(800);

                var nestedExpand = tree.FindElements(By.XPath(
                    ".//li[@role='treeitem']//li[@role='treeitem']//*[contains(@class,'MuiTreeItem-iconContainer') or self::button or @role='button' or self::svg]"))
                    .FirstOrDefault();

                if (nestedExpand != null)
                {
                    ClickJs(driver, nestedExpand);
                    Thread.Sleep(800);
                }

                Log("Select category in modal");
                SelectCategoryInModal(driver, modal, wait);

                Log("Assert documents section");
                IWebElement fuCV2 = modal.FindElements(By.Id("fuCV2")).FirstOrDefault();
                IWebElement fuLicenca2 = modal.FindElements(By.Id("fuLicenca2")).FirstOrDefault();
                IWebElement fuDokJustifikues2 = modal.FindElements(By.Id("fuDokJustifikues2")).FirstOrDefault();
                IWebElement fuVetdeklarim2 = modal.FindElements(By.Id("fuVetdeklarim2")).FirstOrDefault();

                Assert.That(fuCV2, Is.Not.Null, "fuCV2 nuk u gjet.");
                Assert.That(fuLicenca2, Is.Not.Null, "fuLicenca2 nuk u gjet.");
                Assert.That(fuDokJustifikues2, Is.Not.Null, "fuDokJustifikues2 nuk u gjet.");
                Assert.That(fuVetdeklarim2, Is.Not.Null, "fuVetdeklarim2 nuk u gjet.");

                Assert.That(fuCV2.GetAttribute("accept"), Does.Contain(".pdf"));
                Assert.That(fuLicenca2.GetAttribute("accept"), Does.Contain(".pdf"));
                Assert.That(fuDokJustifikues2.GetAttribute("accept"), Does.Contain(".pdf"));
                Assert.That(fuVetdeklarim2.GetAttribute("accept"), Does.Contain(".pdf"));

                Assert.That(fuCV2.GetAttribute("aria-label"), Does.Contain("CV e drejtuesit teknik"));
                Assert.That(fuLicenca2.GetAttribute("aria-label"), Does.Contain("Licenca e drejtuesit teknik"));
                Assert.That(fuDokJustifikues2.GetAttribute("aria-label"), Does.Contain("Dokumentacion teknik justifikues"));
                Assert.That(fuVetdeklarim2.GetAttribute("aria-label"), Does.Contain("Vetëdeklarim i drejtuesit teknik"));

                Log("Assert download buttons");
                var shkarkoButtons = modal.FindElements(By.XPath(".//button[contains(.,'Shkarko')]"));
                Assert.That(shkarkoButtons.Count, Is.GreaterThanOrEqualTo(2), "Butonat Shkarko nuk u gjetën.");

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'Vetëdeklarim i përfaqësuesit ligjor')]"))
                         .Any(x => x.Displayed),
                    Is.True,
                    "Teksti për vetëdeklarimin e përfaqësuesit ligjor mungon."
                );

                Assert.That(
                    modal.FindElements(By.XPath(".//*[contains(normalize-space(),'Vetëdeklarim i drejtuesit teknik')]"))
                         .Any(x => x.Displayed),
                    Is.True,
                    "Teksti për vetëdeklarimin e drejtuesit teknik mungon."
                );

                Log("Click Ruaj without docs and assert required validation");
                ClickElement(driver, wait, By.XPath("//div[contains(@class,'custom-modal-footer')]//button[.//b[contains(normalize-space(),'Ruaj')]] | //div[contains(@class,'custom-modal-footer')]//button[contains(normalize-space(),'Ruaj')]"));

                IWebElement requiredDocError = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//div[contains(@class,'custom-modal-content')]//*[contains(normalize-space(),'Ju lutem ngarkoni dokumentin e kërkuar')]"),
                    By.XPath("//div[contains(@class,'text-danger')][contains(normalize-space(),'Ju lutem ngarkoni dokumentin e kërkuar')]")
                );
                Assert.That(requiredDocError.Displayed, Is.True, "Mesazhi për dokumentin e detyrueshëm nuk u shfaq.");

                Log("Upload wrong docs in modal");
                string fileCV = @"C:\Users\Kreatx\Downloads\Kthim Alfis test(1).pdf";
                string fileLicenca = @"C:\Users\Kreatx\Downloads\15mb.pdf";
                string fileDocTeknik = @"C:\Users\Kreatx\Downloads\image.png";
                string fileVetedeklarim = @"C:\Users\Kreatx\Downloads\TEST.pdf";

                Assert.That(File.Exists(fileCV), Is.True, "File CV nuk ekziston.");
                Assert.That(File.Exists(fileLicenca), Is.True, "File Licenca nuk ekziston.");
                Assert.That(File.Exists(fileDocTeknik), Is.True, "File Doc Teknik nuk ekziston.");
                Assert.That(File.Exists(fileVetedeklarim), Is.True, "File Vetedeklarim nuk ekziston.");

                fuCV2.SendKeys(fileCV);
                fuLicenca2.SendKeys(fileLicenca);
                fuDokJustifikues2.SendKeys(fileDocTeknik);
                fuVetdeklarim2.SendKeys(fileVetedeklarim);

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
                string correctFileCV = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileLicenca = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileDocTeknik = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";
                string correctFileVetedeklarimi = @"C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf";

                Assert.That(File.Exists(correctFileCV), Is.True, "File correct CV nuk ekziston.");
                Assert.That(File.Exists(correctFileLicenca), Is.True, "File correct Licenca nuk ekziston.");
                Assert.That(File.Exists(correctFileDocTeknik), Is.True, "File correct doc teknik nuk ekziston.");
                Assert.That(File.Exists(correctFileVetedeklarimi), Is.True, "File correct vetedeklarimi nuk ekziston.");

                fuCV2 = modal.FindElements(By.Id("fuCV2")).FirstOrDefault();
                fuLicenca2 = modal.FindElements(By.Id("fuLicenca2")).FirstOrDefault();
                fuDokJustifikues2 = modal.FindElements(By.Id("fuDokJustifikues2")).FirstOrDefault();
                fuVetdeklarim2 = modal.FindElements(By.Id("fuVetdeklarim2")).FirstOrDefault();

                fuCV2.SendKeys(correctFileCV);
                fuLicenca2.SendKeys(correctFileLicenca);
                fuDokJustifikues2.SendKeys(correctFileDocTeknik);
                fuVetdeklarim2.SendKeys(correctFileVetedeklarimi);

                Assert.That(fuCV2.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(fuLicenca2.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(fuDokJustifikues2.GetAttribute("value"), Does.Contain(".pdf"));
                Assert.That(fuVetdeklarim2.GetAttribute("value"), Does.Contain(".pdf"));

                Log("Wait before save");
                Thread.Sleep(5000);

                Log("Click Ruaj in modal");
                IWebElement ruajModalBtn = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[4]/div/div[3]/button[2]")
                ));
                ScrollIntoView(driver, ruajModalBtn);
                Thread.Sleep(500);
                ruajModalBtn.Click();

                Log("Wait after save");
                Thread.Sleep(5000);

                Log("Click 'Vazhdo' button - Step 3 after filling the required field");
                ClickElement(driver, wait, By.XPath("/html/body/div/main/div[3]/div/div/div/div/div[3]/button[2]"));

                bool step3ValidationStillVisible = driver.FindElements(By.XPath(
                        "//*[contains(normalize-space(),'Ju lutem shtoni të paktën një drejtues teknik për të vazhduar')]"))
                    .Any(x =>
                    {
                        try { return x.Displayed; }
                        catch { return false; }
                    });

                Assert.That(step3ValidationStillVisible, Is.False,
                    "Pas Ruaj, drejtuesi teknik nuk u ruajt realisht sepse validimi i Step 3 u shfaq përsëri.");

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

                IWebElement step4Container = WaitAnyVisible(
                    driver,
                    wait,
                    By.XPath("//input[@type='file']/ancestor::div[contains(@class,'row')][1]"),
                    By.XPath("//input[@type='file']/ancestor::div[contains(@class,'MuiBox-root')][1]"),
                    By.XPath("//input[@type='file']/ancestor::div[3]"),
                    By.XPath("/html/body/div/main/div[3]/div/div/div/div")
                );

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