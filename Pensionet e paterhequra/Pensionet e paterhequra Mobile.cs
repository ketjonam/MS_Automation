using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System;

[TestFixture]
public class PensionetePaterhequraMobile
{
    [Test]
    public void KryejAplikim()
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
            driver.FindElement(By.Id("Nid")).SendKeys("F60214024S");
            driver.FindElement(By.Id("ServiceCode")).SendKeys("368");
            driver.FindElement(By.Id("MicroserviceName")).SendKeys("issh-ams");
            driver.FindElement(By.Id("UserName")).SendKeys("Ketjona");
            driver.FindElement(By.Id("Email")).SendKeys("ketjona.mema@kreatx.com");
            driver.FindElement(By.Id("PhoneNumber")).SendKeys("0676041404");
            IWebElement tipiProfilitDropdown = driver.FindElement(By.Id("ProfileType"));
            SelectElement tipiProfilit = new SelectElement(tipiProfilitDropdown);
            tipiProfilit.SelectByValue("Individual");
            IWebElement platformaDropdown = driver.FindElement(By.Id("Platform"));
            SelectElement platforma = new SelectElement(platformaDropdown);
            platforma.SelectByValue("MOBILE");
            IWebElement loadService = driver.FindElement(By.ClassName("load-button"));
            loadService.Click();
            Thread.Sleep(3000);

            // krijo aplikim te ri 
            IWebElement AplikimRi = driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div[2]/div/div/div/div/div/div/div/div/h5"));
            AplikimRi.Click();
            Thread.Sleep(6000);

            //Kontrollo informacionet qe shfaqen ne baze te kerkimit ne filter 
            Console.WriteLine("Testi përfundoi me sukses!");
        }
    }
}
