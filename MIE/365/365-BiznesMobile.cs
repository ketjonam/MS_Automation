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
public class BiznesMobile365
{
    [Test]
    public void NIPT()
    {
        var options = new EdgeOptions();
        options.AddArgument("start-maximized");

        using (IWebDriver driver = new EdgeDriver(options))
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

            try
            {
                driver.Navigate().GoToUrl("http://141.95.84.12:8080/");

                // Hape faqen e testimit te sherbimeve
                wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("/html/body/div/main/div/div[1]/div/a"))).Click();

                // Prit qe forma te shfaqet
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Nid")));

                // Mbush fushat
                driver.FindElement(By.Id("Nid")).Clear();
                driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");

                driver.FindElement(By.Id("ServiceCode")).Clear();
                driver.FindElement(By.Id("ServiceCode")).SendKeys("365");

                driver.FindElement(By.Id("MicroserviceName")).Clear();
                driver.FindElement(By.Id("MicroserviceName")).SendKeys("mieinstitution-mie-institution-1");

                driver.FindElement(By.Id("UserName")).Clear();
                driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");

                driver.FindElement(By.Id("Email")).Clear();
                driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");

                driver.FindElement(By.Id("PhoneNumber")).Clear();
                driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");

                // Dropdown-et
                var profileSelect = new SelectElement(driver.FindElement(By.Id("ProfileType")));
                foreach (var opt in profileSelect.Options)
                    Console.WriteLine($"ProfileType -> Text='{opt.Text}', Value='{opt.GetAttribute("value")}'");
                profileSelect.SelectByText("Organisation");

                var platformSelect = new SelectElement(driver.FindElement(By.Id("Platform")));
                foreach (var opt in platformSelect.Options)
                    Console.WriteLine($"Platform -> Text='{opt.Text}', Value='{opt.GetAttribute("value")}'");
                platformSelect.SelectByText("MOBILE");

                Console.WriteLine("Para load URL: " + driver.Url);

                // Gjej butonin Load Service
                var loadBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("load-button")));
                Console.WriteLine("Load button type: " + loadBtn.GetAttribute("type"));
                Console.WriteLine("Load button text: " + loadBtn.Text);

                string oldUrl = driver.Url;

                // Klikim normal
                loadBtn.Click();

                // Prit pak dhe log URL
                for (int i = 1; i <= 5; i++)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine($"Sekonda {i} | URL: {driver.Url}");
                }

                Console.WriteLine("Pas load URL: " + driver.Url);
                Console.WriteLine("URL changed: " + (oldUrl != driver.Url));
                Console.WriteLine("Page title: " + driver.Title);

                // Body text per debug
                Console.WriteLine("Body text:");
                Console.WriteLine(driver.FindElement(By.TagName("body")).Text);

                // Kontrollo fusha invalid
                var invalidFields = driver.FindElements(By.XPath("//*[@aria-invalid='true']"));
                Console.WriteLine("Invalid fields: " + invalidFields.Count);

                // Kontrollo elemente error
                var errorLikeElements = driver.FindElements(By.XPath(
                    "//*[contains(@class,'error') or contains(@class,'alert') or contains(@class,'invalid') or contains(text(),'gabim') or contains(text(),'Error') or contains(text(),'required')]"
                ));
                Console.WriteLine("Error-like elements: " + errorLikeElements.Count);

                foreach (var el in errorLikeElements)
                {
                    Console.WriteLine("ERR: " + el.Text);
                }

                // Screenshot pas load
                string screenshot1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "after-load.png");
                ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(screenshot1);
                Console.WriteLine("Screenshot: " + screenshot1);

                // Nese s'ka shkuar te /UseService/365, provo navigimin manual per debug
                if (!driver.Url.Contains("/UseService/365"))
                {
                    Console.WriteLine("Navigimi automatik nuk shkoi te /UseService/365. Po provoj manualisht...");
                    driver.Navigate().GoToUrl("http://141.95.84.12:8080/UseService/365");
                    Thread.Sleep(3000);

                    Console.WriteLine("Manual URL: " + driver.Url);
                    Console.WriteLine("Manual title: " + driver.Title);
                    Console.WriteLine("Manual body:");
                    Console.WriteLine(driver.FindElement(By.TagName("body")).Text);

                    string screenshot2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "manual-365.png");
                    ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(screenshot2);
                    Console.WriteLine("Manual screenshot: " + screenshot2);
                }

                // Kerko butonin "Aplikim i ri"
                var aplikimIRi = driver.FindElements(
                    By.XPath("//button[.//h6[contains(normalize-space(),'Aplikim i ri')]]")
                );

                Console.WriteLine("Butona 'Aplikim i ri' te gjetur: " + aplikimIRi.Count);

                if (aplikimIRi.Count > 0)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", aplikimIRi[0]);
                    Thread.Sleep(1000);
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", aplikimIRi[0]);
                    Console.WriteLine("Butoni 'Aplikim i ri' u klikua me sukses.");
                }
                else
                {
                    Assert.Fail("Butoni 'Aplikim i ri' nuk u gjet. Kontrollo screenshot-et dhe log-un.");
                }

                Console.WriteLine("Testi përfundoi me sukses!");
            }
            catch (Exception ex)
            {
                string errorScreenshot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.png");
                ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(errorScreenshot);
                Console.WriteLine("Error screenshot: " + errorScreenshot);
                Console.WriteLine("EXCEPTION: " + ex.Message);
                throw;
            }
        }
    }
}