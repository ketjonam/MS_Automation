using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;
using System.Threading;

internal static class DpshtrrFailCaseSupport
{
    internal static readonly string[] InformationalResponseKeys =
    [
        "FailedPayment",
        "FailedToRetrieveBasket",
        "BasketHasNoOrders",
        "FailedParsingDataFromDownStream",
        "EmptyOrInvalidResponseFromDownstreamService",
        "PaymentNotConfiguredForService",
        "PersonNotFound",
        "NoVehiclesFound",
        "NoFinesFound",
        "NoSeriaProvided",
        "CarInfoNotFound",
        "CarInfoServerError",
        "SubjectNotFound",
        "FailedToExtractTokenClaims",
        "TimeoutError",
        "ServiceConnectionError",
        "DPSHTRRUnavailable",
        "InvalidYear",
        "NoServiceFound",
        "NoVehicalesFound",
        "VehicleExistsInSystem",
    ];

    private static readonly (string Key, string[] Phrases)[] InformationalPhrases =
    [
        ("NoVehiclesFound",
        [
            "nuk u gjet asnje mjet",
            "nuk u gjet asnjë mjet",
            "nuk u gjet asnjë automjet",
            "nuk u gjet asnje automjet",
            "nuk dispononi mjete",
            "nuk dispononi automjete",
            "no vehicles found",
        ]),
        ("PersonNotFound",
        [
            "nuk u gjet person",
            "personi nuk u gjet",
            "person not found",
        ]),
        ("SubjectNotFound",
        [
            "nuk u gjet subjekt",
            "subjekti nuk u gjet",
            "subject not found",
        ]),
        ("CarInfoNotFound",
        [
            "car info not found",
            "nuk u gjet informacion për mjetin",
        ]),
        ("CarInfoServerError",
        [
            "car info server error",
        ]),
        ("NoFinesFound",
        [
            "nuk u gjet gjob",
            "nuk u gjetën gjob",
            "nuk u gjeten gjob",
            "no fines found",
        ]),
        ("NoSeriaProvided",
        [
            "nuk u dha seria",
            "no seria provided",
        ]),
        ("DPSHTRRUnavailable",
        [
            "dpshtrr i padisponueshëm",
            "dpshtrr unavailable",
            "shërbimi dpshtrr nuk është i disponueshëm",
        ]),
        ("TimeoutError",
        [
            "timeout",
            "koha e pritjes",
        ]),
        ("ServiceConnectionError",
        [
            "service connection error",
            "gabim në lidhjen me shërbimin",
        ]),
        ("InvalidYear",
        [
            "invalid year",
            "vit i pavlefshëm",
        ]),
        ("NoServiceFound",
        [
            "no service found",
            "nuk u gjet shërbim",
        ]),
        ("VehicleExistsInSystem",
        [
            "vehicle exists in system",
            "mjeti ekziston në sistem",
        ]),
        ("FailedPayment",
        [
            "failed payment",
            "pagesa dështoi",
        ]),
        ("FailedToRetrieveBasket",
        [
            "failed to retrieve basket",
        ]),
        ("BasketHasNoOrders",
        [
            "basket has no orders",
        ]),
        ("FailedParsingDataFromDownStream",
        [
            "failed parsing data",
        ]),
        ("EmptyOrInvalidResponseFromDownstreamService",
        [
            "empty or invalid response",
            "përgjigje e zbrazët",
        ]),
        ("PaymentNotConfiguredForService",
        [
            "payment not configured",
        ]),
        ("FailedToExtractTokenClaims",
        [
            "failed to extract token",
        ]),
    ];

    internal static void ClickConsentCheckboxIfPresent(IWebDriver driver, Action<string> log, params string[] ids)
    {
        string[] candidates = ids is { Length: > 0 }
            ? ids
            : ["agreeCheck", "consentCheckbox", "confirmAdminDocuments"];

        foreach (string id in candidates)
        {
            try
            {
                var matches = driver.FindElements(By.Id(id));
                if (matches.Count == 0)
                    continue;

                IWebElement checkbox = matches[0];
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});",
                    checkbox);
                Thread.Sleep(400);
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", checkbox);
                Thread.Sleep(600);
                log($"U klikua checkbox '{id}' (nëse ishte i pranishëm).");
                return;
            }
            catch (StaleElementReferenceException)
            {
            }
            catch (WebDriverException)
            {
            }
        }

        log("Checkbox i pëlqimit nuk u gjet — vazhdohet me 'Dërgo' pa e klikuar.");
    }

    internal static void AssertInformationalFailAfterDergo(IWebDriver driver, Action<string> log)
    {
        Thread.Sleep(2500);

        if (SawSuccess(driver))
        {
            Assert.Fail(
                "Stimulimi i FAIL dështoi: u shfaq ekrani i suksesit (APLIKIMI JUAJ U DËRGUA ME SUKSES.) " +
                "ndërsa ky test pret një përgjigje informative DPSHTRR (jo sukses).");
        }

        string? modalMessage = TryReadVisibleAlert(driver);
        if (!string.IsNullOrWhiteSpace(modalMessage))
            FinishWithUiMessage(modalMessage, log);

        string uiMessage = CaptureVisibleUiMessage(driver);
        log("Mesazhi i kapur nga UI (rasti FAIL): " + uiMessage);
        FinishWithUiMessage(uiMessage, log);
    }

    internal static void AssertExpectedGabimPopup(
        IWebDriver driver,
        WebDriverWait wait,
        Action<string> log,
        string expectedTitle,
        string expectedDescription,
        string expectedSourceLabel)
    {
        log($"Assert popup Gabim nga {expectedSourceLabel}");
        IWebElement? alert = null;
        try
        {
            alert = wait.Until(ExpectedConditions.ElementIsVisible(
                By.CssSelector(".alert-modal-container")));
        }
        catch (WebDriverTimeoutException)
        {
            log("Popup .alert-modal-container nuk u shfaq brenda timeout-it.");
        }

        if (alert is null)
        {
            string uiMessage = CaptureVisibleUiMessage(driver);
            log("Nuk u shfaq popup. Mesazhi i kapur nga UI: " + uiMessage);
            Assert.Fail(
                $"Nuk u shfaq popup-i i pritur (Gabim / {expectedSourceLabel}). " +
                "Mesazhi që u shfaq në UI: " + uiMessage);
        }

        string actualTitle;
        string actualDescription;
        try
        {
            actualTitle = alert.FindElement(By.CssSelector("h2.alert-modal-title")).Text.Trim();
            actualDescription = alert.FindElement(By.CssSelector(".alert-modal-description")).Text.Trim();
        }
        catch (NoSuchElementException)
        {
            string uiMessage = CaptureVisibleUiMessage(driver);
            log("Popup u gjet por pa titull/përshkrim të pritshëm. Mesazhi i UI: " + uiMessage);
            Assert.Fail(
                "U shfaq një popup tjetër (pa strukturën e pritur). " +
                "Mesazhi që u shfaq në UI: " + uiMessage);
            return;
        }

        string modalMessage = $"[{actualTitle}] {actualDescription}";
        log("Popup: " + modalMessage);

        bool isExpectedPopup =
            string.Equals(actualTitle, expectedTitle, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(actualDescription, expectedDescription, StringComparison.Ordinal);

        if (!isExpectedPopup)
        {
            Assert.Fail(
                $"U shfaq një popup tjetër, jo ai i {expectedSourceLabel}. " +
                "Mesazhi që u shfaq në UI: " + modalMessage);
        }

        log($"U konfirmua popup-i i pritur nga {expectedSourceLabel}.");
    }

    private static void FinishWithUiMessage(string message, Action<string> log)
    {
        if (IsExistingApplicationsMessage(message))
        {
            Assert.Fail(
                "Stimulimi i FAIL dështoi: u shfaq modal 'Kujdes' për aplikime ekzistuese. " +
                $"Mesazhi: {message}");
        }

        if (TryMatchInformationalKey(message, out string key))
        {
            log($"Rasti FAIL — mesazh informativ DPSHTRR ({key}): {message}");
            Assert.Pass($"Rasti FAIL u konfirmua me përgjigje informative '{key}': {message}");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Assert.Fail(
                "Rasti FAIL dështoi: nuk u shfaq as sukses, as mesazh informativ DPSHTRR, as mesazh tjetër në UI.");
        }

        log("Rasti FAIL — mesazh i pranueshëm (jo sukses, jo aplikime ekzistuese): " + message);
        Assert.Pass("Rasti FAIL u konfirmua me mesazh të pranueshëm në UI: " + message);
    }

    internal static bool IsExistingApplicationsMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        bool mentionsApplication =
            text.IndexOf("aplikim", StringComparison.OrdinalIgnoreCase) >= 0 ||
            text.IndexOf("kërkes", StringComparison.OrdinalIgnoreCase) >= 0 ||
            text.IndexOf("kerkes", StringComparison.OrdinalIgnoreCase) >= 0;

        bool mentionsExisting =
            text.IndexOf("ekzist", StringComparison.OrdinalIgnoreCase) >= 0 ||
            text.IndexOf("proces", StringComparison.OrdinalIgnoreCase) >= 0 ||
            text.IndexOf("aktiv", StringComparison.OrdinalIgnoreCase) >= 0 ||
            text.IndexOf("papërfunduar", StringComparison.OrdinalIgnoreCase) >= 0 ||
            text.IndexOf("paperfunduar", StringComparison.OrdinalIgnoreCase) >= 0;

        return mentionsApplication && mentionsExisting;
    }

    internal static bool TryMatchInformationalKey(string text, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (string candidate in InformationalResponseKeys)
        {
            if (text.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                key = candidate;
                return true;
            }
        }

        foreach (var (mappedKey, phrases) in InformationalPhrases)
        {
            foreach (string phrase in phrases)
            {
                if (text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    key = mappedKey;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool SawSuccess(IWebDriver driver)
    {
        By successHeadlineBy = By.XPath(
            "//h5[contains(normalize-space(.),'APLIKIMI JUAJ U DËRGUA ME SUKSES')]");

        try
        {
            return driver.FindElements(successHeadlineBy).Any(e =>
            {
                try { return e.Displayed; }
                catch (StaleElementReferenceException) { return false; }
            });
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    private static string? TryReadVisibleAlert(IWebDriver driver)
    {
        try
        {
            var visibleAlert = driver.FindElements(By.CssSelector(".alert-modal-container")).FirstOrDefault(e =>
            {
                try { return e.Displayed; }
                catch (StaleElementReferenceException) { return false; }
            });

            if (visibleAlert is null)
                return null;

            string title = visibleAlert.FindElement(By.CssSelector("h2.alert-modal-title")).Text.Trim();
            string desc = visibleAlert.FindElement(By.CssSelector(".alert-modal-description")).Text.Trim();
            return $"[{title}] {desc}";
        }
        catch (NoSuchElementException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }

    private static string CaptureVisibleUiMessage(IWebDriver driver)
    {
        Thread.Sleep(800);

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
            ".Toastify__toast-body",
            "form span",
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

        return string.Empty;
    }
}
