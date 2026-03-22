using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobScraper.Migrations
{
    /// <inheritdoc />
    public partial class CustomScraperConfig_SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CustomScraperConfigs",
                columns: new[] { "Id", "DataOrigin", "DetailsScraperScript", "DetailsScrapingEnabled", "Domain", "ListScraperScript", "PaginationScript", "TestDetailsUrl", "TestListUrl", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "Indeed", "() => {\r\n    let description = document.querySelector('#jobDescriptionText')?.innerText ?? '';\r\n\r\n    let rawSalary = Array.from(document.querySelectorAll('span[class*=\"js-match-insights-provider\"]'))\r\n        .filter(span => span.textContent.includes('$'))[0]?.textContent ?? '';\r\n\r\n    let keywords = [];\r\n\r\n    return JSON.stringify({ Description: description, Keywords: keywords });\r\n}\r\n", true, "indeed.com", "() => {\r\n    let titleElements = document.querySelectorAll('h2.jobTitle');\r\n    let companyElements = document.querySelectorAll(\"[data-testid='company-name']\");\r\n    let locationElements = document.querySelectorAll(\"[data-testid='text-location']\");\r\n\r\n    let results = Array.from(titleElements).map((titleEl, i) => {\r\n        let anchor = titleEl.querySelector('a');\r\n        let rawUrl = anchor?.getAttribute('href') ?? '';\r\n        let url = window.location.origin + rawUrl.split('&')[0].replace('/rc/clk', '/viewjob');\r\n\r\n        return {\r\n            Title: titleEl.innerText.trim(),\r\n            Url: url,\r\n            CompanyName: companyElements[i]?.innerText.trim() ?? '',\r\n            Location: locationElements[i]?.innerText.trim() ?? '',\r\n            OfferKeywords: []\r\n        };\r\n    });\r\n\r\n    return JSON.stringify(results);\r\n}\r\n", "() => {\r\n    const nextBtn = document.querySelector(\"a[data-testid='pagination-page-next']\");\r\n    if (!nextBtn) return JSON.stringify({ HasNextPage: false });\r\n    nextBtn.click();\r\n    return JSON.stringify({ HasNextPage: true });\r\n}\r\n", null, null, null },
                    { 2L, "JustJoinIt", "() => {\r\n    let descDiv = document.querySelector('h3')?.parentNode;\r\n    if (!descDiv) return JSON.stringify({ Description: '', Keywords: [] });\r\n\r\n    const result = {\r\n        Description: [...descDiv.childNodes].slice(1).map(x => x?.textContent).join('\\n'),\r\n        Keywords: [...descDiv.querySelectorAll('h4')].map(x => x?.textContent?.trim())\r\n    };\r\n\r\n    return JSON.stringify(result);\r\n}\r\n", true, "justjoin.it", "() => {\r\n    let jobItems = document.querySelectorAll(\"a.offer-card\");\r\n\r\n    let results = Array.from(jobItems).map(a => {\r\n        let mainDiv = a.querySelector('div > div > div');\r\n        if (!mainDiv || mainDiv.childNodes.length < 3) return null;\r\n\r\n        let bottomDiv = mainDiv.childNodes[2];\r\n        let infoDiv = bottomDiv.childNodes[0];\r\n        let keywordsDiv = bottomDiv.childNodes[1];\r\n\r\n        return {\r\n            Title: mainDiv.querySelector('h3')?.textContent.trim(),\r\n            Url: window.location.origin + a.getAttribute('href'),\r\n            CompanyName: mainDiv.querySelector('div > div > div > div > p')?.textContent.trim(),\r\n            Location: infoDiv.childNodes?.[1]?.textContent.replace('Locations', ' Locations').trim(),\r\n            SalaryToParse: mainDiv.querySelector('h6')?.textContent.trim(),\r\n            OfferKeywords: [...keywordsDiv.childNodes].map(x => x?.textContent.trim()).slice(1)\r\n        };\r\n    });\r\n\r\n    return JSON.stringify(results.filter(x => x != null));\r\n}\r\n", "(pageNumber) => {\r\n    window.scrollTo(0, pageNumber * 1800);\r\n    return JSON.stringify({ HasNextPage: true });\r\n}\r\n", null, null, null },
                    { 3L, "NoFluffJobs", "() => {\r\n    document.querySelector('span[data-cy=\"text-fold\"]')?.click();\r\n\r\n    let result = {\r\n        Description: document.querySelector('common-posting-content-wrapper')?.textContent.trim(),\r\n        Keywords: [...(document.querySelector('section[commonpostingrequirements]')?.querySelectorAll('li') ?? [])].map(x => x?.textContent?.trim())\r\n    };\r\n\r\n    return JSON.stringify(result);\r\n}\r\n", true, "nofluffjobs.com", "() => {\r\n    let lists = document.querySelectorAll('nfj-postings-list[listname=\"search\"]');\r\n    let jobItems = Array.from(lists).flatMap(list =>\r\n        Array.from(list.querySelectorAll(\"a.posting-list-item\"))\r\n    );\r\n\r\n    let results = Array.from(jobItems).map(a => {\r\n        let aside = a.querySelector('aside');\r\n        return {\r\n            Title: aside.querySelector('h3')?.textContent.trim(),\r\n            Url: window.location.origin + a.getAttribute('href'),\r\n            CompanyName: aside.querySelector('footer > h4')?.textContent.trim(),\r\n            Location: aside.querySelector('footer > nfj-posting-item-city > div > span')?.textContent.trim(),\r\n            SalaryToParse: aside.querySelector('nfj-posting-item-salary')?.textContent.trim(),\r\n            OfferKeywords: [...aside.querySelectorAll('nfj-posting-item-tiles > span')].map(s => s?.textContent.trim())\r\n        };\r\n    });\r\n\r\n    return JSON.stringify(results);\r\n}\r\n", "() => {\r\n    const button = document.querySelector('button[nfjloadmore]');\r\n    if (!button) return JSON.stringify({ HasNextPage: false });\r\n    button.click();\r\n    return JSON.stringify({ HasNextPage: true });\r\n}\r\n", null, null, null },
                    { 4L, "Olx", null, false, "olx.pl", "() => {\r\n    let cards = document.querySelectorAll('div.jobs-ad-card');\r\n\r\n    let results = Array.from(cards).map(offer => {\r\n        let titleContainer = [...offer.querySelectorAll('div > div > a')].at(-1);\r\n        let firstRow = offer.querySelector('div:nth-child(2) > div > div');\r\n        let secondRow = offer.querySelector('div:nth-child(2) > div > div:nth-child(2)');\r\n\r\n        let title = titleContainer?.textContent.trim() ?? '';\r\n        let url = titleContainer?.getAttribute('href') ?? '';\r\n        let companyName = titleContainer?.parentNode?.querySelector('p')?.textContent.trim() ?? '';\r\n\r\n        let firstRowData = [...(firstRow?.querySelectorAll('div > div > p') ?? [])]\r\n            .map(p => p.textContent.trim()).reverse();\r\n        let secondRowData = [...(secondRow?.querySelectorAll('button') ?? [])]\r\n            .map(b => b.textContent.trim());\r\n\r\n        // First item in reversed firstRowData may be salary, last is location, rest are keywords\r\n        let location = firstRowData.length > 0 ? firstRowData[firstRowData.length - 1] : '';\r\n        let keywords = [...firstRowData.slice(0, -1), ...secondRowData];\r\n\r\n        return {\r\n            Title: title,\r\n            Url: url,\r\n            CompanyName: companyName,\r\n            Location: location,\r\n            OfferKeywords: keywords\r\n        };\r\n    });\r\n\r\n    return JSON.stringify(results);\r\n}\r\n", "() => {\r\n    const nextBtn = document.querySelector(\"a[data-testid='pagination-forward']\");\r\n    if (!nextBtn) return JSON.stringify({ HasNextPage: false });\r\n    nextBtn.click();\r\n    return JSON.stringify({ HasNextPage: true });\r\n}\r\n", null, null, null },
                    { 5L, "PracujPl", null, false, "pracuj.pl", "() => {\r\n    // Expand hidden descriptions\r\n    document.querySelectorAll('[data-test=\"section-short-description\"] div.invisible span')\r\n        .forEach(el => el.click());\r\n\r\n    let offers = document.querySelectorAll('div[data-test=\"positioned-offer\"], div[data-test=\"default-offer\"]');\r\n\r\n    let results = Array.from(offers).map(offer => {\r\n        let title = offer.querySelector('h2[data-test=\"offer-title\"]')?.textContent.trim() ?? '';\r\n        let links = [...offer.querySelectorAll('a[data-test=\"link-offer\"]')].map(a => a.getAttribute('href'));\r\n        let url = links[0]?.split('?')[0] ?? '';\r\n        let salary = offer.querySelector('span[data-test=\"offer-salary\"]')?.textContent.trim() ?? '';\r\n        let jobKeys = [...offer.querySelectorAll('ul > li')].map(li => li.textContent.trim());\r\n        let description = offer.querySelector('[data-test=\"section-short-description\"]')?.textContent.trim() ?? '';\r\n        let companyName = offer.querySelector('[data-test=\"text-company-name\"]')?.textContent.trim() ?? '';\r\n        let location = offer.querySelector('h4[data-test=\"text-region\"]')?.textContent.trim() ?? '';\r\n\r\n        return {\r\n            Title: title,\r\n            Url: url,\r\n            CompanyName: companyName,\r\n            Location: location,\r\n            SalaryToParse: salary,\r\n            OfferKeywords: jobKeys,\r\n            Description: description\r\n        };\r\n    });\r\n\r\n    return JSON.stringify(results);\r\n}\r\n", "() => {\r\n    const nextBtn = document.querySelector(\"button[data-test='bottom-pagination-button-next']\");\r\n    if (!nextBtn) return JSON.stringify({ HasNextPage: false });\r\n    nextBtn.click();\r\n    return JSON.stringify({ HasNextPage: true });\r\n}\r\n", null, null, null },
                    { 6L, "RocketJobs", "() => {\r\n    let descDiv = document.querySelector('h3')?.parentNode;\r\n    if (!descDiv) return JSON.stringify({ Description: '', Keywords: [] });\r\n\r\n    const result = {\r\n        Description: [...descDiv.childNodes].slice(1).map(x => x?.textContent).join('\\n'),\r\n        Keywords: [...descDiv.querySelectorAll('h4')].map(x => x?.textContent?.trim())\r\n    };\r\n\r\n    return JSON.stringify(result);\r\n}\r\n", true, "rocketjobs.pl", "() => {\r\n    let jobItems = document.querySelectorAll(\"a.offer-card\");\r\n\r\n    let results = Array.from(jobItems).map(a => {\r\n        let mainDiv = a.querySelector('div > div > div');\r\n        if (!mainDiv || mainDiv.childNodes.length < 3) return null;\r\n\r\n        let bottomDiv = mainDiv.childNodes[2];\r\n        let infoDiv = bottomDiv.childNodes[0];\r\n        let keywordsDiv = bottomDiv.childNodes[1];\r\n\r\n        return {\r\n            Title: mainDiv.querySelector('h3')?.textContent.trim(),\r\n            Url: window.location.origin + a.getAttribute('href'),\r\n            CompanyName: mainDiv.querySelector('div > div > div > div > p')?.textContent.trim(),\r\n            Location: infoDiv.childNodes?.[1]?.textContent.replace('Locations', ' Locations').trim(),\r\n            SalaryToParse: mainDiv.querySelector('h6')?.textContent.trim(),\r\n            OfferKeywords: [...keywordsDiv.childNodes].map(x => x?.textContent.trim()).slice(1)\r\n        };\r\n    });\r\n\r\n    return JSON.stringify(results.filter(x => x != null));\r\n}\r\n", "(pageNumber) => {\r\n    window.scrollTo(0, pageNumber * 1200);\r\n    return JSON.stringify({ HasNextPage: true });\r\n}\r\n", null, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CustomScraperConfigs",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "CustomScraperConfigs",
                keyColumn: "Id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "CustomScraperConfigs",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "CustomScraperConfigs",
                keyColumn: "Id",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "CustomScraperConfigs",
                keyColumn: "Id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "CustomScraperConfigs",
                keyColumn: "Id",
                keyValue: 6L);
        }
    }
}
