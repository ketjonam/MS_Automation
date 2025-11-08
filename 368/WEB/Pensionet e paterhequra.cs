using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System;

[TestFixture]
public class PensionetePaterhequra
{
    [Test]
    public void KlikoTestSherbimesh()
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
            platforma.SelectByValue("WEB");
            IWebElement loadService = driver.FindElement(By.ClassName("load-button"));
            loadService.Click();
            Thread.Sleep(3000);

            // krijo aplikim te ri 
            IWebElement AplikimRi = driver.FindElement(By.XPath("/html/body/div/main/div[3]/div/div/div[2]/div/div/div/div/div/div/div/div/h5"));
            AplikimRi.Click();
            Thread.Sleep(6000);

            /// Kliko filtrin "Kërko"
            IWebElement kerkofilter = wait.Until(
    SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(
        By.Id(":r0:")));

            // skenari kur nuk gjen te dhena sipas kerkimit ne filter
            kerkofilter.SendKeys("test");

            Thread.Sleep(2000);

            // Prit që tabela të përditësohet dhe të shfaqet mesazhi
            var emptyMessageCell = wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.XPath("/html/body/div/main/div[3]/div/div/div[2]/div/div[3]/table/tbody/tr")));

            Thread.Sleep(1000);

            // Assert 1 – Kontrollo që elementi ekziston dhe është i dukshëm
            Assert.That(emptyMessageCell.Displayed, Is.True,
                "Mesazhi 'Nuk u gjend asnjë masë' nuk u shfaq në tabelë.");

            Thread.Sleep(1000);

            // Assert 2 – Kontrollo që teksti i tij është i saktë
            Assert.That(emptyMessageCell.Text.Trim(), Is.EqualTo("Nuk u gjend asnjë masë"),
                $"Teksti i mesazhit nuk përputhet. U gjet: '{emptyMessageCell.Text}'.");

            //skenari kur gjen te dhena ne filter

            IWebElement kerkofilter2 = wait.Until(
    SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(
        By.Id(":r0:")));
            kerkofilter2.Clear();
            kerkofilter2.SendKeys("7085");

            Thread.Sleep(1000);

            // Merr të gjithë rreshtat që përmbajnë "7085"
            var matchingRows = driver.FindElements(
                By.XPath("//table/tbody/tr[td[contains(text(),'7085')]]"));

            // Assert 1 – Duhet të ketë të paktën një rresht me "7085"
            Assert.That(matchingRows.Count, Is.GreaterThan(0),
                "Nuk u gjet asnjë rresht që përmban '7085' pas filtrimit.");

            // Assert 2 – Mesazhi "Nuk u gjend asnjë masë" nuk duhet të jetë më i pranishëm
            var emptyMessageRows = driver.FindElements(
                By.XPath("//table/tbody/tr/td[contains(text(),'Nuk u gjend asnjë masë')]"));

            Assert.That(emptyMessageRows.Count, Is.EqualTo(0),
                "Mesazhi 'Nuk u gjend asnjë masë' nuk duhet të shfaqet kur ka rezultate për '20091'.");
            Console.WriteLine("Testi përfundoi me sukses!");
            Console.WriteLine("Testi përfundoi me sukses!");
        }
    }
}


