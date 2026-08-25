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
/// Stimulon rastin e FAIL të 11136: të njëjtat të dhëna, por pa ngarkuar dokumente,
/// që pas Dërgo të mos shfaqet as sukses as "Kujdes". Testi dështon me mesazhin e UI.
/// </summary>
[TestFixture]
public class _11136_NID_Web_FailCase
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
    public void Aplikim_i_Ri_NID_11136_FailCase_ReturnsUiMessage()
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