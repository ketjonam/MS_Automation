using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System;

[TestFixture]
public class LogimSiBiznesWEB
{
    [Test]
    public void NIPTWeb()
    {
        var options = new EdgeOptions();
        options.AddArgument("start-maximized");

        using (IWebDriver driver = new EdgeDriver(options))
        {
            driver.Navigate().GoToUrl("http://141.95.84.12:8080/");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            // Kliko butonin për "Test Sherbimesh"
            var btn = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(
                By.XPath("/html/body/div/main/div/div[1]/div/a")));
            btn.Click();

            // Mbush fushat tekstuale
            driver.FindElement(By.Id("Nid")).SendKeys("L12121023B");
            driver.FindElement(By.Id("ServiceCode")).SendKeys("368");
            driver.FindElement(By.Id("MicroserviceName")).SendKeys("issh-ams");
            driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
            driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
            driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");
            IWebElement tipiProfilitDropdown = driver.FindElement(By.Id("ProfileType"));
            SelectElement tipiProfilit = new SelectElement(tipiProfilitDropdown);
            tipiProfilit.SelectByValue("Organisation");
            IWebElement platformaDropdown = driver.FindElement(By.Id("Platform"));
            SelectElement platforma = new SelectElement(platformaDropdown);
            platforma.SelectByValue("WEB");
            IWebElement loadService = driver.FindElement(By.ClassName("load-button"));
            loadService.Click();

            Thread.Sleep(3000);

            //kontrollo qe te shfaqet mesazhi qe sherbimi nuk eshte i disponueshem per biznes
            var alertMessage = wait.Until(
    SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
        By.Id("swal2-html-container")));

            // Assert 1 – elementi ekziston dhe është i dukshëm
            Assert.That(alertMessage.Displayed, Is.True,
                "Mesazhi i popup-it nuk u shfaq.");
            Thread.Sleep(1000);

            // Assert 2 – teksti përputhet me vlerën e pritur
            Assert.That(alertMessage.Text.Trim(),
                Is.EqualTo("Ky shërbim ofrohet për llogaritë e tipit 'Qytetar'"),
                $"Teksti i popup-it nuk përputhet. U gjet: '{alertMessage.Text}'.");



            Console.WriteLine("Testi përfundoi me sukses!");
        }
    }
}
